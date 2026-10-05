using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("Scroll Settings")]
    [Tooltip("Kecepatan gerak background ke kiri")]
    [SerializeField] private float scrollSpeed = 1f;

    private float lengthX;
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;

        // Menghitung lebar pasti (Width) dari SpriteRenderer secara otomatis
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            lengthX = spriteRenderer.bounds.size.x;
        }
        else
        {
            Debug.LogWarning("ParallaxBackground: SpriteRenderer tidak ditemukan pada " + gameObject.name);
        }
    }

    private void Update()
    {
        // Geser posisi ke kiri
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        // Jika gambar sudah bergeser sejauh panjang lebarnya sendiri, reset posisi secara mulus
        if (transform.position.x <= startPosition.x - lengthX)
        {
            // Menambahkan offset presisi agar tidak ada celah/gap pixel
            transform.position += new Vector3(lengthX * 2f, 0, 0);
        }
    }
}