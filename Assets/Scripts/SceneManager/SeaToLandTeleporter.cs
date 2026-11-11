using UnityEngine;
using UnityEngine.SceneManagement;

public class SeaToLandTeleporter : MonoBehaviour
{
    [Header("Pengaturan Scene Tujuan")]
    [SerializeField] private string landSceneName = "04_Misool";
    [SerializeField] private IslandType currentIsland;


    /// <summary>
    /// Menyiapkan data stage dan exit point sebelum scene darat dimuat (tanpa memicu LoadScene langsung).
    /// Mencegah race condition ketika dipanggil bersamaan dengan Screen Fade.
    /// </summary>
    public void PrepareReturn()
    {
        MasterQuestManager mqm = MasterQuestManager.Instance;
        if (mqm == null)
        {
            Debug.LogWarning("[SeaToLandTeleporter] MasterQuestManager tidak ditemukan!");
            return;
        }

        int currentStage = mqm.CurrentStage;
        int count = mqm.CollectibleCount;
        string exitPoint = "SEA_MAIN";

        if (currentIsland == IslandType.Sawinggrai || mqm.CurrentIsland == IslandType.Sawinggrai)
        {
            if (currentStage == 4)
                mqm.AdvanceStageTo(5);

            if (currentStage == 4 || currentStage == 5)
                exitPoint = "SAWINGGRAI_PIER";
        }
        // Stage transition logic for Misool.
        else if (currentStage == 2)
        {
            mqm.AdvanceStageTo(3);
        }
        else if ((currentStage >= 3 && currentStage <= 4 && count >= 3) || currentStage == 5)
        {
            mqm.AdvanceStageTo(5);
        }
        else if (currentStage == 8)
        {
            exitPoint = "SEA_MANTA";
        }

        mqm.SetLastExitPoint(exitPoint);
        Debug.Log($"[SeaToLandTeleporter] Data transisi disiapkan. Stage: {mqm.CurrentStage}, ExitPoint: {exitPoint}");
    }

    /// <summary>
    /// Dipanggil jika transisi ingin langsung mengeksekusi perpindahan scene.
    /// </summary>
    public void ReturnToLand()
    {
        PrepareReturn();
        SceneManager.LoadScene(landSceneName);
    }
}
