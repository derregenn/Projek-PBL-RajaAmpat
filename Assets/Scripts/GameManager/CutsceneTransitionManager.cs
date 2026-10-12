using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class CutsceneTransitionManager : MonoBehaviour
{
    public static CutsceneTransitionManager Instance;

    [Header("UI Components")]
    public GameObject cutsceneCanvas;
    public Image portraitDisplay;       // Gambar ilustrasi pesawat/lokasi
    public GameObject dialogueBox;     // Panel/Box Dialog
    public TMP_Text dialogueText;       // Komponen Teks Narasi cerita
    
    [Header("Fade Settings")]
    public Image fadeBlackScreen;
    public float fadeDuration = 1.0f;

    private Sprite[] currentPortraits;
    private string[] currentDialogues;  // Array teks cerita untuk tiap gambar
    private int portraitIndex = 0;
    private string nextSceneName;
    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance == null) {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            
            if (cutsceneCanvas != null) {
                DontDestroyOnLoad(cutsceneCanvas);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Panggil fungsi ini dengan mengirimkan Gambar DAN Narasi
    public void StartPortraitCutscene(Sprite[] portraits, string[] dialogues, string sceneToLoad)
    {
        currentPortraits = portraits;
        currentDialogues = dialogues;
        nextSceneName = sceneToLoad;
        portraitIndex = 0;
        isTransitioning = false;
        
        SetFadeAlpha(0f); 

        portraitDisplay.gameObject.SetActive(true);
        if (dialogueBox != null) dialogueBox.SetActive(true);

        cutsceneCanvas.SetActive(true);
        ShowNextPortrait();
    }

    void Update()
    {
        if (isTransitioning) return;

        // BISA KLIK DI MANA SAJA ATAU TEKAN SPACE / ENTER UNTUK LANJUT
        if (cutsceneCanvas.activeSelf && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            ShowNextPortrait();
        }
    }

    private void ShowNextPortrait()
    {
        if (portraitIndex < currentPortraits.Length)
        {
            string txt = (currentDialogues != null && portraitIndex < currentDialogues.Length) ? currentDialogues[portraitIndex] : "";
            StartCoroutine(FadeToNextPortrait(currentPortraits[portraitIndex], txt));
            portraitIndex++;
        }
        else
        {
            StartCoroutine(EndCutsceneAndLoadScene());
        }
    }

    private IEnumerator FadeToNextPortrait(Sprite nextSprite, string nextText)
    {
        isTransitioning = true;

        // Fade out gambar lama jika ada
        if (portraitDisplay.sprite != null && portraitDisplay.color.a > 0)
        {
            yield return StartCoroutine(FadeAlpha(portraitDisplay, 1f, 0f, 0.3f));
        }

        // Update gambar & teks cerita baru
        portraitDisplay.sprite = nextSprite;
        if (dialogueText != null) dialogueText.text = nextText;

        // Fade in gambar baru
        yield return StartCoroutine(FadeAlpha(portraitDisplay, 0f, 1f, 0.3f));

        isTransitioning = false;
    }

    private IEnumerator EndCutsceneAndLoadScene()
    {
        isTransitioning = true; 

        // 1. Fade In Layar Hitam
        yield return StartCoroutine(FadeAlpha(fadeBlackScreen, 0f, 1f, fadeDuration));

        // 2. Sembunyikan elemen cutscene
        portraitDisplay.gameObject.SetActive(false); 
        portraitDisplay.sprite = null; 
        if (dialogueBox != null) dialogueBox.SetActive(false);

        // 3. Load Scene Waisai
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null; 
        }

        // 4. Fade Out Hitam di Waisai
        yield return StartCoroutine(FadeAlpha(fadeBlackScreen, 1f, 0f, fadeDuration));

        // 5. Matikan kanvas
        portraitDisplay.gameObject.SetActive(true); 
        if (dialogueBox != null) dialogueBox.SetActive(true);
        cutsceneCanvas.SetActive(false);
        isTransitioning = false;
    }

    private IEnumerator FadeAlpha(Image img, float startAlpha, float targetAlpha, float duration)
    {
        float time = 0;
        Color c = img.color;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            img.color = c;
            yield return null; 
        }
        
        c.a = targetAlpha;
        img.color = c;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeBlackScreen != null) {
            Color c = fadeBlackScreen.color;
            c.a = alpha;
            fadeBlackScreen.color = c;
        }
    }
}