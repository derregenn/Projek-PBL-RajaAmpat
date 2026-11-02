using UnityEngine;

public class PetugasMisool : MonoBehaviour
{
    [SerializeField] private Dialogue dialogueSystem;

    [Header("Naskah Dialog Stage 1 (Suruh Menyelam)")]
    [TextArea(2, 4)][SerializeField] private string[] dialogSuruhMenyelam;

    [Header("Naskah Dialog Stage 3 (Minta Bantuan)")]
    [TextArea(2, 4)][SerializeField] private string[] dialogMintaBantuan;

    [Header("Naskah Dialog Stage 4 (Sedang Kerja)")]
    [TextArea(2, 4)][SerializeField] private string[] dialogSedangKerja;

    [Header("Naskah Dialog Stage 5 (Terima Kasih)")]
    [TextArea(2, 4)][SerializeField] private string[] dialogTerimaKasih;

    public void InteractWithOfficer()
    {
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);

        if (stage == 1)
        {
            dialogueSystem.StartCustomDialogue(dialogSuruhMenyelam, () =>
            {
                QuestController.Instance?.ProgressObjective("misool_start_dive", 1);
                PlayerPrefs.SetInt("Misool_QuestStage", 2); // Siap menyelam pertama
                PlayerPrefs.Save();
            });
        }
        else if (stage == 3)
        {
            dialogueSystem.StartCustomDialogue(dialogMintaBantuan, () =>
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
            dialogueSystem.StartCustomDialogue(dialogSedangKerja, null);
        }
        else if (stage == 5)
        {
            dialogueSystem.StartCustomDialogue(dialogTerimaKasih, () =>
            {
                QuestController.Instance?.ProgressObjective("misool_go_to_manta", 1);
                PlayerPrefs.SetInt("Misool_QuestStage", 6); // Lanjut ke Quest 3 (Ibu-Ibu)
                PlayerPrefs.Save();

                MisoolLandSceneManager.Instance?.RefreshLandState();
            });
        }
    }
}