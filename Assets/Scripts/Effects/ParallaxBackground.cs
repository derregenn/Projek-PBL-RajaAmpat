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

    private void Update()
    {
        if (lengthX <= 0f) return;

        // Geser posisi ke kiri
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        // Reset posisi agar looping mulus
        while (transform.position.x <= startPosition.x - lengthX)
        {
            transform.position += new Vector3(lengthX * 2f, 0, 0);
        }
    }
}