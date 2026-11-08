using System.Collections;
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
            if (playerObj == null) playerObj = GameObject.Find("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        RepositionPlayer();
        StartCoroutine(VerifyPositionNextFrame());
    }

    private IEnumerator VerifyPositionNextFrame()
    {
        yield return null;
        // Jaga-jaga jika kamera Cinemachine atau fisika Rigidbody memerlukan snap ulang
        if (playerTransform != null)
        {
            Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private void RepositionPlayer()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (playerTransform == null) return;

        // Ambil penanda lokasi terakhir lalu reset dari memori temporary
        string lastExit = "DEFAULT";
        if (MasterQuestManager.Instance != null)
        {
            lastExit = MasterQuestManager.Instance.ConsumeLastExitPoint();
        }
        else
        {
            lastExit = PlayerPrefs.GetString("Misool_LastExitPoint", "DEFAULT");
            PlayerPrefs.SetString("Misool_LastExitPoint", "DEFAULT");
            PlayerPrefs.Save();
        }

        Transform targetPoint = defaultSpawnPoint;

        if (lastExit == "SEA_MAIN")
        {
            if (pierSpawnPoint != null) targetPoint = pierSpawnPoint;
            Debug.Log("[MisoolPlayerSpawner] Menempatkan Player di Dermaga (SEA_MAIN).");
        }
        else if (lastExit == "SEA_MANTA")
        {
            if (beachSpawnPoint != null) targetPoint = beachSpawnPoint;
            Debug.Log("[MisoolPlayerSpawner] Menempatkan Player di Pesisir Ujung (SEA_MANTA).");
        }
        else
        {
            if (defaultSpawnPoint != null) targetPoint = defaultSpawnPoint;
            Debug.Log("[MisoolPlayerSpawner] Menempatkan Player di Spawn Default.");
        }

        if (targetPoint != null)
        {
            Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.position = targetPoint.position;
            }
            playerTransform.position = targetPoint.position;
        }
    }
}
