using UnityEngine;
using UnityEngine.SceneManagement;

public class SeaToLandTeleporter : MonoBehaviour
{
    [SerializeField] private string landSceneName = "04_Misool";

    // Fungsi ini dipanggil saat pemain menyentuh trigger batas atas laut 
    // atau menekan tombol kembali ke kapal
    public void ReturnToLand()
    {
        // Ambil stage saat ini dari MasterQuestManager
        int currentStage = MasterQuestManager.Instance.CurrentStage;

        // 1. UPDATE STAGE BERDASARKAN ALUR MISOOL
        if (currentStage == 2)
        {
            // Pemain baru selesai Monolog Laut, kembali untuk bicara dengan Petugas (Poin 3)
            MasterQuestManager.Instance.AdvanceStageTo(3);
            MasterQuestManager.Instance.SetLastExitPoint("SEA_MAIN"); // Agar spawn di Dermaga
        }
        else if (currentStage == 4 && MasterQuestManager.Instance.CollectibleCount >= 3)
        {
            // Pemain sudah ambil 3 Bulu Seribu, kembali untuk lapor ke Petugas (Poin 5)
            MasterQuestManager.Instance.AdvanceStageTo(5);
            MasterQuestManager.Instance.SetLastExitPoint("SEA_MAIN"); // Agar spawn di Dermaga
        }
        else if (currentStage == 7)
        {
            // Pemain selesai menolong Pari Manta, kembali lapor ke Ibu-Ibu (Poin 8)
            MasterQuestManager.Instance.AdvanceStageTo(8);
            MasterQuestManager.Instance.SetLastExitPoint("SEA_MANTA"); // Agar spawn di Pesisir Ujung
        }
        else
        {
            // Jika pemain kembali ke darat tapi quest belum selesai (misal stage 4 tapi hama baru 1)
            // Jangan naikkan stage, cukup simpan exit point saja.
            MasterQuestManager.Instance.SetLastExitPoint("SEA_MAIN");
        }

        // 2. PINDAH KE SCENE DARAT
        // Jika kamu memakai UniversalFader, panggil fade di sini. 
        // Jika tidak, gunakan SceneManager langsung:
        SceneManager.LoadScene(landSceneName);
    }
}
// Contoh trigger otomatis saat player berenang ke permukaan