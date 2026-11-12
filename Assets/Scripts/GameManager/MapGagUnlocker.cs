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
        if (gagButton == null) gagButton = GetComponent<Button>();

        bool isUnlocked = false;

        // 1. Cek dari GamePhaseManager
        if (GamePhaseManager.Instance != null && GamePhaseManager.Instance.GetIsGagUnlocked())
        {
            isUnlocked = true;
        }

        // 2. Cek dari PlayerPrefs
        if (PlayerPrefs.GetInt("GPM_GagUnlocked", 0) == 1 || PlayerPrefs.GetInt("GagUnlocked", 0) == 1)
        {
            isUnlocked = true;
        }

        // 3. Cek apakah pemain sudah menyelesaikan pulau-pulau lain atau quest yang cukup
        string completedStr = PlayerPrefs.GetString("QJM_CompletedQuests", "");
        int completedCount = completedStr.Split(new char[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
        if (completedCount >= 3 ||
            PlayerPrefs.GetInt("Visited_06_Sawinggrai", 0) == 1 ||
            PlayerPrefs.GetInt("Visited_07_Friwen", 0) == 1 ||
            PlayerPrefs.GetInt("Visited_04_Misool", 0) == 1)
        {
            isUnlocked = true;
        }

        if (isUnlocked && GamePhaseManager.Instance != null && !GamePhaseManager.Instance.GetIsGagUnlocked())
        {
            GamePhaseManager.Instance.UnlockGag(true);
        }

        if (gagButton != null)
        {
            gagButton.interactable = isUnlocked;
        }
    }
}