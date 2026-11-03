using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SasiInteractable : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag PromptPanel (Tombol E di atas kepala MC) ke sini")]
    public GameObject uiPrompt;

    [Tooltip("Drag QTE Panel (Lingkaran QTE Biru / QTE1 / QTE2) ke sini")]
    public GameObject qtePanel;

    [Header("Animation Setup")]
    [Tooltip("Drag GameObject Player atau Animator MC ke sini (Boleh kosong, script akan auto-find tag Player)")]
    public Animator playerAnimator;
    [Tooltip("Nama parameter Bool di Animator Player untuk jongkok")]
    public string crouchBoolName = "isCrouching";

    [Header("Events")]
    public UnityEvent OnSasiCompleted; // Dipanggil saat QTE sukses

    private bool isPlayerInArea = false;
    private bool isQTEActive = false;

    private void Start()
    {
        // Pastikan QTE Biru mati di awal
        if (qtePanel != null) qtePanel.SetActive(false);
    }

    private void Update()
    {
        // Jika player di area, QTE belum jalan, dan menekan tombol E
        if (isPlayerInArea && !isQTEActive && Input.GetKeyDown(KeyCode.E))
        {
            MulaiQTE();
        }
    }

    private void MulaiQTE()
    {
        isQTEActive = true;

        // 1. Sembunyikan prompt tombol E di atas kepala
        if (uiPrompt != null) uiPrompt.SetActive(false);

        // 2. Pemicu Animasi Jongkok MC
        SetPlayerCrouch(true);

        // 3. Tampilkan UI QTE Biru
        if (qtePanel != null) qtePanel.SetActive(true);
    }

    // Panggil fungsi ini dari Event / Manager QTE saat QTE Selesai / Berhasil
    public void FinishQTE()
    {
        isQTEActive = false;

        // 1. Kembalikan Animasi MC ke berdiri
        SetPlayerCrouch(false);

        // 2. Sembunyikan QTE Biru
        if (qtePanel != null) qtePanel.SetActive(false);
        if (uiPrompt != null) uiPrompt.SetActive(false);

        // 3. Jalankan event sukses (progress objective, dll)
        OnSasiCompleted?.Invoke();

        // 4. Matikan titik interaksi air pantai
        gameObject.SetActive(false);
    }

    private void SetPlayerCrouch(bool isCrouching)
    {
        // Jika animator belum di-assign di Inspector, cari otomatis di GameObject Tag Player
        if (playerAnimator == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerAnimator = player.GetComponentInChildren<Animator>();
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetBool(crouchBoolName, isCrouching);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isQTEActive)
        {
            isPlayerInArea = true;
            if (uiPrompt != null) uiPrompt.SetActive(true);
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