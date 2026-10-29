using UnityEngine;
using UnityEngine.Events;

public class IbuIbuMisool : MonoBehaviour
{
    [SerializeField] private Dialogue dialogueSystem;

    [Header("Naskah Dialog")]
    [TextArea(2, 4)][SerializeField] private string[] dialogMintaTolongManta; // Stage 6
    [TextArea(2, 4)][SerializeField] private string[] dialogTerimaKasihManta;  // Stage 8

    [Header("Quest Selesai")]
    [SerializeField] private UnityEvent onQuest3Completed;

    public void InteractWithIbuIbu()
    {
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);

        if (stage == 6)
        {
            dialogueSystem.StartDialogue(dialogMintaTolongManta, () =>
            {
                QuestController.Instance?.ProgressObjective("misool_free_manta", 1);
                PlayerPrefs.SetInt("Misool_QuestStage", 7); // Disuruh menyelam bebaskan Manta
                PlayerPrefs.Save();
            });
        }
        else if (stage == 8)
        {
            dialogueSystem.StartDialogue(dialogTerimaKasihManta, () =>
            {
                // Selesai dialog terima kasih -> Trigger Fade & Tour Guide
                onQuest3Completed?.Invoke();
            });
        }
    }
}