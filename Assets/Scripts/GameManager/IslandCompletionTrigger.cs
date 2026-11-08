using UnityEngine;

public class IslandCompletionTrigger : MonoBehaviour
{
    [Header("Pilih Pulau yang Akan Diselesaikan")]
    public IslandName islandToComplete;

    /// <summary>
    /// Panggil fungsi ini dari event On Dialogue End () Supir Perahu / Tour Guide
    /// </summary>
    public void CompleteThisIsland()
    {
        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.CompleteIsland(islandToComplete);
        }
        else
        {
            Debug.LogWarning("[IslandCompletionTrigger] GamePhaseManager tidak ditemukan, menyimpan ke PlayerPrefs langsung.");
            PlayerPrefs.SetInt("Island_" + (int)islandToComplete, 1);
            PlayerPrefs.Save();
        }

        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.CompleteIsland((int)islandToComplete);
        }
    }

    public void CompleteIslandByName(string islandNameStr)
    {
        if (System.Enum.TryParse(islandNameStr, true, out IslandName parsed))
        {
            islandToComplete = parsed;
            CompleteThisIsland();
        }
        else
        {
            Debug.LogWarning($"[IslandCompletionTrigger] Nama pulau '{islandNameStr}' tidak valid.");
        }
    }
}
