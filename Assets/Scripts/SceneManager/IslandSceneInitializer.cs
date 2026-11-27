using UnityEngine;

public class IslandSceneInitializer : MonoBehaviour
{
    [Header("Pilih Pulau Untuk Scene Ini")]
    public IslandType currentIsland;

    private void Start()
    {
        if (MasterQuestManager.Instance != null)
        {
            // Beritahu manager bahwa kita sekarang berada di pulau ini
            MasterQuestManager.Instance.SetActiveIsland(currentIsland);
        }
    }
}