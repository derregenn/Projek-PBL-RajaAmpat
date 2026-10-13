using UnityEngine;
using UnityEngine.Events;

// Daftar pulau untuk mempermudah identifikasi
public enum IslandName
{
    Waisai,
    Piaynemo,
    Arborek,
    Misool,
    Sawinggrai,
    Friwen,
    Gag
}

public class GamePhaseManager : MonoBehaviour
{
    public static GamePhaseManager Instance;

    [Header("Island Completion Status")]
    public bool isWaisaiCompleted = false;      // 10%
    public bool isPiaynemoCompleted = false;    // 15%
    public bool isArborekCompleted = false;     // 20%
    public bool isMisoolCompleted = false;      // 15%
    public bool isSawinggraiCompleted = false;  // 15%
    public bool isFriwenCompleted = false;      // 10%
    public bool isGagCompleted = false;         // 15%

    [Header("Progress Info (Read Only)")]
    [SerializeField] private float currentCompletionPercentage = 0f;
    [SerializeField] private bool isGagUnlocked = false;

    [Header("Events")]
    public UnityEvent<float> OnPercentageUpdated;
    public UnityEvent OnGagUnlocked; // Event untuk membuka tombol Gag di UI Peta

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CalculatePercentage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Panggil fungsi ini saat player akan naik perahu untuk meninggalkan pulau.
    /// Contoh dari Inspector: GamePhaseManager.CompleteIsland (Pilih Piaynemo)
    /// </summary>
    public void CompleteIsland(IslandName island)
    {
        switch (island)
        {
            case IslandName.Waisai: isWaisaiCompleted = true; break;
            case IslandName.Piaynemo: isPiaynemoCompleted = true; break;
            case IslandName.Arborek: isArborekCompleted = true; break;
            case IslandName.Misool: isMisoolCompleted = true; break;
            case IslandName.Sawinggrai: isSawinggraiCompleted = true; break;
            case IslandName.Friwen: isFriwenCompleted = true; break;
            case IslandName.Gag: isGagCompleted = true; break;
        }

        Debug.Log($"[GamePhaseManager] Pulau {island} diselesaikan!");
        CalculatePercentage();
    }

    private void CalculatePercentage()
    {
        float total = 0f;

        if (isWaisaiCompleted) total += 10f;
        if (isPiaynemoCompleted) total += 15f;
        if (isArborekCompleted) total += 20f;
        if (isMisoolCompleted) total += 15f;
        if (isSawinggraiCompleted) total += 15f;
        if (isFriwenCompleted) total += 10f;

        // Cek apakah sudah 85% untuk membuka Gag
        if (total >= 85f && !isGagUnlocked)
        {
            isGagUnlocked = true;
            Debug.Log("[GamePhaseManager] 85% Tercapai! Pulau Gag Terbuka!");
            OnGagUnlocked?.Invoke(); 
        }

        // Tambahkan persen Gag jika sudah selesai (Ending)
        if (isGagCompleted) total += 15f; 

        currentCompletionPercentage = total;
        OnPercentageUpdated?.Invoke(currentCompletionPercentage);
        
        if (currentCompletionPercentage >= 100f)
        {
            Debug.Log("[GamePhaseManager] 100% Tercapai! Mainkan Ending!");
        }
    }

    public float GetCurrentPercentage()
    {
        return currentCompletionPercentage;
    }
    
    public bool GetIsGagUnlocked()
    {
        return isGagUnlocked;
    }
}