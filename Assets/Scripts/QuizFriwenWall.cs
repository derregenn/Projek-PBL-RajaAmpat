using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class QuizFriwenWall : MonoBehaviour
{
    [Header("UI Panel & Options")]
    public GameObject quizPanel;
    public Button buttonOptionA; // Benar (Makhluk hidup rapuh)
    public Button buttonOptionB; // Salah (Dinding keras)
    public Button buttonOptionC; // Salah (Ikan marah)

    [Header("Quest Objective")]
    [SerializeField] private string objectiveID = "foto_pasir_timbul";

    [Header("Audio SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successSfx;
    [SerializeField] private AudioClip failSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Events")]
    public UnityEvent onAnswerCorrect;
    public UnityEvent onAnswerWrong;

    [Header("Feedback Outline Settings")]
    [SerializeField] private Color correctOutlineColor = new Color(0.18f, 0.85f, 0.42f, 1f);
    [SerializeField] private Color wrongOutlineColor = new Color(0.95f, 0.22f, 0.24f, 1f);
    [SerializeField] private float feedbackOutlineThickness = 14f;
    [SerializeField] private float feedbackDelay = 0.85f;
    private bool isShowingFeedback = false;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
        if (successSfx != null && successSfx.loadState != AudioDataLoadState.Loaded) successSfx.LoadAudioData();
        if (failSfx != null && failSfx.loadState != AudioDataLoadState.Loaded) failSfx.LoadAudioData();

        ClearAllFeedbackOutlines();
        if (quizPanel != null) quizPanel.SetActive(false);

        if (buttonOptionA != null) buttonOptionA.onClick.AddListener(() => OnOptionClicked(buttonOptionA, true));
        if (buttonOptionB != null) buttonOptionB.onClick.AddListener(() => OnOptionClicked(buttonOptionB, false));
        if (buttonOptionC != null) buttonOptionC.onClick.AddListener(() => OnOptionClicked(buttonOptionC, false));
    }

    public void ShowQuiz()
    {
        isShowingFeedback = false;
        ClearAllFeedbackOutlines();
        SetButtonsInteractable(true);
        if (quizPanel != null) quizPanel.SetActive(true);
    }

    public void SelectCorrectAnswer()
    {
        OnOptionClicked(buttonOptionA, true);
    }

    public void SelectWrongAnswer()
    {
        OnOptionClicked(buttonOptionB, false);
    }

    private void OnOptionClicked(Button clickedBtn, bool isCorrect)
    {
        if (isShowingFeedback) return;
        StartCoroutine(ShowFeedbackAndFinish(clickedBtn, isCorrect));
    }

    private System.Collections.IEnumerator ShowFeedbackAndFinish(Button clickedBtn, bool isCorrect)
    {
        isShowingFeedback = true;
        SetButtonsInteractable(false);
        PlaySfx(isCorrect ? successSfx : failSfx);

        Color outlineCol = isCorrect ? correctOutlineColor : wrongOutlineColor;
        ApplyOutlineToObject(clickedBtn != null ? clickedBtn.gameObject : null, true, outlineCol);
        ApplyOutlineToQuizBox(true, outlineCol);

        yield return new WaitForSeconds(feedbackDelay);

        ClearAllFeedbackOutlines();
        SetButtonsInteractable(true);
        isShowingFeedback = false;

        if (quizPanel != null) quizPanel.SetActive(false);

        if (isCorrect)
        {
            if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
            {
                QuestController.Instance.ProgressObjective(objectiveID, 1);
            }
            onAnswerCorrect?.Invoke();
        }
        else
        {
            onAnswerWrong?.Invoke();
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (buttonOptionA != null) buttonOptionA.interactable = interactable;
        if (buttonOptionB != null) buttonOptionB.interactable = interactable;
        if (buttonOptionC != null) buttonOptionC.interactable = interactable;
    }

    private void ClearAllFeedbackOutlines()
    {
        if (buttonOptionA != null) ApplyOutlineToObject(buttonOptionA.gameObject, false, Color.clear);
        if (buttonOptionB != null) ApplyOutlineToObject(buttonOptionB.gameObject, false, Color.clear);
        if (buttonOptionC != null) ApplyOutlineToObject(buttonOptionC.gameObject, false, Color.clear);
        ApplyOutlineToQuizBox(false, Color.clear);
    }

    private void ApplyOutlineToQuizBox(bool visible, Color col)
    {
        if (quizPanel == null) return;
        Transform qBox = quizPanel.transform.Find("QuestionBox");
        GameObject target = qBox != null ? qBox.gameObject : quizPanel;
        ApplyOutlineToObject(target, visible, col);
    }

    private void ApplyOutlineToObject(GameObject target, bool visible, Color col)
    {
        if (target == null) return;
        ProceduralRoundedRect rounded = target.GetComponent<ProceduralRoundedRect>();
        if (rounded != null)
        {
            rounded.SetOutline(visible, col, feedbackOutlineThickness);
            return;
        }
        Graphic g = target.GetComponent<Graphic>();
        if (g == null) return;
        Outline outline = g.GetComponent<Outline>();
        if (outline == null)
        {
            if (!visible) return;
            outline = g.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(8f, 8f);
            outline.useGraphicAlpha = false;
        }
        outline.effectColor = col;
        outline.enabled = visible;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        GameObject tempGo = new GameObject("QuizFriwenWall_SFX");
        AudioSource src = tempGo.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = sfxVolume;
        src.spatialBlend = 0f;
        src.Play();
        Destroy(tempGo, clip.length + 0.1f);
    }
}