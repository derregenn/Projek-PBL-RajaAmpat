using UnityEngine;

public class MisoolPlayerSpawner : MonoBehaviour
{
    [Header("Referensi Player")]
    [SerializeField] private Transform playerTransform;

    [Header("Titik Spawn Berdasarkan Asal/Stage")]
    [SerializeField] private Transform defaultSpawnPoint;  // Posisi awal di darat
    [SerializeField] private Transform pierSpawnPoint;     // Posisi setelah selesai dari Laut Utama (Dermaga)
    [SerializeField] private Transform beachSpawnPoint;    // Posisi setelah selesai dari Laut Manta (Pesisir Ujung)

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        RepositionPlayer();
    }

    public void RepositionPlayer()
    {
        if (playerTransform == null) return;

        // Ambil penanda lokasi spawn terakhir (jika ada) atau gunakan logika QuestStage
        string lastExitPoint = PlayerPrefs.GetString("Misool_LastExitPoint", "DEFAULT");
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);

        if (lastExitPoint == "SEA_MAIN" || stage == 3 || stage == 5)
        {
            // Jika baru kembali dari Laut Utama (Monolog / Bulu Seribu)
            if (pierSpawnPoint != null)
            {
                playerTransform.position = pierSpawnPoint.position;
                Debug.Log("[PlayerSpawner] Player dipindahkan ke Titik Dermaga.");
            }
        }
        else if (lastExitPoint == "SEA_MANTA" || stage == 8)
        {
            // Jika baru kembali dari Laut Manta (Point 8)
            if (beachSpawnPoint != null)
            {
                playerTransform.position = beachSpawnPoint.position;
                Debug.Log("[PlayerSpawner] Player dipindahkan ke Titik Pesisir Ujung.");
            }
        }
        else
        {
            // Posisi Default Awal Game
            if (defaultSpawnPoint != null)
            {
                playerTransform.position = defaultSpawnPoint.position;
            }
        }

        // Reset penanda setelah digunakan agar tidak selalu teleport ke sana saat restart
        PlayerPrefs.SetString("Misool_LastExitPoint", "DEFAULT");
        PlayerPrefs.Save();
    }
}