using System.Diagnostics;
using UnityEngine;

[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public class Coin : MonoBehaviour
{
    public AudioClip coinClip;

    [Header("Quest Objective")]
    [SerializeField] private string objectiveID = "collect_coins"; // Samakan dengan objectiveID di ScriptableObject Quest
    [SerializeField] private int amountToAdd = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                player.coins += 1;
                player.PlaySFX(coinClip);
            }

            // Tambahkan progres quest
            if (QuestController.Instance != null)
            {
                QuestController.Instance.ProgressObjective(
                    objectiveID,
                    amountToAdd
                );
            }
        }
    }

    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}