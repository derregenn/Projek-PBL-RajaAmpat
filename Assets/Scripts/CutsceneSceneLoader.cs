using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneSceneLoader : MonoBehaviour
{
    // Dipanggil saat dialog NPC perahu selesai
    public void FinishCutsceneAndLoadIsland()
    {
        Time.timeScale = 1f;
        
        // Memuat scene pulau yang tersimpan di MapController (contoh: "03_Pianemo")
        if (!string.IsNullOrEmpty(MapController.targetIslandScene))
        {
            SceneManager.LoadScene(MapController.targetIslandScene);
        }
        else
        {
            SceneManager.LoadScene("03_Pianemo"); // Default jika kosong
        }
    }
}