using UnityEngine;

public class SawinggraiPlayerSpawner : MonoBehaviour
{
    [Header("Referensi Karakter")]
    [Tooltip("Masukkan GameObject Player utama (MC) di sini")]
    [SerializeField] private GameObject player;

    [Header("Titik Spawn (Spawn Points)")]
    [Tooltip("Titik awal saat pemain pertama kali datang ke Sawinggrai dari World Map")]
    [SerializeField] private Transform defaultSpawnPoint;

    [Tooltip("Titik spawn saat pemain kembali dari laut (Quest 2 - Bawah Rumah Warga)")]
    [SerializeField] private Transform pierSpawnPoint;

    // Tambahkan titik spawn lain jika ada transisi scene lain (misalnya dari dalam rumah/hutan)
    // [SerializeField] private Transform forestSpawnPoint;

    private void Start()
    {
        if (player == null)
        {
            // Coba cari otomatis jika kolom player lupa diisi di Inspector
            player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                Debug.LogError("[SawinggraiPlayerSpawner] Player tidak ditemukan! Pastikan tag Player sudah diatur.");
                return;
            }
        }

        if (MasterQuestManager.Instance == null)
        {
            Debug.LogWarning("[SawinggraiPlayerSpawner] MasterQuestManager tidak ditemukan. Menggunakan Default Spawn.");
            MovePlayerTo(defaultSpawnPoint);
            return;
        }

        // Ambil data pintu keluar terakhir dari MasterQuestManager
        string exitPoint = MasterQuestManager.Instance.ConsumeLastExitPoint();
        Debug.Log($"<color=yellow>[SawinggraiPlayerSpawner] Memuat posisi pemain dari Exit Point: {exitPoint}</color>");

        // Tentukan posisi berdasarkan kode exit point
        switch (exitPoint)
        {
            case "SAWINGGRAI_PIER":
                MovePlayerTo(pierSpawnPoint);
                break;

            case "DEFAULT":
            default:
                MovePlayerTo(defaultSpawnPoint);
                break;
        }
    }

    private void MovePlayerTo(Transform targetPoint)
    {
        if (targetPoint != null)
        {
            player.transform.position = targetPoint.position;
            Debug.Log($"[SawinggraiPlayerSpawner] Pemain dipindahkan ke: {targetPoint.name}");
        }
        else
        {
            Debug.LogWarning("[SawinggraiPlayerSpawner] Titik spawn target belum dimasukkan ke Inspector!");
        }
    }
}