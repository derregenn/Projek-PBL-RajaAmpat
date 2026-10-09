using UnityEngine;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance;

    // Enum untuk mengunci fase cerita secara rapi
    public enum StoryPhase { 
        NewGame, 
        Arrival_Airstrip, 
        Waisai_Hub, 
        Piaynemo, 
        Arborek, 
        Misool, 
        Sawinggrai, 
        Friwen, 
        Gag_Climax 
    }

    public StoryPhase currentPhase = StoryPhase.NewGame;

    void Awake()
    {
        if (Instance == null) {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    // Fungsi ini dipanggil setiap pemain selesai 1 pulau
    public void AdvanceStoryPhase()
    {
        currentPhase++;
        CalculateGlobalProgress();
    }

    public float CalculateGlobalProgress()
    {
        // Total ada 8 tahap cerita. Kalkulasi persentase global (Misal: Phase 4 = 50%)
        return ((float)currentPhase / 8f) * 100f; 
    }
}