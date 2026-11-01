using System;
using UnityEngine;

public class PetugasMisool : MonoBehaviour
{
    [SerializeField] private Dialogue dialogueSystem;

    // Karena Dialogue.cs menggunakan enum internal DialogueSpeaker, 
    // kita gunakan pendekatan yang sama agar sejalan dengan sistemnya.
    private enum DialogueSpeaker { NPC, Player }

    [Header("Naskah Dialog Stage 1 (Suruh Menyelam)")]
    [TextArea(2, 4)][SerializeField] private string[] dialogSuruhMenyelam;
    [SerializeField] private DialogueSpeaker[] speakerSuruhMenyelam;

    [Header("Naskah Dialog Stage 3 (Minta Bantuan)")]
    [TextArea(2, 4)][SerializeField] private string[] dialogMintaBantuan;
    [SerializeField] private DialogueSpeaker[] speakerMintaBantuan;

    [Header("Naskah Dialog Stage 4 (Sedang Kerja)")]
    [TextArea(2, 4)][SerializeField] private string[] dialogSedangKerja;
    [SerializeField] private DialogueSpeaker[] speakerSedangKerja;

    [Header("Naskah Dialog Stage 5 (Terima Kasih)")]
    [TextArea(2, 4)][SerializeField] private string[] dialogTerimaKasih;
    [SerializeField] private DialogueSpeaker[] speakerTerimaKasih;

    public void InteractWithOfficer()
    {
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);

        if (stage == 1)
        {
            StartDynamicDialogue(dialogSuruhMenyelam, speakerSuruhMenyelam, () =>
            {
                QuestController.Instance?.ProgressObjective("misool_start_dive", 1);
                PlayerPrefs.SetInt("Misool_QuestStage", 2); // Siap menyelam pertama
                PlayerPrefs.Save();
            });
        }
        else if (stage == 3)
        {
            StartDynamicDialogue(dialogMintaBantuan, speakerMintaBantuan, () =>
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
            StartDynamicDialogue(dialogSedangKerja, speakerSedangKerja, null);
        }
        else if (stage == 5)
        {
            StartDynamicDialogue(dialogTerimaKasih, speakerTerimaKasih, () =>
            {
                QuestController.Instance?.ProgressObjective("misool_go_to_manta", 1);
                PlayerPrefs.SetInt("Misool_QuestStage", 6); // Lanjut ke Quest 3 (Ibu-Ibu)
                PlayerPrefs.Save();

                // Tambahkan refresh UI/State darat jika dibutuhkan
                MisoolLandSceneManager.Instance?.RefreshLandState();
            });
        }
    }

    /// <summary>
    /// Fungsi untuk mengatur variabel di komponen Dialogue (termasuk siapa yang bicara) secara dinamis
    /// sebelum memulai percakapan.
    /// </summary>
    private void StartDynamicDialogue(string[] lines, DialogueSpeaker[] speakers, System.Action onComplete)
    {
        // 1. Validasi
        if (dialogueSystem == null || lines == null || lines.Length == 0) return;

        // 2. Setel nilai naskah menggunakan Reflection karena variabelnya di set Private di Dialogue.cs
        // Pendekatan alternatif tanpa perlu mengubah Dialogue.cs
        var type = typeof(Dialogue);

        var fieldLines = type.GetField("DialogueLines", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldLines != null) fieldLines.SetValue(dialogueSystem, lines);

        var fieldSpeakers = type.GetField("DialogueLineSpeakers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldSpeakers != null) fieldSpeakers.SetValue(dialogueSystem, ConvertToIntArray(speakers));

        var fieldCallback = type.GetField("onDialogueCompleteCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldCallback != null) fieldCallback.SetValue(dialogueSystem, onComplete);

        // 3. Mulai Dialog
        dialogueSystem.StartDialogue();
    }

    // Helper untuk mengubah tipe Enum lokal ke Enum internal Dialogue.cs 
    private Array ConvertToIntArray(DialogueSpeaker[] localSpeakers)
    {
        if (localSpeakers == null) return null;

        // Kita paksa konversi ke tipe integer/enum dasar untuk di-pass ke variabel private
        Type targetEnumType = typeof(Dialogue).GetNestedType("DialogueSpeaker", System.Reflection.BindingFlags.NonPublic);
        if (targetEnumType == null) return null;

        Array targetArray = Array.CreateInstance(targetEnumType, localSpeakers.Length);
        for (int i = 0; i < localSpeakers.Length; i++)
        {
            targetArray.SetValue((int)localSpeakers[i], i);
        }
        return targetArray;
    }
}