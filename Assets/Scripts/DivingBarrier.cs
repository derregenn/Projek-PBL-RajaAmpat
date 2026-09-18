using UnityEngine;

public class DivingBarrier : MonoBehaviour
{
    [Header("UI Feedback")]
    [Tooltip("Masukkan objek teks peringatan dari Canvas (Opsional)")]
    public GameObject warningPanel;

    private void Start()
    {
        // Pastikan panel peringatan mati saat game dimulai
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }

    // Menggunakan OnTrigger agar bisa ditembus dan mendeteksi pemain masuk ke air
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 1. Tampilkan UI Peringatan
            if (warningPanel != null)
            {
                warningPanel.SetActive(true);
            }

            // 2. Aktifkan Mode Berenang
            PlayerDiving playerDiving = collision.GetComponent<PlayerDiving>();
            if (playerDiving != null)
            {
                playerDiving.StartDiving();
            }
        }
    }

    // Terpanggil ketika pemain keluar dari area air ke darat
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 1. Sembunyikan UI Peringatan
            if (warningPanel != null)
            {
                warningPanel.SetActive(false);
            }

            // 2. Matikan Mode Berenang
            PlayerDiving playerDiving = collision.GetComponent<PlayerDiving>();
            if (playerDiving != null)
            {
                playerDiving.StopDiving();
            }
        }
    }
}