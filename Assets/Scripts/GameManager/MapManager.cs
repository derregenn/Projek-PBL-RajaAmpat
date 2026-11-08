using UnityEngine;
using UnityEngine.SceneManagement;

public class MapTeleport : MonoBehaviour
{
    // Fungsi ini yang dipanggil oleh On Click () tombol di Inspector
    public void PindahKePulau(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[MapTeleport] sceneName kosong!");
            return;
        }

        Time.timeScale = 1f;

        MapController.targetIslandScene = sceneName;

        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.targetIslandName = sceneName;
        }

        PlayerPrefs.SetString("TargetIsland", sceneName);
        PlayerPrefs.Save();

        Debug.Log($"[MapTeleport] PindahKePulau: '{sceneName}'. Memuat Cutscene_BoatToIsland.");

        // Selalu pindah ke Cutscene Perahu dulu
        SceneManager.LoadScene("Cutscene_BoatToIsland");
    }
}
