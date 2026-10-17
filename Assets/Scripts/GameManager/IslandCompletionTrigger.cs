using UnityEngine;

public class IslandCompletionTrigger : MonoBehaviour
{
    [Header("Pilih Pulau yang Akan Diselesaikan")]
    public IslandName islandToComplete;

    /// <summary>
    /// Panggil fungsi ini dari event On Dialogue End () Supir Perahu
    /// </summary>
    public void CompleteThisIsland()
    {
        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.CompleteIsland(islandToComplete);
        }
        else
        {
            Debug.LogWarning("GamePhaseManager tidak ditemukan! Pastikan mulai game dari Main Menu.");
        }
    }
}