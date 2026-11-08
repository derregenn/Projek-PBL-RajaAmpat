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
        if (dialogueSystem == null) dialogueSystem = FindFirstObjectByType<Dialogue>();

        MasterQuestManager mqm = MasterQuestManager.Instance;
        if (mqm == null)
        {
            Debug.LogWarning("[PetugasMisool] MasterQuestManager tidak ditemukan!");
            return;
        }

        mqm.SetActiveIsland(IslandType.Misool);
        int stage = mqm.CurrentStage;
        Debug.Log($"[PetugasMisool] Interaksi dimulai. Stage Misool saat ini: {stage}");

        if (stage == 1 || stage == 2)
        {
            // Stage 1: Petugas menyuruh Player menyelam pertama kali (Monolog)
            dialogueSystem.StartCustomDialogue(dialogSuruhMenyelam, () =>
            {
                ProgressMisoolObjective("petugas_konservasi", "misool_start_dive");
                mqm.AdvanceStageTo(2); // Naik ke Stage 2 (Siap menyelam)
                if (MisoolQuestManager.Instance != null) MisoolQuestManager.Instance.AdvanceStageTo(2);
            });
        }
        else if (stage == 3)
        {
            // Stage 3: Setelah monolog laut selesai, Petugas minta bantuan 3 Bulu Seribu
            dialogueSystem.StartCustomDialogue(dialogMintaBantuan, () =>
            {
                ProgressMisoolObjective("petugas_report", "misool_collect_cots");
                mqm.ResetCollectibleCount();
                mqm.AdvanceStageTo(4); // Naik ke Stage 4 (Ambil Bulu Seribu)
                if (MisoolQuestManager.Instance != null)
                {
                    MisoolQuestManager.Instance.ResetCotsCount();
                    MisoolQuestManager.Instance.AdvanceStageTo(4);
                }
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
                ProgressMisoolObjective("pesisir_pantai", "misool_go_to_manta");
                mqm.AdvanceStageTo(6); // Naik ke Stage 6 (Aktifkan Ibu-Ibu)
                if (MisoolQuestManager.Instance != null) MisoolQuestManager.Instance.AdvanceStageTo(6);
            });
        }
    }

    private void ProgressMisoolObjective(string primaryID, string fallbackID)
    {
        if (QuestController.Instance == null) return;

        // Coba primary objective terlebih dahulu, jika tidak ada baru gunakan fallback
        if (!string.IsNullOrEmpty(primaryID))
        {
            QuestController.Instance.ProgressObjective(primaryID, 1);
        }
        if (!string.IsNullOrEmpty(fallbackID))
        {
            QuestController.Instance.ProgressObjective(fallbackID, 1);
        }
    }
}
