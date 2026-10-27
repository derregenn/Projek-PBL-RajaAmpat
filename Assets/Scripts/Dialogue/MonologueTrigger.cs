using UnityEngine;
using UnityEngine.Events;

public class MonologueTrigger : MonoBehaviour
{
    [Header("Referensi UI Player")]
    [SerializeField] private Dialogue playerDialogue;
    [SerializeField] private GameObject dialogueBoxObject;
    [SerializeField] private UnityEvent onDialogueBoxActivated;
    [SerializeField] private UnityEvent onMonologueCompleted;

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
                if (dialogueBoxObject.activeInHierarchy)
                {
                    Debug.Log(">>> Dialogue box sudah muncul (aktif di hierarchy).");
                }
                else
                {
                    Debug.LogWarning(">>> Dialogue box belum muncul karena GameObject atau parent-nya tidak aktif.");
                }
            }
            else
            {
                Debug.LogWarning(">>> Dialogue Box Object belum diisi di Inspector!");
            }

            onDialogueBoxActivated?.Invoke();

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
        onMonologueCompleted?.Invoke();
        gameObject.SetActive(false);
    }
}