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

        if (quizPanel != null) quizPanel.SetActive(false);

        if (buttonOptionA != null) buttonOptionA.onClick.AddListener(SelectCorrectAnswer);
        if (buttonOptionB != null) buttonOptionB.onClick.AddListener(SelectWrongAnswer);
        if (buttonOptionC != null) buttonOptionC.onClick.AddListener(SelectWrongAnswer);
    }

    public void ShowQuiz()
    {
        if (quizPanel != null) quizPanel.SetActive(true);
    }

    public void SelectCorrectAnswer()
    {
        PlaySfx(successSfx);
        if (quizPanel != null) quizPanel.SetActive(false);

        // Update Quest ke Pasir Timbul
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
        }

        onAnswerCorrect?.Invoke();
    }

    public void SelectWrongAnswer()
    {
        PlaySfx(failSfx);
        if (quizPanel != null) quizPanel.SetActive(false);

        onAnswerWrong?.Invoke();
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