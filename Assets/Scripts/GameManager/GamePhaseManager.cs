using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum IslandName
{
    Waisai, Piaynemo, Arborek, Misool, Sawinggrai, Friwen, Gag
}

public class GamePhaseManager : MonoBehaviour
{
    public static GamePhaseManager Instance;

    [Header("Island Completion Status")]
    public bool isWaisaiCompleted = false;
    public bool isPiaynemoCompleted = false;
    public bool isArborekCompleted = false;
    public bool isMisoolCompleted = false;
    public bool isSawinggraiCompleted = false;
    public bool isFriwenCompleted = false;
    public bool isGagCompleted = false;

    [Header("Progress Info (Read Only)")]
    [SerializeField] private float currentCompletionPercentage = 0f;
    [SerializeField] private bool isGagUnlocked = false;

    [Header("Events")]
    public UnityEvent<float> OnPercentageUpdated;
    public UnityEvent OnGagUnlocked;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Otomatis memuat data save saat game baru dibuka
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

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

        // Tambahkan baris ini di dalam fungsi CompleteIsland() atau saat pemindahan scene:
        PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();

        Debug.Log($"[GamePhaseManager] Pulau {island} diselesaikan!");
        CalculatePercentage();

        // Auto-Save setiap kali pemain menyelesaikan sebuah pulau
        SaveProgress();
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

        if (total >= 85f && !isGagUnlocked)
        {
            isGagUnlocked = true;
            OnGagUnlocked?.Invoke();
        }

        if (isGagCompleted) total += 15f;

        currentCompletionPercentage = total;
        OnPercentageUpdated?.Invoke(currentCompletionPercentage);
    }

    // ==========================================
    // SISTEM SAVE & LOAD (PLAYERPREFS)
    // ==========================================

    public void SaveProgress()
    {
        // Menyimpan status boolean sebagai integer (1 = true, 0 = false)
        PlayerPrefs.SetInt("Save_Waisai", isWaisaiCompleted ? 1 : 0);
        PlayerPrefs.SetInt("Save_Piaynemo", isPiaynemoCompleted ? 1 : 0);
        PlayerPrefs.SetInt("Save_Arborek", isArborekCompleted ? 1 : 0);
        PlayerPrefs.SetInt("Save_Misool", isMisoolCompleted ? 1 : 0);
        PlayerPrefs.SetInt("Save_Sawinggrai", isSawinggraiCompleted ? 1 : 0);
        PlayerPrefs.SetInt("Save_Friwen", isFriwenCompleted ? 1 : 0);
        PlayerPrefs.SetInt("Save_Gag", isGagCompleted ? 1 : 0);

        PlayerPrefs.Save(); // Eksekusi penyimpanan ke perangkat
        Debug.Log("[GamePhaseManager] Game Tersimpan Otomatis!");
    }

    public void LoadProgress()
    {
        // Membaca data dari perangkat, default 0 (false) jika belum pernah main
        isWaisaiCompleted = PlayerPrefs.GetInt("Save_Waisai", 0) == 1;
        isPiaynemoCompleted = PlayerPrefs.GetInt("Save_Piaynemo", 0) == 1;
        isArborekCompleted = PlayerPrefs.GetInt("Save_Arborek", 0) == 1;
        isMisoolCompleted = PlayerPrefs.GetInt("Save_Misool", 0) == 1;
        isSawinggraiCompleted = PlayerPrefs.GetInt("Save_Sawinggrai", 0) == 1;
        isFriwenCompleted = PlayerPrefs.GetInt("Save_Friwen", 0) == 1;
        isGagCompleted = PlayerPrefs.GetInt("Save_Gag", 0) == 1;

        Debug.Log("[GamePhaseManager] Data Save Berhasil Dimuat.");
        CalculatePercentage(); // Kalkulasi ulang persenan setelah data dimuat
    }

    public void ResetProgress()
    {
        // Menghapus semua data save terkait game ini
        PlayerPrefs.DeleteKey("Save_Waisai");
        PlayerPrefs.DeleteKey("Save_Piaynemo");
        PlayerPrefs.DeleteKey("Save_Arborek");
        PlayerPrefs.DeleteKey("Save_Misool");
        PlayerPrefs.DeleteKey("Save_Sawinggrai");
        PlayerPrefs.DeleteKey("Save_Friwen");
        PlayerPrefs.DeleteKey("Save_Gag");

        // Reset variabel di memori
        isWaisaiCompleted = false;
        isPiaynemoCompleted = false;
        isArborekCompleted = false;
        isMisoolCompleted = false;
        isSawinggraiCompleted = false;
        isFriwenCompleted = false;
        isGagCompleted = false;
        isGagUnlocked = false;

        CalculatePercentage();
        Debug.Log("[GamePhaseManager] Progres Direset (New Game)!");
    }

    // ==========================================

    public float GetCurrentPercentage() => currentCompletionPercentage;
    public bool GetIsGagUnlocked() => isGagUnlocked;
}