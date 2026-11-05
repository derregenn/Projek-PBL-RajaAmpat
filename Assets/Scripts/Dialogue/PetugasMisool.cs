using UnityEngine;

public class PetugasMisool : MonoBehaviour
{
    [SerializeField] private Dialogue dialogueSystem;

    [Header("Naskah Dialog Stage 1 (Suruh Menyelam Pertama)")]
    [TextArea(2, 4)][SerializeField] private string[] dialogSuruhMenyelam;

    [Header("Naskah Dialog Stage 3 (Minta Bantuan Bulu Seribu)")]
    [TextArea(2, 4)][SerializeField] private string[] dialogMintaBantuan;

    [Header("Naskah Dialog Stage 4 (Sedang Kumpulkan Hama)")]
    [TextArea(2, 4)][SerializeField] private string[] dialogSedangKerja;

    [Header("Naskah Dialog Stage 5 (Terima Kasih Hama Selesai)")]
    [TextArea(2, 4)][SerializeField] private string[] dialogTerimaKasih;

    /// <summary>
    /// Dipanggil otomatis oleh Dialogue.cs saat Player menekan 'E' di dekat Petugas
    /// </summary>
    public void InteractWithOfficer()
    {
        if (dialogueSystem == null) dialogueSystem = GetComponent<Dialogue>();
        if (dialogueSystem == null || MisoolQuestManager.Instance == null) return;

        int stage = MisoolQuestManager.Instance.CurrentStage;

        if (stage == 1 || stage == 2)
        {
            // Stage 1: Petugas menyuruh Player menyelam pertama kali (Monolog)
            dialogueSystem.StartCustomDialogue(dialogSuruhMenyelam, () =>
            {
                QuestController.Instance?.ProgressObjective("misool_start_dive", 1);
                MisoolQuestManager.Instance.AdvanceStageTo(2); // Naik ke Stage 2 (Siap menyelam)
            });
        }
        else if (stage == 3)
        {
            // Stage 3: Setelah monolog laut selesai, Petugas minta bantuan 3 Bulu Seribu
            dialogueSystem.StartCustomDialogue(dialogMintaBantuan, () =>
            {
                QuestController.Instance?.ProgressObjective("misool_collect_cots", 1);
                MisoolQuestManager.Instance.ResetCotsCount();
                MisoolQuestManager.Instance.AdvanceStageTo(4); // Naik ke Stage 4 (Ambil Bulu Seribu)
            });
        }
        else if (stage == 4)
        {
            // Stage 4: Pemain bicara lagi saat Bulu Seribu belum terkumpul 3
            dialogueSystem.StartCustomDialogue(dialogSedangKerja, null);
        }
        else if (stage == 5)
        {
            // Stage 5: 3 Bulu Seribu sudah terkumpul, Petugas berterima kasih & mengarahkan ke Ibu-Ibu
            dialogueSystem.StartCustomDialogue(dialogTerimaKasih, () =>
            {
                QuestController.Instance?.ProgressObjective("misool_go_to_manta", 1);
                MisoolQuestManager.Instance.AdvanceStageTo(6); // Naik ke Stage 6 (Aktifkan Ibu-Ibu)
            });
        }
    }
}