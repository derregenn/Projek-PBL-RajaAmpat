using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Cutscene Assets")]
    public Sprite[] arrivalPortraits; // Gambar jendela pesawat

    [Header("UI Buttons")]
    public Button continueButton;     // Drag Btn_Continue ke sini

    void Start()
    {
        // Cek apakah ada data save scene ATAU data save pulau di PlayerPrefs
        if (PlayerPrefs.HasKey("SavedScene") || PlayerPrefs.HasKey("Save_Waisai"))
        {
            if (continueButton != null)
                continueButton.interactable = true; // Nyalakan tombol Continue
        }
        else
        {
            if (continueButton != null)
                continueButton.interactable = false; // Matikan/Gelapkan jika belum ada save
        }
    }

    // 🔴 DIPANGGIL OLEH TOMBOL 'NEW GAME'
    public void StartNewGame()
    {
        // 1. Hapus semua data save PlayerPrefs (SavedScene, status pulau, dll)
        PlayerPrefs.DeleteAll();

        // 2. Reset progres persentase di GamePhaseManager
        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.ResetProgress();
        }

        // 3. Set fase cerita ke awal di StoryManager
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.currentPhase = StoryManager.StoryPhase.Arrival_Airstrip;
        }

        // 4. Putar Cutscene Arrival, lalu otomatis load Waisai (atau scene Airstrip)
        if (CutsceneTransitionManager.Instance != null)
        {
            CutsceneTransitionManager.Instance.StartPortraitCutscene(arrivalPortraits, "02_Waisai");
        }
        else
        {
            // Fallback jika CutsceneTransitionManager belum dipasang/null saat testing
            SceneManager.LoadScene("02_Waisai");
        }
    }

    // 🟢 DIPANGGIL OLEH TOMBOL 'CONTINUE'
    public void ContinueGame()
    {
        // 1. Muat ulang status persentase & pulau di GamePhaseManager
        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.LoadProgress();
        }

        // 2. Ambil data fase cerita terakhir yang disimpan
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.currentPhase = (StoryManager.StoryPhase)PlayerPrefs.GetInt("SavedStoryPhase", 0);
        }

        // 3. Ambil nama scene terakhir yang disimpan (Default ke Waisai jika belum ada)
        string sceneToLoad = PlayerPrefs.GetString("SavedScene", "02_Waisai");

        // 4. LANGSUNG LOAD SCENE, LEWATI CUTSCENE!
        SceneManager.LoadScene(sceneToLoad);
    }

    public void QuitGame()
    {
        Debug.Log("Game Keluar!");
        Application.Quit();
    }
}