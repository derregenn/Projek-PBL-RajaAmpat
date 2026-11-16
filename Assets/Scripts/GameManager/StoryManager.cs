using UnityEngine;
using TMPro;

public class StoryManager : MonoBehaviour
{
    private static StoryManager _instance;
    public static StoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<StoryManager>();
            }
            return _instance;
        }
        set => _instance = value;
    }

    // Fase Cerita sesuai urutan
    public enum StoryPhase
    {
        Arrival_Airstrip, // Waisai
        Pianemo,
        Misool,
        Arborek,
        Sawinggrai,
        Friwen,
        Gag_Climax
    }

    public StoryPhase currentPhase = StoryPhase.Arrival_Airstrip;

    [Header("Progress Tracking")]
    // Array boolean untuk mencatat pulau mana saja yang sudah diselesaikan
    // Indeks: 0=Waisai, 1=Pianemo, 2=Misool, 3=Arborek, 4=Sawinggrai, 5=Friwen, 6=Gag
    public bool[] islandsCompleted = new bool[7]; 

    [Header("Journal UI")]
    public TMP_Text progressPercentageText; // Text (TMP) di Travel Journal (Misal: "0%")

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        LoadProgress();
        UpdateJournalUI();
    }

    public static int IslandNameToStoryIndex(IslandName island)
    {
        switch (island)
        {
            case IslandName.Waisai: return 0;
            case IslandName.Piaynemo: return 1;
            case IslandName.Misool: return 2;
            case IslandName.Arborek: return 3;
            case IslandName.Sawinggrai: return 4;
            case IslandName.Friwen: return 5;
            case IslandName.Gag: return 6;
            default: return -1;
        }
    }

    public void CompleteIsland(IslandName island)
    {
        int idx = IslandNameToStoryIndex(island);
        if (idx >= 0)
        {
            CompleteIsland(idx);
        }
    }

    public void CompleteIsland(int islandIndex)
    {
        if (islandIndex >= 0 && islandIndex < islandsCompleted.Length)
        {
            islandsCompleted[islandIndex] = true;
            SaveProgress();
            UpdateJournalUI();
            
            Debug.Log($"[StoryManager] Pulau ke-{islandIndex} Selesai! Progress: {CalculateProgressPercentage()}%");
        }
    }

    public int CalculateProgressPercentage()
    {
        int completedCount = 0;
        foreach (bool completed in islandsCompleted)
        {
            if (completed) completedCount++;
        }

        float percentage = ((float)completedCount / islandsCompleted.Length) * 100f;
        return Mathf.RoundToInt(percentage);
    }

    public void UpdateJournalUI()
    {
        if (progressPercentageText != null)
        {
            if (progressPercentageText.transform.parent != null && progressPercentageText.transform.parent.name.Contains("ProgressUI"))
            {
                progressPercentageText.transform.parent.gameObject.SetActive(false);
            }
            progressPercentageText.text = CalculateProgressPercentage() + "%";
        }
    }

    public void SaveProgress()
    {
        PlayerPrefs.SetInt("SavedStoryPhase", (int)currentPhase);
        
        for (int i = 0; i < islandsCompleted.Length; i++)
        {
            if (islandsCompleted[i])
            {
                PlayerPrefs.SetInt("Story_Island_" + i, 1);
            }
            else
            {
                PlayerPrefs.SetInt("Story_Island_" + i, 0);
            }
        }
        PlayerPrefs.Save();
    }

    public void LoadProgress()
    {
        if (PlayerPrefs.HasKey("SavedStoryPhase"))
        {
            currentPhase = (StoryPhase)PlayerPrefs.GetInt("SavedStoryPhase");
        }

        for (int i = 0; i < islandsCompleted.Length; i++)
        {
            islandsCompleted[i] = PlayerPrefs.GetInt("Story_Island_" + i, 0) == 1; 
        }
    }
    
    public void ResetProgress()
    {
        for (int i = 0; i < islandsCompleted.Length; i++)
        {
            islandsCompleted[i] = false;
            PlayerPrefs.DeleteKey("Story_Island_" + i);
        }
        currentPhase = StoryPhase.Arrival_Airstrip;
        SaveProgress();
        UpdateJournalUI();
        Debug.LogWarning("[StoryManager] Seluruh progress StoryManager telah direset.");
    }
}
