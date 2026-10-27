using UnityEngine;

public class MonologueTrigger : MonoBehaviour
{
    [Header("Referensi UI Player")]
    [SerializeField] private Dialogue playerDialogue;
    // 1. Tambahkan slot ini untuk menarik DialogueBox
    [SerializeField] private GameObject dialogueBoxObject;

    [Header("Naskah Monolog MC")]
    [TextArea(2, 4)]
    [SerializeField] private string[] mcMonologueLines;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.Log(">>> Player masuk area monolog!");

            if (dialogueBoxObject != null)
            {
                dialogueBoxObject.SetActive(true);
            }

            if (playerDialogue != null)
            {
                playerDialogue.StartDialogue(mcMonologueLines, OnMonologueEnd);
            }
            else
            {
                Debug.LogError(">>> Kolom Player Dialogue di Inspector masih KOSONG!");
            }
        }
    }

    private void OnMonologueEnd()
    {
        // (Isi dengan kode update quest milikmu yang sebelumnya)
        gameObject.SetActive(false);
    }
}