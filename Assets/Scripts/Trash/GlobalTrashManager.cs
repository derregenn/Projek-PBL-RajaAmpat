using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistem terpusat untuk melacak seluruh sampah di semua pulau.
/// DontDestroyOnLoad — satu instance hidup sepanjang sesi game.
///
/// CARA KERJA:
///   - Setiap objek sampah (TrashPickup) mendaftarkan dirinya lewat RegisterTrash().
///   - Saat diambil, sampah memanggil CollectTrash(trashID) → disimpan ke PlayerPrefs.
///   - Di scene berikutnya, sampah yang sudah diambil tidak akan muncul (auto-disable di Start).
/// </summary>
public class GlobalTrashManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static GlobalTrashManager _instance;
    public static GlobalTrashManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GlobalTrashManager>();
                if (_instance == null)
                {
                    var go = new GameObject("GlobalTrashManager");
                    _instance = go.AddComponent<GlobalTrashManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    // =========================================================
    // SAVE KEYS
    // =========================================================

    private const string KeyCollectedSet = "GTM_CollectedIDs";   // ID yang sudah diambil (pipa "|" sebagai pemisah)
    private const string KeyTotalInGame = "GTM_TotalInGame";    // Total sampah terdaftar di game ini
    private const string KeyTotalCollected = "GTM_TotalCollected"; // Total yang sudah dikumpulkan
    private const string KeyFreeRoamMode = "GTM_FreeRoamMode";   // 1 = mode free roam aktif

    // =========================================================
    // STATE RUNTIME
    // =========================================================

    private HashSet<string> collectedIDs = new HashSet<string>();
    private HashSet<string> registeredIDs = new HashSet<string>();
    private int totalInGameCount = 0;   // Diisi dari Inspector masing-masing scene via RegisterTotal()

    // =========================================================
    // EVENTS
    // =========================================================

    [Header("Events")]
    public UnityEvent<int, int> OnTrashCollected;    // (collected, total)
    public UnityEvent OnAllTrashCollected;           // Ketika 100% tercapai
    public UnityEvent OnFreeRoamActivated;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

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

        LoadFromPrefs();
    }

    // =========================================================
    // LOAD / SAVE
    // =========================================================

    private void LoadFromPrefs()
    {
        collectedIDs.Clear();
        string raw = PlayerPrefs.GetString(KeyCollectedSet, "");
        if (!string.IsNullOrEmpty(raw))
        {
            foreach (string id in raw.Split('|'))
            {
                if (!string.IsNullOrWhiteSpace(id))
                    collectedIDs.Add(id.Trim());
            }
        }

        totalInGameCount = PlayerPrefs.GetInt(KeyTotalInGame, 0);

        Debug.Log($"[GlobalTrashManager] Dimuat. Dikumpulkan: {collectedIDs.Count} / {totalInGameCount}");
    }

    private void SaveToPrefs()
    {
        PlayerPrefs.SetString(KeyCollectedSet, string.Join("|", collectedIDs));
        PlayerPrefs.SetInt(KeyTotalCollected, collectedIDs.Count);
        PlayerPrefs.SetInt(KeyTotalInGame, totalInGameCount);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Dipanggil oleh MainMenu saat New Game dimulai.
    /// </summary>
    public void ResetAll()
    {
        collectedIDs.Clear();
        registeredIDs.Clear();
        totalInGameCount = 0;

        PlayerPrefs.DeleteKey(KeyCollectedSet);
        PlayerPrefs.DeleteKey(KeyTotalInGame);
        PlayerPrefs.DeleteKey(KeyTotalCollected);
        PlayerPrefs.DeleteKey(KeyFreeRoamMode);
        PlayerPrefs.Save();

        Debug.LogWarning("[GlobalTrashManager] Seluruh data sampah direset untuk New Game.");
    }

    // =========================================================
    // REGISTRATION API (dipanggil oleh TrashPickup)
    // =========================================================

    /// <summary>
    /// Daftarkan sebuah objek sampah ke sistem global.
    /// Kembalikan true jika sampah BELUM diambil (harus tetap aktif).
    /// Kembalikan false jika sampah SUDAH diambil (objek harus nonaktif).
    /// </summary>
    public bool RegisterTrash(string trashID)
    {
        if (string.IsNullOrWhiteSpace(trashID))
        {
            Debug.LogWarning("[GlobalTrashManager] TrashID kosong, tidak bisa didaftarkan.");
            return true; // Biarkan aktif agar tidak error silent
        }

        if (!registeredIDs.Contains(trashID))
        {
            registeredIDs.Add(trashID);
        }

        return !collectedIDs.Contains(trashID); // true = belum diambil = tetap aktif
    }

    /// <summary>
    /// Daftarkan jumlah total sampah untuk pulau/scene tertentu.
    /// Panggil dari GlobalTrashSceneConfig.Awake().
    /// </summary>
    public void RegisterTotalForScene(string sceneKey, int count)
    {
        string prefKey = "GTM_Scene_" + sceneKey;
        // Simpan jika belum pernah tercatat (mencegah penghitungan ganda saat scene reload)
        if (PlayerPrefs.GetInt(prefKey, 0) == 0 && count > 0)
        {
            PlayerPrefs.SetInt(prefKey, count);
            totalInGameCount += count;
            PlayerPrefs.SetInt(KeyTotalInGame, totalInGameCount);
            PlayerPrefs.Save();
            Debug.Log($"[GlobalTrashManager] Scene '{sceneKey}' mendaftarkan {count} sampah. Total game: {totalInGameCount}");
        }
    }

    // =========================================================
    // COLLECTION API
    // =========================================================

    /// <summary>
    /// Catat sampah sebagai "sudah diambil".
    /// Kembalikan false jika sudah diambil sebelumnya (duplikat).
    /// </summary>
    public bool CollectTrash(string trashID)
    {
        if (string.IsNullOrWhiteSpace(trashID)) return false;

        if (collectedIDs.Contains(trashID))
        {
            Debug.Log($"[GlobalTrashManager] '{trashID}' sudah diambil sebelumnya, diabaikan.");
            return false;
        }

        collectedIDs.Add(trashID);
        SaveToPrefs();

        int collected = collectedIDs.Count;
        int total = Mathf.Max(totalInGameCount, registeredIDs.Count);

        Debug.Log($"<color=cyan>[GlobalTrashManager] Sampah diambil: '{trashID}' ({collected}/{total})</color>");

        OnTrashCollected?.Invoke(collected, total);

        // Update field lama (TrashInteractable.totalCollected) agar kompatibel
        TrashInteractable.totalCollected = collected;
        PlayerPrefs.SetInt("TotalTrashCollected", collected);
        PlayerPrefs.Save();

        // Cek 100%
        if (total > 0 && collected >= total)
        {
            Debug.Log("<color=gold>[GlobalTrashManager] SEMUA SAMPAH TERKUMPUL! 100%!</color>");
            OnAllTrashCollected?.Invoke();
        }

        return true;
    }

    // =========================================================
    // QUERY API
    // =========================================================

    public bool IsCollected(string trashID) => collectedIDs.Contains(trashID);

    public int TotalCollected => collectedIDs.Count;

    public int TotalInGame => Mathf.Max(totalInGameCount, registeredIDs.Count);

    public float CollectionPercentage
    {
        get
        {
            int total = TotalInGame;
            if (total <= 0) return 0f;
            return (float)TotalCollected / total * 100f;
        }
    }

    public bool IsAllTrashCollected()
    {
        int total = TotalInGame;
        return total > 0 && TotalCollected >= total;
    }

    // =========================================================
    // FREE ROAM MODE
    // =========================================================

    public bool IsFreeRoamMode => PlayerPrefs.GetInt(KeyFreeRoamMode, 0) == 1;

    public void ActivateFreeRoamMode()
    {
        PlayerPrefs.SetInt(KeyFreeRoamMode, 1);
        PlayerPrefs.Save();
        Debug.Log("[GlobalTrashManager] FREE ROAM MODE Diaktifkan.");
        OnFreeRoamActivated?.Invoke();
    }

    public void DeactivateFreeRoamMode()
    {
        PlayerPrefs.SetInt(KeyFreeRoamMode, 0);
        PlayerPrefs.Save();
    }
}
