using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Cutscene Assets")]
    public Sprite[] arrivalPortraits; // Gambar portrait pesawat
    
    [TextArea(2, 5)] // Membuat kotak input teks di Inspector jadi lebih luas
    public string[] arrivalDialogues; // Teks cerita/dialogue untuk masing-masing gambar

    [Header("UI Buttons")]
    public Button continueButton;

    void Start()
    {
        if (PlayerPrefs.HasKey("SavedScene"))
        {
            continueButton.interactable = true;
        }
        else
        {
            continueButton.interactable = false;
        // Cek apakah ada data save sebelumnya. 
        // (Misal kamu simpan nama scene terakhir di PlayerPrefs dengan key "SavedScene")
        if (PlayerPrefs.HasKey("SavedScene"))
        {
            continueButton.interactable = true; // Nyalakan tombol Continue
        }
        else
        {
            continueButton.interactable = false; // Matikan/Gelapkan jika belum ada save
>>>>>>> parent of bccb1f1 (save state to game phase, continue button)
        }
    }

    public void StartNewGame()
    {
<<<<<<< HEAD
        PlayerPrefs.DeleteAll(); 
        
=======
        // 1. Hapus data save lama agar benar-benar dari nol
        PlayerPrefs.DeleteAll(); 
        
        // 2. Set fase cerita ke awal
>>>>>>> parent of bccb1f1 (save state to game phase, continue button)
        if (StoryManager.Instance != null) {
            StoryManager.Instance.currentPhase = StoryManager.StoryPhase.Arrival_Airstrip;
        }

<<<<<<< HEAD
        // Mengirimkan gambar DAN teks naskah ke CutsceneTransitionManager
        CutsceneTransitionManager.Instance.StartPortraitCutscene(arrivalPortraits, arrivalDialogues, "02_Waisai"); 
=======
        // 3. Putar Cutscene Arrival, lalu otomatis load Waisai (atau scene Airstrip)
        CutsceneTransitionManager.Instance.StartPortraitCutscene(arrivalPortraits, "02_Waisai"); 
>>>>>>> parent of bccb1f1 (save state to game phase, continue button)
    }

    public void ContinueGame()
    {
<<<<<<< HEAD
        string sceneToLoad = PlayerPrefs.GetString("SavedScene");

=======
        // 1. Ambil nama scene terakhir yang disimpan (Default ke Waisai jika error)
        string sceneToLoad = PlayerPrefs.GetString("SavedScene", "02_Waisai");

        // 2. Ambil data fase cerita terakhir yang disimpan
>>>>>>> parent of bccb1f1 (save state to game phase, continue button)
        if (StoryManager.Instance != null) {
            StoryManager.Instance.currentPhase = (StoryManager.StoryPhase)PlayerPrefs.GetInt("SavedStoryPhase", 0);
        }

<<<<<<< HEAD
=======
        // 3. LANGSUNG LOAD SCENE, LEWATI CUTSCENE!
>>>>>>> parent of bccb1f1 (save state to game phase, continue button)
        SceneManager.LoadScene(sceneToLoad);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit!");
        Application.Quit();
    }
}