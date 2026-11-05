using UnityEngine;
using UnityEngine.SceneManagement;

public class SeaToLandTeleporter : MonoBehaviour
{
    [Header("Pengaturan Scene Tujuan")]
    [SerializeField] private string landSceneName = "04_Misool";

    public void ReturnToLand()
    {
        int currentStage = MasterQuestManager.Instance.CurrentStage;

        // 1. EVALUASI STAGE & SIMPAN PROGRES SEBELUM PINDAH
        if (currentStage == 2)
        {
            // Selesai Monolog Laut -> Lanjut Stage 3 (Bulu Seribu)
            MasterQuestManager.Instance.AdvanceStageTo(3);
            MasterQuestManager.Instance.SetLastExitPoint("SEA_MAIN");
        }
        else if (currentStage == 4 && MasterQuestManager.Instance.CollectibleCount >= 3)
        {
            // Selesai Ambil 3 Bulu Seribu -> Lanjut Stage 5 (Serahkan ke Petugas)
            MasterQuestManager.Instance.AdvanceStageTo(5);
            MasterQuestManager.Instance.SetLastExitPoint("SEA_MAIN");
        }
        else if (currentStage == 7)
        {
            // Selesai Rescue Pari Manta -> Lanjut Stage 8 (Lapor Ibu-Ibu)
            MasterQuestManager.Instance.AdvanceStageTo(8);
            MasterQuestManager.Instance.SetLastExitPoint("SEA_MANTA");
        }
        else
        {
            // Jika kembali ke darat sebelum quest laut selesai
            MasterQuestManager.Instance.SetLastExitPoint("SEA_MAIN");
        }

        // Data otomatis ter-save ke PlayerPrefs via MasterQuestManager!

        // 2. LOAD SCENE DARAT
        SceneManager.LoadScene(landSceneName);
    }
}