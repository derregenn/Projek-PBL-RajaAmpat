using UnityEngine;
using UnityEngine.SceneManagement;

public class MapTeleport : MonoBehaviour
{
    // Fungsi ini yang dipanggil oleh On Click () tombol di Inspector
    public void PindahKePulau(string sceneName)
    {
Time.timeScale = 1f;

    if (GamePhaseManager.Instance != null)
    {
        GamePhaseManager.Instance.targetIslandName = sceneName;
    }

    PlayerPrefs.SetString("TargetIsland", sceneName);
    PlayerPrefs.Save();

        // 3. Selalu pindah ke Cutscene Perahu dulu
        SceneManager.LoadScene("Cutscene_BoatToIsland");
    }
}