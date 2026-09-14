using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin")]
    [SerializeField] private AudioClip coinClip;

    [Header("Quest Objective")]
    [SerializeField] private string objectiveID = "collect_coins";
    [SerializeField] private int amountToAdd = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        // ========================================
        // COIN SYSTEM
        // ========================================

        Player player = collision.GetComponent<Player>();

        if (player != null)
        {
            player.coins += 1;

            if (coinClip != null)
            {
                player.PlaySFX(coinClip);
            }
        }

        // ========================================
        // QUEST SYSTEM
        // ========================================

        if (QuestController.Instance != null)
        {
            QuestController.Instance.ProgressObjective(
                objectiveID,
                amountToAdd
            );
        }
        else
        {
            Debug.LogWarning(
                "Coin: QuestController.Instance tidak ditemukan."
            );
        }

        // ========================================
        // DESTROY COIN
        // ========================================

        Destroy(gameObject);
    }
}