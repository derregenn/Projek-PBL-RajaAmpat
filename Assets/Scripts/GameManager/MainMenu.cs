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
        // Cek apakah ada data save sebelumnya. 
        // (Misal kamu simpan nama scene terakhir di PlayerPrefs dengan key "SavedScene")
        if (PlayerPrefs.HasKey("SavedScene"))
        {
            continueButton.interactable = true; // Nyalakan tombol Continue
        }
        else
        {
            continueButton.interactable = false; // Matikan/Gelapkan jika belum ada save
        }
    }

    // 🔴 DIPANGGIL OLEH TOMBOL 'NEW GAME'
    public void StartNewGame()
    {
        // 1. Hapus data save lama agar benar-benar dari nol
        PlayerPrefs.DeleteAll(); 
        
        // 2. Set fase cerita ke awal
        if (StoryManager.Instance != null) {
            StoryManager.Instance.currentPhase = StoryManager.StoryPhase.Arrival_Airstrip;
        }

        // 3. Putar Cutscene Arrival, lalu otomatis load Waisai (atau scene Airstrip)
        CutsceneTransitionManager.Instance.StartPortraitCutscene(arrivalPortraits, "02_Waisai"); 
    }

    // 🟢 DIPANGGIL OLEH TOMBOL 'CONTINUE'
    public void ContinueGame()
    {
        // 1. Ambil nama scene terakhir yang disimpan (Default ke Waisai jika error)
        string sceneToLoad = PlayerPrefs.GetString("SavedScene", "02_Waisai");

        // 2. Ambil data fase cerita terakhir yang disimpan
        if (StoryManager.Instance != null) {
            StoryManager.Instance.currentPhase = (StoryManager.StoryPhase)PlayerPrefs.GetInt("SavedStoryPhase", 0);
        }

        // 3. LANGSUNG LOAD SCENE, LEWATI CUTSCENE!
        SceneManager.LoadScene(sceneToLoad);
    }

    public void QuitGame()
    {
        Debug.Log("Game Keluar!");
        Application.Quit();
    }
}