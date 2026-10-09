using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneTransitionManager : MonoBehaviour
{
    public static CutsceneTransitionManager Instance;

    [Header("UI Components")]
    public GameObject cutsceneCanvas;
    public Image portraitDisplay;
    public Button nextButton; // 🔴 BARU: Referensi ke tombol Next
    
    [Header("Fade Settings")]
    public Image fadeBlackScreen;
    public float fadeDuration = 1.0f;

    private Sprite[] currentPortraits;
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
    }

    // 🔴 BARU: Fungsi untuk menyambungkan tombol dari Inspector (Dipanggil di Awake/Start)
    void Start()
    {
        if (nextButton != null)
        {
            // Daftarkan fungsi klik ke tombol Next
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    public void StartPortraitCutscene(Sprite[] portraits, string sceneToLoad)
    {
        currentPortraits = portraits;
        nextSceneName = sceneToLoad;
        portraitIndex = 0;
        isTransitioning = false;
        
        SetFadeAlpha(0f); 

        // Pastikan portrait nyala dan tombol Next aktif
        portraitDisplay.gameObject.SetActive(true);
        if (nextButton != null) nextButton.interactable = true;

        cutsceneCanvas.SetActive(true);
        ShowNextPortrait();
    }

    // 🔴 BARU: Fungsi khusus yang dipanggil saat tombol Next diklik
    public void OnNextButtonClicked()
    {
        if (isTransitioning) return; // Abaikan klik jika sedang fade
        ShowNextPortrait();
    }

    void Update()
    {
        if (isTransitioning) return;

        // Mempertahankan fungsi klik sembarang (opsional, bisa kamu hapus jika HANYA mau pakai tombol)
        if (cutsceneCanvas.activeSelf && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return)))
        {
            // Jangan memicu transisi ganda jika pemain mengklik TEPAT di atas tombol Next
            // (Unity akan memanggil OnNextButtonClicked DAN ini bersamaan)
            // Jadi kita cegah konflik ringan:
            UnityEngine.EventSystems.EventSystem currentEventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (currentEventSystem != null && currentEventSystem.currentSelectedGameObject == nextButton.gameObject)
            {
               return; // Biarkan OnNextButtonClicked yang bekerja
            }

            ShowNextPortrait();
        }
    }

    private void ShowNextPortrait()
    {
        if (portraitIndex < currentPortraits.Length)
        {
            StartCoroutine(FadeToNextPortrait(currentPortraits[portraitIndex]));
            portraitIndex++;
        }
        else
        {
            StartCoroutine(EndCutsceneAndLoadScene());
        }
    }

    private IEnumerator FadeToNextPortrait(Sprite nextSprite)
    {
        isTransitioning = true;
        if (nextButton != null) nextButton.interactable = false; // Matikan tombol saat fade

        if (portraitDisplay.sprite != null && portraitDisplay.color.a > 0)
        {
            yield return StartCoroutine(FadeAlpha(portraitDisplay, 1f, 0f, 0.5f));
        }

        portraitDisplay.sprite = nextSprite;

        yield return StartCoroutine(FadeAlpha(portraitDisplay, 0f, 1f, 0.5f));

        isTransitioning = false;
        if (nextButton != null) nextButton.interactable = true; // Nyalakan tombol lagi
    }

    private IEnumerator EndCutsceneAndLoadScene()
    {
        isTransitioning = true; 
        if (nextButton != null) nextButton.interactable = false;

        yield return StartCoroutine(FadeAlpha(fadeBlackScreen, 0f, 1f, fadeDuration));

        portraitDisplay.gameObject.SetActive(false); 
        portraitDisplay.sprite = null; 

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        
        while (!asyncLoad.isDone)
        {
            yield return null; 
        }

        yield return StartCoroutine(FadeAlpha(fadeBlackScreen, 1f, 0f, fadeDuration));

        portraitDisplay.gameObject.SetActive(true); 
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