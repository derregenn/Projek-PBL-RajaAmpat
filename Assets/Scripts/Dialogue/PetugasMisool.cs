using UnityEngine;

public class PetugasMisool : MonoBehaviour
{
    [SerializeField] private Dialogue dialogueSystem;

    [Header("Naskah Dialog Petugas")]
    [TextArea(2, 4)][SerializeField] private string[] dialogSuruhMenyelam; // Stage 1
    [TextArea(2, 4)][SerializeField] private string[] dialogMintaBantuan;  // Stage 3
    [TextArea(2, 4)][SerializeField] private string[] dialogSedangKerja;   // Stage 4 (Belum 3/3)
    [TextArea(2, 4)][SerializeField] private string[] dialogTerimaKasih;   // Stage 5

    public void InteractWithOfficer()
    {
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);

        if (stage == 1)
        {
            dialogueSystem.StartDialogue(dialogSuruhMenyelam, () =>
            {
                QuestController.Instance?.ProgressObjective("misool_start_dive", 1);
                PlayerPrefs.SetInt("Misool_QuestStage", 2); // Siap menyelam pertama
                PlayerPrefs.Save();
            });
        }
        else if (stage == 3)
        {
            dialogueSystem.StartDialogue(dialogMintaBantuan, () =>
            {
                QuestController.Instance?.ProgressObjective("misool_collect_cots", 1);
                PlayerPrefs.SetInt("Misool_QuestStage", 4); // Disuruh ambil Bulu Seribu
                PlayerPrefs.SetInt("Misool_CotsCount", 0);  // Reset counter hama
                PlayerPrefs.Save();
            });
        }
        else if (stage == 4)
        {
            // Jika diajak bicara saat hama belum terkumpul 3
            dialogueSystem.StartDialogue(dialogSedangKerja, null);
        }
        else if (stage == 5)
        {
            dialogueSystem.StartDialogue(dialogTerimaKasih, () =>
            {
                QuestController.Instance?.ProgressObjective("misool_go_to_manta", 1);
                PlayerPrefs.SetInt("Misool_QuestStage", 6); // Lanjut ke Quest 3 (Ibu-Ibu)
                PlayerPrefs.Save();
            });
        }
    }
}
