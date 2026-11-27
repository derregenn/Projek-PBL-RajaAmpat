using UnityEngine;
using TMPro;

public class ProgressUI : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text progressText;

    private void Start()
    {
        if (GamePhaseManager.Instance != null)
        {
            // Update teks saat UI ini pertama kali dimunculkan
            UpdateText(GamePhaseManager.Instance.GetCurrentPercentage());

            // Dengarkan perubahan persentase secara real-time
            GamePhaseManager.Instance.OnPercentageUpdated.AddListener(UpdateText);
        }
    }

    private void UpdateText(float percentage)
    {
        if (progressText != null)
        {
            progressText.text = $"Progress: {percentage}%";
        }
    }

    private void OnDestroy()
    {
        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.OnPercentageUpdated.RemoveListener(UpdateText);
        }
    }
}