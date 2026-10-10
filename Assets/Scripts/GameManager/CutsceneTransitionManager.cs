using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CutsceneTransitionManager : MonoBehaviour
{
    public static CutsceneTransitionManager Instance { get; private set; }

    [Header("UI Components")]
    [Tooltip("Canvas tempat UI Cutscene diletakkan.")]
    public GameObject cutsceneCanvas;

    [Tooltip("Komponen UI Image yang menampilkan gambar portrait.")]
    public Image portraitDisplay;

    // Variabel internal
    private Sprite[] currentPortraits;
    private int portraitIndex = 0;
    private string nextSceneName;
    private bool isCutsceneActive = false;

    private void Awake()
    {
        // Pola Singleton & Auto-Destroy
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); // Memastikan objek berada di Root Hierarchy
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Matikan canvas secara otomatis saat awal game berjalan
        if (cutsceneCanvas != null)
        {
            cutsceneCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        // Deteksi klik mouse / Enter hanya jika cutscene sedang aktif
        if (isCutsceneActive && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            ShowNextPortrait();
        }
    }

    /// <summary>
    /// Panggil fungsi ini dari mana saja (MainMenuController / Tombol Peta Kapal) untuk memulai Cutscene.
    /// </summary>
    /// <param name="portraits">Array gambar Sprite yang ingin ditampilkan berurutan.</param>
    /// <param name="sceneToLoad">Nama Scene tujuan setelah cutscene selesai.</param>
    public void StartPortraitCutscene(Sprite[] portraits, string sceneToLoad)
    {
        // Validasi data
        if (portraits == null || portraits.Length == 0)
        {
            Debug.LogError("[CutsceneManager] Array gambar kosong! Pindah scene tanpa cutscene.");
            SceneManager.LoadScene(sceneToLoad);
            return;
        }

        currentPortraits = portraits;
        nextSceneName = sceneToLoad;
        portraitIndex = 0;
        isCutsceneActive = true;

        if (cutsceneCanvas != null)
        {
            cutsceneCanvas.SetActive(true);
        }

        ShowNextPortrait();
    }

    private void ShowNextPortrait()
    {
        if (portraitIndex < currentPortraits.Length)
        {
            // Update gambar
            if (portraitDisplay != null && currentPortraits[portraitIndex] != null)
            {
                portraitDisplay.sprite = currentPortraits[portraitIndex];
            }

            portraitIndex++;
        }
        else
        {
            // Gambar habis -> Masuk ke proses pindah scene
            StartCoroutine(EndCutsceneAndLoadScene());
        }
    }

    private IEnumerator EndCutsceneAndLoadScene()
    {
        isCutsceneActive = false;

        // Jeda sebentar sebelum load (bisa ditambah animasi Fade Out jika ada)
        yield return new WaitForSeconds(0.2f);

        // Muat scene baru
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("[CutsceneManager] Nama target Scene belum diisi!");
        }

        // Sembunyikan kembali Canvas Cutscene
        if (cutsceneCanvas != null)
        {
            cutsceneCanvas.SetActive(false);
        }
    }
}