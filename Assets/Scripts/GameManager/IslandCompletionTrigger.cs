using UnityEngine;
using UnityEngine.SceneManagement;

public class IslandCompletionTrigger : MonoBehaviour
{
    [Header("Pilih Pulau yang Akan Diselesaikan")]
    public IslandName islandToComplete;

    /// <summary>
    /// Panggil fungsi ini dari event On Dialogue End () Supir Perahu / Tour Guide
    /// </summary>
    public void CompleteThisIsland()
    {
        IslandName targetIsland = QuestJournalManager.GetIslandFromSceneName(SceneManager.GetActiveScene().name) ?? islandToComplete;
        islandToComplete = targetIsland;

        if (!QuestJournalManager.CanCompleteIslandOrOpenMap(targetIsland))
        {
            Debug.Log($"[IslandCompletionTrigger] Pulau {targetIsland} belum menyelesaikan semua quest-nya; penyelesaian pulau ditahan.");
            return;
        }

        if (QuestController.Instance != null)
        {
            QuestController.Instance.CompleteAllActiveQuests();
        }

        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.CompleteIsland(targetIsland);
        }
        else
        {
            Debug.LogWarning("[IslandCompletionTrigger] GamePhaseManager tidak ditemukan, menyimpan ke PlayerPrefs langsung.");
            PlayerPrefs.SetInt("Island_" + (int)targetIsland, 1);
            PlayerPrefs.Save();
        }

        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.CompleteIsland(targetIsland);
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
