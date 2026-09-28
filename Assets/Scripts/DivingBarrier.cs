using UnityEngine;

public class DivingBarrier : MonoBehaviour
{
    [Header("UI Feedback")]
    public GameObject warningPanel;

    private void Start()
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (warningPanel != null)
            {
                warningPanel.SetActive(true);
            }

            PlayerDiving playerDiving = collision.GetComponent<PlayerDiving>();
            if (playerDiving != null)
            {
                playerDiving.StartDiving();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (warningPanel != null)
            {
                warningPanel.SetActive(false);
            }

            PlayerDiving playerDiving = collision.GetComponent<PlayerDiving>();
            if (playerDiving != null)
            {
                playerDiving.StopDiving();
            }
        }
    }
}