using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class CameraPanner : MonoBehaviour
{
    [Header("Pengaturan Kamera")]
    [SerializeField] private GameObject objectiveCamera; // Masukkan ObjectiveCamera di sini
    [SerializeField] private float viewDuration = 2.5f;  // Berapa lama kamera menyorot objektif

    [Header("Event Lanjutan (Opsional)")]
    // Event ini berguna jika kamu ingin menjalankan quest/dialog lain SETELAH kamera balik ke player
    public UnityEvent onPanFinished; 

    private void Start()
    {
        // Pastikan kamera objektif mati saat awal mulai
        if (objectiveCamera != null)
        {
            objectiveCamera.SetActive(false);
        }
    }

    // Fungsi ini yang akan kita panggil dari OnDialogueEnd atau UnityEvent lainnya
    public void ShowObjectivePan()
    {
        StartCoroutine(PanCameraRoutine());
    }

    private IEnumerator PanCameraRoutine()
    {
        // 1. Nyalakan kamera objektif (Cinemachine otomatis melakukan panning mulus ke sini)
        if (objectiveCamera != null)
        {
            objectiveCamera.SetActive(true);
        }

        // 2. Tunggu pemain melihat objektif tersebut
        yield return new WaitForSeconds(viewDuration);

        // 3. Matikan kamera objektif (Cinemachine otomatis panning balik ke Player)
        if (objectiveCamera != null)
        {
            objectiveCamera.SetActive(false);
        }

        // 4. Tunggu sebentar agar animasi panning balik selesai (opsional, sesuaikan durasi blend)
        yield return new WaitForSeconds(1.5f); 

        // 5. Jalankan event lanjutan jika ada
        onPanFinished?.Invoke();
    }
}