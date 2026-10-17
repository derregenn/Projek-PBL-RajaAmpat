using UnityEngine;
using TMPro; // Untuk update Text UI

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance;

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
        // Setup Singleton & Jangan Hancurkan saat pindah pulau
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadProgress(); // Muat data saat game baru dimulai
        UpdateJournalUI();
    }

    // 🔴 PANGGIL FUNGSI INI SETIAP KALI PEMAIN SELESAI QUEST DI SEBUAH PULAU
    public void CompleteIsland(int islandIndex)
    {
        if (islandIndex >= 0 && islandIndex < islandsCompleted.Length)
        {
            islandsCompleted[islandIndex] = true;
            SaveProgress();
            UpdateJournalUI();
            
            Debug.Log("Pulau ke-" + islandIndex + " Selesai! Progress: " + CalculateProgressPercentage() + "%");
        }
    }

    // Fungsi matematika untuk menghitung persen (Max 100%)
    private int CalculateProgressPercentage()
    {
        int completedCount = 0;
        foreach (bool completed in islandsCompleted)
        {
            if (completed) completedCount++;
        }

        // Rumus: (Jumlah pulau selesai / Total 7 Pulau) * 100
        float percentage = ((float)completedCount / islandsCompleted.Length) * 100f;
        return Mathf.RoundToInt(percentage);
    }

    // Fungsi untuk mengubah angka di UI Travel Journal
    public void UpdateJournalUI()
    {
        if (progressPercentageText != null)
        {
            progressPercentageText.text = CalculateProgressPercentage() + "%";
        }
    }

    // --- SISTEM SAVE / LOAD KHUSUS PROGRESS JURNAL ---
    public void SaveProgress()
    {
        PlayerPrefs.SetInt("SavedStoryPhase", (int)currentPhase);
        
        // Simpan status tiap pulau menjadi 1 (True) atau 0 (False)
        for (int i = 0; i < islandsCompleted.Length; i++)
        {
            PlayerPrefs.SetInt("Island_" + i, islandsCompleted[i] ? 1 : 0);
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
            // Ambil data, defaultnya 0 (belum selesai)
            islandsCompleted[i] = PlayerPrefs.GetInt("Island_" + i, 0) == 1; 
        }
    }
    
    // (Opsional) Panggil ini dari MainMenu.StartNewGame() untuk reset
    public void ResetProgress()
    {
        for (int i = 0; i < islandsCompleted.Length; i++)
        {
            islandsCompleted[i] = false;
        }
        currentPhase = StoryPhase.Arrival_Airstrip;
        SaveProgress();
        UpdateJournalUI();
    }
}