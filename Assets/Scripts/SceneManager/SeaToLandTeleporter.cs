using UnityEngine;
using UnityEngine.SceneManagement;

public class SeaToLandTeleporter : MonoBehaviour
{
    [Header("Pengaturan Scene Tujuan")]
    [SerializeField] private string landSceneName = "04_Misool";

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

        // 1. EVALUASI STAGE & SIMPAN PROGRES SEBELUM PINDAH
        if (currentStage == 2)
        {
            // Selesai Monolog Laut -> Lanjut Stage 3 (Bulu Seribu)
            mqm.AdvanceStageTo(3);
            mqm.SetLastExitPoint("SEA_MAIN");
        }
        else if (count >= 3 || currentStage == 4 || currentStage == 5)
        {
            // Selesai Ambil 3 Bulu Seribu -> Lanjut Stage 5 (Serahkan ke Petugas)
            mqm.AdvanceStageTo(5);
            mqm.SetLastExitPoint("SEA_MAIN");
        }
        else if (currentStage == 7 || currentStage == 8)
        {
            // Selesai Rescue Pari Manta -> Lanjut Stage 8 (Lapor Ibu-Ibu)
            mqm.AdvanceStageTo(8);
            mqm.SetLastExitPoint("SEA_MANTA");
        }
        else
        {
            // Jika kembali ke darat sebelum quest laut selesai
            mqm.SetLastExitPoint("SEA_MAIN");
        }

        Debug.Log($"[SeaToLandTeleporter] Data transisi disiapkan. Stage: {mqm.CurrentStage}, ExitPoint: SEA_MAIN");
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
