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
        }
    }

    public void StartNewGame()
    {
        PlayerPrefs.DeleteAll(); 
        
        if (StoryManager.Instance != null) {
            StoryManager.Instance.currentPhase = StoryManager.StoryPhase.Arrival_Airstrip;
        }

        // Mengirimkan gambar DAN teks naskah ke CutsceneTransitionManager
        CutsceneTransitionManager.Instance.StartPortraitCutscene(arrivalPortraits, arrivalDialogues, "02_Waisai"); 
    }

    public void ContinueGame()
    {
        string sceneToLoad = PlayerPrefs.GetString("SavedScene");

        if (StoryManager.Instance != null) {
            StoryManager.Instance.currentPhase = (StoryManager.StoryPhase)PlayerPrefs.GetInt("SavedStoryPhase", 0);
        }

        SceneManager.LoadScene(sceneToLoad);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit!");
        Application.Quit();
    }
}