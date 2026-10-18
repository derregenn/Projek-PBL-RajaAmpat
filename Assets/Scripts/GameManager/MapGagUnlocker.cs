using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MapGagUnlocker : MonoBehaviour
{
    private Button gagButton;

    private void Awake()
    {
        gagButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        // Setiap kali peta dibuka, cek apakah Gag sudah boleh diakses
        if (GamePhaseManager.Instance != null)
        {
            gagButton.interactable = GamePhaseManager.Instance.GetIsGagUnlocked();
        }
    }
}