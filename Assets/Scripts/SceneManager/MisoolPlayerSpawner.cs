using UnityEngine;

public class MisoolPlayerSpawner : MonoBehaviour
{
    [Header("Referensi Player")]
    [SerializeField] private Transform playerTransform;

    [Header("Titik Spawn Coordinate")]
    [SerializeField] private Transform defaultSpawnPoint; // Pantai Awal
    [SerializeField] private Transform pierSpawnPoint;    // Tangga Dermaga (Dari Laut Utama)
    [SerializeField] private Transform beachSpawnPoint;   // Pesisir Ujung (Dari Laut Manta)

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        RepositionPlayer();
    }

    private void RepositionPlayer()
    {
        if (playerTransform == null) return;

        // Ambil penanda lokasi terakhir lalu reset dari memori temporary
        string lastExit = MasterQuestManager.Instance.ConsumeLastExitPoint();

        if (lastExit == "SEA_MAIN")
        {
            if (pierSpawnPoint != null) playerTransform.position = pierSpawnPoint.position;
            Debug.Log("[Spawner] Player ditempatkan di Dermaga.");
        }
        else if (lastExit == "SEA_MANTA")
        {
            if (beachSpawnPoint != null) playerTransform.position = beachSpawnPoint.position;
            Debug.Log("[Spawner] Player ditempatkan di Pesisir Ujung.");
        }
        else
        {
            if (defaultSpawnPoint != null) playerTransform.position = defaultSpawnPoint.position;
        }
    }
}