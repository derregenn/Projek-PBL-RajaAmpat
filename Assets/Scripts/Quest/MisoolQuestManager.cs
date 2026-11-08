using UnityEngine;

public class MisoolQuestManager : MonoBehaviour
{
    private static MisoolQuestManager _instance;
    public static MisoolQuestManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MisoolQuestManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MisoolQuestManager");
                    _instance = go.AddComponent<MisoolQuestManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private const string STAGE_KEY = "Misool_QuestStage";
    private const string COTS_KEY = "Misool_CotsCount";
    private const string EXIT_KEY = "Misool_LastExitPoint";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public int CurrentStage
    {
        get => PlayerPrefs.GetInt(STAGE_KEY, 0);
        private set
        {
            PlayerPrefs.SetInt(STAGE_KEY, value);
            PlayerPrefs.Save();
        }
    }

    public void AdvanceStageTo(int newStage)
    {
        CurrentStage = newStage;
        Debug.Log($"<color=green>[MisoolQuestManager] Quest Stage diperbarui ke: {newStage}</color>");

        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.SetActiveIsland(IslandType.Misool);
            if (MasterQuestManager.Instance.CurrentStage != newStage)
            {
                MasterQuestManager.Instance.AdvanceStageTo(newStage);
            }
        }

        if (MisoolLandSceneManager.Instance != null)
        {
            MisoolLandSceneManager.Instance.RefreshLandState();
        }
    }

    public int CotsCount => PlayerPrefs.GetInt(COTS_KEY, 0);

    public void AddCotsCount()
    {
        int count = CotsCount + 1;
        PlayerPrefs.SetInt(COTS_KEY, count);
        PlayerPrefs.Save();
        Debug.Log($"[MisoolQuestManager] Bulu Seribu terkumpul: {count}/3");

        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.AddBuluSeribu();
        }

        if (count >= 3)
        {
            AdvanceStageTo(5);
        }
    }

    public void ResetCotsCount()
    {
        PlayerPrefs.SetInt(COTS_KEY, 0);
        PlayerPrefs.Save();
        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.ResetCollectibleCount();
        }
    }

    public void SetLastExitPoint(string exitPointName)
    {
        PlayerPrefs.SetString(EXIT_KEY, exitPointName);
        PlayerPrefs.Save();
        if (MasterQuestManager.Instance != null)
        {
            MasterQuestManager.Instance.SetLastExitPoint(exitPointName);
        }
    }

    public string ConsumeLastExitPoint()
    {
        if (MasterQuestManager.Instance != null)
        {
            return MasterQuestManager.Instance.ConsumeLastExitPoint();
        }
        string exit = PlayerPrefs.GetString(EXIT_KEY, "DEFAULT");
        PlayerPrefs.SetString(EXIT_KEY, "DEFAULT");
        PlayerPrefs.Save();
        return exit;
    }

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
