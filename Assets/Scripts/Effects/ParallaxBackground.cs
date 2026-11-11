using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("Scroll Settings")]
    [Tooltip("Kecepatan gerak background ke kiri")]
    [SerializeField] private float scrollSpeed = 1f;

    [Header("Manual Width (Isi jika gambar ada di Child Object)")]
    [Tooltip("Isi lebar gambar manual jika SpriteRenderer/RectTransform bernilai 0")]
    [SerializeField] private float manualWidth = 0f;

    [Header("Camera Loop")]
    [Tooltip("Kamera yang batasnya digunakan untuk mendeteksi loop")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Pindahkan background ke sisi kanan kamera setelah seluruhnya melewati batas kiri")]
    [SerializeField] private bool loopAtCameraBoundary = true;

    private float lengthX;
    private Vector3 startPosition;
    private float currentDistance;
    private SpriteRenderer[] backgroundRenderers;

    private void Start()
    {
        startPosition = transform.position;
        backgroundRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (TryGetSpriteBounds(out Bounds spriteBounds))
        {
            lengthX = spriteBounds.size.x;
        }
        else if (TryGetComponent<RectTransform>(out RectTransform rectTransform))
        {
            lengthX = rectTransform.rect.width * Mathf.Abs(transform.lossyScale.x);
        }

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

        if (loopAtCameraBoundary && targetCamera != null)
        {
            UpdateCameraBoundaryLoop();
            return;
        }

        // 1. Tambahkan jarak tempuh secara absolut
        currentDistance += scrollSpeed * Time.deltaTime;

        // 2. Gunakan Mathf.Repeat untuk menghitung sisa offset dengan presisi absolut
        // Ini memastikan jaraknya akan mengulang persis dari 0 saat menyentuh nilai lengthX
        float offset = Mathf.Repeat(currentDistance, lengthX);

        // 3. Terapkan posisinya berdasarkan posisi awal agar tidak ada pergeseran/drift posisi
        transform.position = startPosition + (Vector3.left * offset);
    }

    private void UpdateCameraBoundaryLoop()
    {
        Vector3 position = transform.position;
        position.x -= scrollSpeed * Time.deltaTime;
        transform.position = position;

        if (!TryGetSpriteBounds(out Bounds spriteBounds))
        {
            spriteBounds = new Bounds(
                transform.position,
                new Vector3(lengthX, 0f, 0f));
        }

        float depth = targetCamera.WorldToScreenPoint(transform.position).z;
        float cameraLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth)).x;
        float cameraRight = targetCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth)).x;

        if (spriteBounds.max.x < cameraLeft)
        {
            position = transform.position;
            position.x += cameraRight - spriteBounds.min.x;
            transform.position = position;
        }
    }

    private bool TryGetSpriteBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        foreach (SpriteRenderer spriteRenderer in backgroundRenderers)
        {
            if (!spriteRenderer.enabled || !spriteRenderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (hasBounds)
            {
                bounds.Encapsulate(spriteRenderer.bounds);
            }
            else
            {
                bounds = spriteRenderer.bounds;
                hasBounds = true;
            }
        }

        return hasBounds;
    }
}