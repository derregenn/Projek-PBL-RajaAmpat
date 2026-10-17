using UnityEngine;
using UnityEngine.SceneManagement;

public class MapTeleport : MonoBehaviour
{
    public void PindahKePulau(string namaSceneTujuan)
    {
        Time.timeScale = 1f; 
        
        SceneManager.LoadScene(namaSceneTujuan);
    }
}