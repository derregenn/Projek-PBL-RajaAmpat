using UnityEngine;

public class MisoolQuestManager : MonoBehaviour
{
    public static MisoolQuestManager Instance { get; private set; }

    // String Key terpusat untuk mencegah typo
    private const string STAGE_KEY = "Misool_QuestStage";
    private const string COTS_KEY = "Misool_CotsCount";
    private const string EXIT_KEY = "Misool_LastExitPoint";

    private void Awake()
    {
        // Pola Singleton & Persistent Object antar-scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ==========================================
    // 1. MANAJEMEN QUEST STAGE (0 - 9)
    // ==========================================

    /// <summary>
    /// Membaca stage quest saat ini dari PlayerPrefs
    /// </summary>
    public int CurrentStage
    {
        get => PlayerPrefs.GetInt(STAGE_KEY, 0);
        private set
        {
            PlayerPrefs.SetInt(STAGE_KEY, value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Fungsi utama untuk menaikkan/mengubah stage quest
    /// </summary>
    public void AdvanceStageTo(int newStage)
    {
        CurrentStage = newStage;
        Debug.Log($"<color=green>[MisoolQuestManager] Quest Stage diperbarui ke: {newStage}</color>");

        // Otomatis memperbarui tampilan NPC di scene darat jika LandManager aktif
        if (MisoolLandSceneManager.Instance != null)
        {
            MisoolLandSceneManager.Instance.RefreshLandState();
        }
    }

    // ==========================================
    // 2. MANAJEMEN HAMA BULU SERIBU (POIN 5)
    // ==========================================

    public int CotsCount => PlayerPrefs.GetInt(COTS_KEY, 0);

    public void AddCotsCount()
    {
        int count = CotsCount + 1;
        PlayerPrefs.SetInt(COTS_KEY, count);
        PlayerPrefs.Save();
        Debug.Log($"[MisoolQuestManager] Bulu Seribu terkumpul: {count}/3");

        // Jika sudah terkumpul 3, otomatis naik ke Stage 5
        if (count >= 3)
        {
            AdvanceStageTo(5);
        }
    }

    public void ResetCotsCount()
    {
        PlayerPrefs.SetInt(COTS_KEY, 0);
        PlayerPrefs.Save();
    }

    // ==========================================
    // 3. MANAJEMEN SPAWN POINT PLAYER
    // ==========================================

    public void SetLastExitPoint(string exitPointName)
    {
        PlayerPrefs.SetString(EXIT_KEY, exitPointName);
        PlayerPrefs.Save();
        Debug.Log($"[MisoolQuestManager] Exit Point disimpan: {exitPointName}");
    }

    public string ConsumeLastExitPoint()
    {
        string exit = PlayerPrefs.GetString(EXIT_KEY, "DEFAULT");
        PlayerPrefs.SetString(EXIT_KEY, "DEFAULT");
        PlayerPrefs.Save();
        return exit;
    }

    // ==========================================
    // 4. UTILITY DEBUGGING (KHUSUS UNITY EDITOR)
    // ==========================================

    [ContextMenu("Reset All Misool Quest Data")]
    public void ResetAllQuestData()
    {
        PlayerPrefs.DeleteKey(STAGE_KEY);
        PlayerPrefs.DeleteKey(COTS_KEY);
        PlayerPrefs.DeleteKey(EXIT_KEY);
        PlayerPrefs.Save();
        Debug.Log("<color=yellow>[MisoolQuestManager] Seluruh Data Quest Misool Direset ke Stage 0!</color>");

        if (MisoolLandSceneManager.Instance != null)
        {
            MisoolLandSceneManager.Instance.RefreshLandState();
        }
    }
}