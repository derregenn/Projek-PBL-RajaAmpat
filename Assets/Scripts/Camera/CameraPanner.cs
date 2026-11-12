using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using Unity.Cinemachine;

public class CameraPanner : MonoBehaviour
{
    [Header("Pengaturan Kamera")]
    [SerializeField] private GameObject objectiveCamera; // Masukkan ObjectiveCamera di sini
    [SerializeField] private float viewDuration = 2.5f;  // Berapa lama kamera menyorot objektif

    [Header("Trigger Collider (Opsional)")]
    [SerializeField] private bool triggerOnPlayerEnter = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Event Lanjutan (Opsional)")]
    // Event ini berguna jika kamu ingin menjalankan quest/dialog lain SETELAH kamera balik ke player
    public UnityEvent onPanFinished;

    private bool isPanning;

    private void Start()
    {
        if (objectiveCamera == null)
        {
            objectiveCamera = GameObject.Find("ObjectiveCamera");
        }

        // Pastikan kamera objektif mati saat awal mulai
        if (objectiveCamera != null)
        {
            CinemachineCamera cmCam = objectiveCamera.GetComponent<CinemachineCamera>();
            if (cmCam != null) cmCam.Priority.Value = 0;
            objectiveCamera.SetActive(false);
        }
    }

    // Fungsi ini yang dipanggil saat trigger tersentuh atau dipanggil via UnityEvent
    public void ShowObjectivePan()
    {
        if (isPanning)
        {
            return;
        }

        isPanning = true;
        StartCoroutine(PanCameraRoutine());
    }

    // PERBAIKAN: Menggunakan OnTriggerEnter2D untuk Fisika 2D (Box Collider 2D)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnPlayerEnter && (other.CompareTag(playerTag) || other.gameObject.name.Contains("Player")))
        {
            ShowObjectivePan();
        }
    }

    private IEnumerator PanCameraRoutine()
    {
        if (objectiveCamera == null)
        {
            objectiveCamera = GameObject.Find("ObjectiveCamera");
        }

        CinemachineCamera cmCam = objectiveCamera != null ? objectiveCamera.GetComponent<CinemachineCamera>() : null;

        // 1. Nyalakan kamera objektif dengan prioritas 30 (Cinemachine otomatis melakukan panning mulus ke sini)
        if (objectiveCamera != null)
        {
            if (cmCam != null) cmCam.Priority.Value = 30;
            objectiveCamera.SetActive(true);
        }

        // 2. Tunggu pemain melihat objektif tersebut
        yield return new WaitForSeconds(viewDuration);

        // 3. Matikan kamera objektif dan kembalikan prioritas ke 0 (Cinemachine otomatis panning balik ke Player)
        if (cmCam != null) cmCam.Priority.Value = 0;
        if (objectiveCamera != null)
        {
            objectiveCamera.SetActive(false);
        }

        // 4. Tunggu sebentar agar animasi panning balik selesai
        yield return new WaitForSeconds(1.5f);

        // 5. Jalankan event lanjutan jika ada
        onPanFinished?.Invoke();
        isPanning = false;
    }
}
