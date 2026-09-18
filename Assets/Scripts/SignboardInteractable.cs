using UnityEngine;
using TMPro;

public class SignboardInteractable : MonoBehaviour, IInteractable
{
    [Header("Content")]
    [SerializeField] private string infoTitle;
    [TextArea][SerializeField] private string infoDescription;

    [Header("UI Reference")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TextMeshProUGUI titleTextUI;
    [SerializeField] private TextMeshProUGUI descTextUI;

    private bool isOpen = false;

    public void Interact()
    {
        isOpen = !isOpen;

        if (infoPanel != null)
        {
            infoPanel.SetActive(isOpen);
            if (isOpen)
            {
                if (titleTextUI != null) titleTextUI.text = infoTitle;
                if (descTextUI != null) descTextUI.text = infoDescription;
            }
        }
    }

    public string GetPromptText() => isOpen ? "Tutup Info" : "Baca Informasi";

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && isOpen)
        {
            isOpen = false;
            if (infoPanel != null) infoPanel.SetActive(false);
        }
    }
}