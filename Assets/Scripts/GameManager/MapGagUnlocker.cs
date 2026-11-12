using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MapGagUnlocker : MonoBehaviour
{
    private Button gagButton;
    private Image buttonImage;

    [Header("Referensi Teleport")]
    [Tooltip("Opsional: Drag objek yang punya komponen MapTeleport ke sini (otomatis dicari jika kosong)")]
    public MapTeleport mapTeleport;

    [Header("Sprites Peta Gag")]
    public Sprite unlockedSprite;
    public Sprite lockedSprite;

    private void Awake()
    {
        gagButton = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        CariReferensiMapTeleport();
    }

    private void CariReferensiMapTeleport()
    {
        // 1. Cek jika MapTeleport menempel di tombol ini sendiri
        if (mapTeleport == null)
            mapTeleport = GetComponent<MapTeleport>();

        // 2. Jika tidak ada di tombol ini, cari di dalam scene (misal di MapManager / Canvas)
        if (mapTeleport == null)
            mapTeleport = FindFirstObjectByType<MapTeleport>();
    }

    private void OnEnable()
    {
        if (gagButton == null) gagButton = GetComponent<Button>();
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (mapTeleport == null) CariReferensiMapTeleport();

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
            gagButton.onClick.RemoveAllListeners();
            if (isUnlocked)
            {
                gagButton.onClick.AddListener(() =>
                {
                    if (mapTeleport != null)
                    {
                        mapTeleport.PindahKePulau("10_Gag");
                    }
                    else
                    {
                        Debug.LogError("[MapGagUnlocker] Komponen MapTeleport tidak ditemukan di tombol maupun di scene!");
                    }
                });
            }
        }

        if (buttonImage != null)
        {
            if (isUnlocked && unlockedSprite != null)
            {
                buttonImage.sprite = unlockedSprite;
            }
            else if (!isUnlocked && lockedSprite != null)
            {
                buttonImage.sprite = lockedSprite;
            }
        }
    }
}