using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine; // Unity 6 Cinemachine

public class LandscapeViewInteractable : MonoBehaviour
{
    [Header("Camera Setup")]
    public CinemachineCamera camViewTambang; // Kamera tebing tambang
    public CinemachineCamera playerCamera;    // Kamera Player MC utama
    public int activePriority = 30;
    public float durasiLihat = 4.0f;         // Berapa detik kamera diam di tebing

    [Header("UI & Input")]
    public GameObject uiPrompt;              // Prompt tombol [F]

    [Header("Events")]
    public UnityEvent OnViewStarted;         // Dipanggil saat [F] ditekan
    public UnityEvent OnViewEnded;           // Dipanggil SETELAH kamera balik ke MC

    private bool isViewing = false;
    private bool isPlayerInArea = false;

    private void Start()
    {
        // Pastikan kamera tebing tambang tidak membajak kamera MC di awal game
        if (camViewTambang != null) camViewTambang.Priority.Value = 0;
        if (playerCamera != null) playerCamera.Priority.Value = 10;
    }

    private void Update()
    {
        if (isPlayerInArea && !isViewing && Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(RoutineViewing());
        }
    }

    private IEnumerator RoutineViewing()
    {
        isViewing = true;
        if (uiPrompt != null) uiPrompt.SetActive(false);

        // 1. Pan / Zoom ke Tebing Tambang
        if (camViewTambang != null) camViewTambang.Priority.Value = activePriority;
        OnViewStarted?.Invoke();

        // 2. Tunggu player melihat pemandangan
        yield return new WaitForSeconds(durasiLihat);

        // 3. PAKSA KAMERA BALIK KE MC (Set Priority Player Camera ke 10)
        if (camViewTambang != null) camViewTambang.Priority.Value = 0;
        if (playerCamera != null) playerCamera.Priority.Value = 10;

        // 4. Tunggu transisi pergerakan kamera smooth kembali menyorot MC
        yield return new WaitForSeconds(1.5f);

        // 5. Jalankan event penutup (Quest progress / dialog)
        if (QuestController.Instance != null)
        {
            QuestController.Instance.ProgressObjective("Lihat “pemandangan”", 1);
        }
        OnViewEnded?.Invoke();

        isViewing = false;
        if (isPlayerInArea && uiPrompt != null) uiPrompt.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInArea = true;
            if (uiPrompt != null && !isViewing) uiPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInArea = false;
            if (uiPrompt != null) uiPrompt.SetActive(false);
        }
    }
}