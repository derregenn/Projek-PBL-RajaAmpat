using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("Scroll Settings")]
    [Tooltip("Kecepatan gerak background ke kiri")]
    [SerializeField] private float scrollSpeed = 1f;

    [Header("Manual Width (Isi jika gambar ada di Child Object)")]
    [Tooltip("Isi lebar gambar manual jika SpriteRenderer/RectTransform bernilai 0")]
    [SerializeField] private float manualWidth = 0f;

    private float lengthX;
    private Vector3 startPosition;
    
    // Variabel baru untuk melacak jarak absolut
    private float currentDistance; 

    private void Start()
    {
        startPosition = transform.position;

        // 1. Coba ambil dari SpriteRenderer di objek ini
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            lengthX = spriteRenderer.bounds.size.x;
        }
        // 2. Jika tidak ada, coba ambil dari RectTransform (UI)
        else if (TryGetComponent<RectTransform>(out RectTransform rectTransform))
        {
            lengthX = rectTransform.rect.width * transform.lossyScale.x;
        }
        // 3. Jika masih tidak ketemu, coba cari SpriteRenderer di child object
        else
        {
            SpriteRenderer childSprite = GetComponentInChildren<SpriteRenderer>();
            if (childSprite != null)
            {
                lengthX = childSprite.bounds.size.x;
            }
        }

        // 4. Fallback ke Manual Width
        if (lengthX <= 0f && manualWidth > 0f)
        {
            lengthX = manualWidth;
        }

        if (lengthX <= 0f)
        {
            Debug.LogWarning($"ParallaxBackground: Lebar background pada '{gameObject.name}' masih 0. Silakan isi 'Manual Width' di Inspector!", this);
        }
    }

    // Menggunakan LateUpdate agar dieksekusi setelah semua pergerakan (seperti kamera/player) selesai di frame ini
    private void LateUpdate()
    {
        if (lengthX <= 0f) return;

        // 1. Tambahkan jarak tempuh secara absolut
        currentDistance += scrollSpeed * Time.deltaTime;

        // 2. Gunakan Mathf.Repeat untuk menghitung sisa offset dengan presisi absolut
        // Ini memastikan jaraknya akan mengulang persis dari 0 saat menyentuh nilai lengthX
        float offset = Mathf.Repeat(currentDistance, lengthX);

        // 3. Terapkan posisinya berdasarkan posisi awal agar tidak ada pergeseran/drift posisi
        transform.position = startPosition + (Vector3.left * offset);
    }
}