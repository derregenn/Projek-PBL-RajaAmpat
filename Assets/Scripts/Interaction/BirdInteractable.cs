using UnityEngine;
using UnityEngine.Events;

public class BirdInteractable : MonoBehaviour
{
    [Header("QTE Jalan Perlahan)")]
    [Tooltip("Masukkan objek Player")]
    [SerializeField] private Transform playerTarget;
    [Tooltip("Titik awal jalan mengendap-endap. Player akan dilempar ke sini jika gagal QTE.")]
    [SerializeField] private Transform resetPoint;

    [Tooltip("Masukkan 3 GameObject Trigger/Area QTE yang ada di scene")]
    [SerializeField] private GameObject[] qteTriggers;
    private int currentQTEProgress = 0;

    [Header("Animasi Burung")]
    [Tooltip("Objek animasi burung yang akan dinyalakan saat QTE gagal")]
    [SerializeField] private GameObject birdAnimation;
    [Tooltip("Animator burung yang memiliki state diam")]
    [SerializeField] private Animator birdAnimator;
    [SerializeField] private string birdIdleState = "Idle";

    [Header("Setup Poin 10 (Ambil Foto)")]
    [SerializeField] private KeyCode photoKey = KeyCode.F;
    [SerializeField] private GameObject promptFotoUI; // UI tulisan "Tekan F"
    [SerializeField] private GameObject flashEffect;  // UI Canvas Putih untuk efek kilat kamera

    [Header("Quest Objective")]
    [Tooltip("ID Objective untuk diserahkan ke Pembisnis Foto")]
    [SerializeField] private string objectiveID = "serahkan_foto_pembisnis";

    private bool canTakePhoto = false;
    private bool isPhotoDone = false;

    private void Start()
    {
        // Pastikan UI Prompt dan Efek Flash mati di awal
        if (promptFotoUI != null) promptFotoUI.SetActive(false);
        if (flashEffect != null) flashEffect.SetActive(false);
        if (birdAnimator != null) birdAnimator.enabled = false;
    }

    private void Update()
    {
        // Poin 10: Player sudah dekat dan memencet tombol F
        if (canTakePhoto && !isPhotoDone && Input.GetKeyDown(photoKey))
        {
            AmbilFoto();
        }
    }

    // --------------------------------------------------------
    // FUNGSI UNTUK DIPANGGIL OLEH SISTEM QTE (EVENT ON SUCCESS)
    // --------------------------------------------------------

    /// <summary>
    /// Panggil fungsi ini setiap kali Player berhasil melewati 1 tahapan QTE
    /// </summary>
    public void OnQTESuccess()
    {
        currentQTEProgress++;
        Debug.Log($"[Cenderawasih] Berhasil QTE! Progres: {currentQTEProgress}/3");

        if (currentQTEProgress >= 3)
        {
            // Jika sudah 3x sukses, izinkan ambil foto
            SiapAmbilFoto();
        }
    }

    // --------------------------------------------------------
    // FUNGSI UNTUK DIPANGGIL OLEH SISTEM QTE (EVENT ON FAIL)
    // --------------------------------------------------------

    /// <summary>
    /// Panggil fungsi ini jika Player GAGAL menekan QTE pada waktunya
    /// </summary>
    public void OnQTEFailed()
    {
        Debug.LogWarning("[Cenderawasih] GAGAL QTE! Mengulang dari awal titik mengendap.");

        currentQTEProgress = 0;
        canTakePhoto = false;
        if (promptFotoUI != null) promptFotoUI.SetActive(false);
        if (birdAnimator != null) birdAnimator.enabled = true;
        if (birdAnimator != null && !string.IsNullOrEmpty(birdIdleState))
        {
            int idleStateHash = Animator.StringToHash(birdIdleState);
            if (birdAnimator.HasState(0, idleStateHash))
                birdAnimator.Play(idleStateHash, 0, 0f);
            else
                Debug.LogWarning($"[Cenderawasih] Animator tidak memiliki state '{birdIdleState}'.");
        }

        // 1. Hukuman: Teleport player kembali ke titik awal (Reset Point)
        if (playerTarget != null && resetPoint != null)
        {
            playerTarget.position = resetPoint.position;
        }

        // 2. Reset semua Trigger Area QTE agar aktif/muncul lagi
        if (qteTriggers == null || qteTriggers.Length == 0)
        {
            Debug.LogWarning("[Cenderawasih] Tidak ada QTE Trigger yang terdaftar untuk di-reset.");
            return;
        }

        int resetCount = 0;
        foreach (GameObject qte in qteTriggers)
        {
            if (qte == null)
            {
                Debug.LogWarning("[Cenderawasih] Ada referensi QTE Trigger yang kosong.");
                continue;
            }

            qte.SetActive(true);
            resetCount++;
            Debug.Log($"[Cenderawasih] QTE Trigger '{qte.name}' diaktifkan kembali.");
        }

        Debug.Log($"[Cenderawasih] Reset QTE selesai. {resetCount}/{qteTriggers.Length} Trigger diaktifkan.");
    }

    // --------------------------------------------------------
    // LOGIKA INTERNAL POIN 10 (FOTO)
    // --------------------------------------------------------

    private void SiapAmbilFoto()
    {
        canTakePhoto = true;

        if (promptFotoUI != null)
            promptFotoUI.SetActive(true);

        Debug.Log("[Cenderawasih] Player berhasil mendekat! Tekan F untuk mengambil foto.");
    }

    private void AmbilFoto()
    {
        isPhotoDone = true;
        canTakePhoto = false;

        // Matikan tulisan "Tekan F"
        if (promptFotoUI != null) promptFotoUI.SetActive(false);

        // Nyalakan efek flash kamera sebentar
        if (flashEffect != null)
        {
            flashEffect.SetActive(true);
            Invoke(nameof(MatikanFlash), 0.15f); // Flash kilat selama 0.15 detik
        }

        // Update Quest Controller sesuai Guideline Poin 10
        if (QuestController.Instance != null)
        {
            QuestController.Instance.ProgressObjective(objectiveID, 1);
            Debug.Log("[Cenderawasih] Foto diambil! Objective diupdate: Serahkan Hasil Foto kepada Pembisnis Foto");
        }
    }

    private void MatikanFlash()
    {
        if (flashEffect != null) flashEffect.SetActive(false);
    }
}