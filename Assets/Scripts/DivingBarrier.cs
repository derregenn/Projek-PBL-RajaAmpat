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
        // Abaikan collider trigger internal milik player (seperti GroundCheck / WallCheck)
        if (collision.isTrigger) return;

        if (collision.CompareTag("Player"))
        {
            PlayerDiving playerDiving = collision.GetComponentInParent<PlayerDiving>();
            if (playerDiving != null && !playerDiving.isDiving)
            {
                if (warningPanel != null)
                {
                    warningPanel.SetActive(true);
                }

                playerDiving.StartDiving();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Abaikan collider trigger internal milik player (seperti GroundCheck / WallCheck)
        if (collision.isTrigger) return;

        if (collision.CompareTag("Player"))
        {
            PlayerDiving playerDiving = collision.GetComponentInParent<PlayerDiving>();
            if (playerDiving != null && playerDiving.isDiving)
            {
                if (warningPanel != null)
                {
                    warningPanel.SetActive(false);
                }

                playerDiving.StopDiving();
            }
        }
    }
}