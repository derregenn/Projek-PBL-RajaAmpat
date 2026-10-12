using UnityEngine;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    // Enum untuk mengunci fase cerita secara rapi
    public enum StoryPhase
    {
        NewGame = 0,
        Arrival_Airstrip = 1,
        Waisai_Hub = 2,
        Piaynemo = 3,
        Arborek = 4,
        Misool = 5,
        Sawinggrai = 6,
        Friwen = 7,
        Gag_Climax = 8
    }

    [Header("Current Progress")]
    [Tooltip("Fase cerita yang sedang aktif saat ini.")]
    public StoryPhase currentPhase = StoryPhase.NewGame;

    private void Awake()
    {
        // Pola Singleton & Auto-Destroy jika ada duplikat saat reload scene
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); // Memastikan objek berada di Root Hierarchy
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Memajukan cerita ke fase berikutnya secara otomatis.
    /// </summary>
    public void AdvanceStoryPhase()
    {
        int maxPhases = System.Enum.GetValues(typeof(StoryPhase)).Length - 1;

        if ((int)currentPhase < maxPhases)
        {
            currentPhase++;
            Debug.Log($"[StoryManager] Progress Berhasil Naik ke: {currentPhase}");
        }
        else
        {
            Debug.LogWarning("[StoryManager] Cerita sudah mencapai fase maksimum (Klimaks)!");
        }
    }

    /// <summary>
    /// Mengeset fase cerita secara spesifik (misal: loncat ke Gag saat pilih Peta).
    /// </summary>
    public void SetStoryPhase(StoryPhase targetPhase)
    {
        currentPhase = targetPhase;
        Debug.Log($"[StoryManager] Fase cerita di-set manual ke: {currentPhase}");
    }

    /// <summary>
    /// Mengembalikan persentase progres cerita global dari 0% hingga 100%.
    /// Dipanggil oleh QuestJournalManager untuk update UI Jurnal.
    /// </summary>
    public float CalculateGlobalProgress()
    {
        int totalPhases = System.Enum.GetValues(typeof(StoryPhase)).Length - 1;
        float percentage = ((float)currentPhase / (float)totalPhases) * 100f;
        return Mathf.Clamp(percentage, 0f, 100f);
    }
}