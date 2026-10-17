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
        if (lengthX <= 0f)
        {
            return;
        }

        // Geser posisi ke kiri
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        // Pertahankan sisa jarak agar tetap mulus meski satu frame bergerak melewati batas.
        while (transform.position.x <= startPosition.x - lengthX)
        {
            transform.position += new Vector3(lengthX * 2f, 0, 0);
        }
    }
}