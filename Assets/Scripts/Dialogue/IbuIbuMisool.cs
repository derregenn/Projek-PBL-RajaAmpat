using UnityEngine;
using System;

public class IbuIbuMisool : MonoBehaviour
{
    [SerializeField] private Dialogue dialogueSystem;

    // Menyamakan struktur enum speaker dengan Dialogue.cs
    private enum DialogueSpeaker { NPC, Player }

    [Header("Naskah Dialog Point 7 (Minta Tolong)")]
    [TextArea(2, 3)][SerializeField] private string[] dialogPoint7_MintaTolong;
    [SerializeField] private DialogueSpeaker[] speakerPoint7_MintaTolong;

    [Header("Naskah Dialog Point 9 (Terima Kasih)")]
    [TextArea(2, 3)][SerializeField] private string[] dialogPoint9_TerimaKasih;
    [SerializeField] private DialogueSpeaker[] speakerPoint9_TerimaKasih;

    public void OnInteractWithIbuIbu()
    {
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);

        // Point 7: Diminta tolong tolong Pari Manta
        if (stage == 6)
        {
            StartDynamicDialogue(dialogPoint7_MintaTolong, speakerPoint7_MintaTolong, () =>
            {
                QuestController.Instance?.ProgressObjective("misool_free_manta", 1);
                PlayerPrefs.SetInt("Misool_QuestStage", 7); // Menyelam bebaskan Manta
                PlayerPrefs.Save();
            });
        }
        // Point 9: Lapor Manta sudah bebas
        else if (stage == 8)
        {
            StartDynamicDialogue(dialogPoint9_TerimaKasih, speakerPoint9_TerimaKasih, () =>
            {
                // Trigger Fade, Hilangkan Ibu-ibu & Panggil Tour Guide
                MisoolLandSceneManager.Instance?.TriggerPoint9FadeAndTourGuide();
            });
        }
    }

    /// <summary>
    /// Fungsi injeksi dinamis untuk mengatur baris dialog beserta siapa penuturnya
    /// ke dalam komponen Dialogue yang ter-enkapsulasi.
    /// </summary>
    private void StartDynamicDialogue(string[] lines, DialogueSpeaker[] speakers, Action onComplete)
    {
        if (dialogueSystem == null)
        {
            Debug.LogWarning("IbuIbuMisool: Dialogue System belum di-assign di Inspector.", this);
            return;
        }

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("IbuIbuMisool: Naskah dialog kosong atau belum diisi di Inspector.", this);
            return;
        }

        var type = typeof(Dialogue);

        // Inject Naskah
        var fieldLines = type.GetField("DialogueLines", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldLines == null)
        {
            Debug.LogWarning("IbuIbuMisool: Field DialogueLines tidak ditemukan pada Dialogue.", this);
            return;
        }
        fieldLines.SetValue(dialogueSystem, lines);

        // Inject Penutur (NPC / Player)
        var fieldSpeakers = type.GetField("DialogueLineSpeakers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldSpeakers == null)
        {
            Debug.LogWarning("IbuIbuMisool: Field DialogueLineSpeakers tidak ditemukan pada Dialogue.", this);
            return;
        }
        fieldSpeakers.SetValue(dialogueSystem, ConvertToIntArray(speakers));

        // Inject Event Akhir Dialog
        var fieldCallback = type.GetField("onDialogueCompleteCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldCallback == null)
        {
            Debug.LogWarning("IbuIbuMisool: Field onDialogueCompleteCallback tidak ditemukan pada Dialogue.", this);
            return;
        }
        fieldCallback.SetValue(dialogueSystem, onComplete);

        // Jalankan dialog utama tanpa argumen agar membaca variabel yang baru di-inject
        dialogueSystem.StartDialogue();
    }

    // Helper untuk mengonversi Enum lokal ke tipe Enum internal milik Dialogue.cs
    private Array ConvertToIntArray(DialogueSpeaker[] localSpeakers)
    {
        if (localSpeakers == null) return null;

        Type targetEnumType = typeof(Dialogue).GetNestedType("DialogueSpeaker", System.Reflection.BindingFlags.NonPublic);
        if (targetEnumType == null)
        {
            Debug.LogWarning("IbuIbuMisool: Enum DialogueSpeaker tidak ditemukan di dalam Dialogue.", this);
            return null;
        }

        if (localSpeakers == null) return null;

        Array targetArray = Array.CreateInstance(targetEnumType, localSpeakers.Length);
        for (int i = 0; i < localSpeakers.Length; i++)
        {
            targetArray.SetValue(Enum.ToObject(targetEnumType, (int)localSpeakers[i]), i);
        }
        return targetArray;
    }
}