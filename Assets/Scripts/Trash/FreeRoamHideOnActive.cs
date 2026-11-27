using UnityEngine;

/// <summary>
/// Pasang script ini ke seluruh objek di scene yang perlu disembunyikan
/// ketika mode Free Roam aktif (quest objects, barriers, NPC quest-only, dll).
///
/// CARA SETUP:
///   - Pasang ke objek sampah, barrier, atau NPC yang TIDAK boleh aktif
///     saat Free Roam (karena quest sudah selesai).
///   - Objek ini akan dinonaktifkan secara otomatis saat Free Roam = true.
/// </summary>
public class FreeRoamHideOnActive : MonoBehaviour
{
    private void Start()
    {
        if (GlobalTrashManager.Instance != null && GlobalTrashManager.Instance.IsFreeRoamMode)
        {
            gameObject.SetActive(false);
        }
    }
}
