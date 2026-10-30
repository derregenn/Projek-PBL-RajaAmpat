using System.Collections;
using UnityEngine;

public class PlayerTeleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Drag Empty GameObject yang menjadi titik lokasi tujuan teleportasi Player")]
    [SerializeField] private Transform targetDestination;
    [Tooltip("Jeda sebelum Player dipindahkan (detik)")]
    [SerializeField] private float teleportDelay = 1f;

    [Header("Player Settings (Opsional)")]
    [Tooltip("Kosongkan jika Player menggunakan Tag 'Player'")]
    [SerializeField] private Transform playerTransform;

    /// <summary>
    /// Panggil fungsi ini dari On Mid Fade () milik FaderTrigger
    /// </summary>
    public void TeleportPlayer()
    {
        StartCoroutine(TeleportPlayerAfterDelay());
    }

    private IEnumerator TeleportPlayerAfterDelay()
    {
        if (teleportDelay > 0f)
        {
            yield return new WaitForSeconds(teleportDelay);
        }

        // 1. Cari Player secara otomatis jika kolom playerTransform belum diisi
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        // 2. Eksekusi pemindahan posisi saat layar sedang GELAP TOTAL
        if (playerTransform != null && targetDestination != null)
        {
            playerTransform.position = targetDestination.position;
            Debug.Log($"[PlayerTeleporter] Player berhasil dipindahkan ke titik: {targetDestination.name}");
        }
        else
        {
            Debug.LogWarning("[PlayerTeleporter] Gagal Teleport! Pastikan Target Destination dan Tag Player sudah benar.", this);
        }
    }
}