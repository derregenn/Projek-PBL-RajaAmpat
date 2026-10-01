using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("Scroll Settings")]
    [Tooltip("Kecepatan gerak background ke kiri (positif = bergerak ke kiri)")]
    [SerializeField] private float scrollSpeed = 2f;

    [Tooltip("Batas koordinat X paling kiri sebelum gambar di-loop ke kanan")]
    [SerializeField] private float resetPositionX = -20f;

    [Tooltip("Posisi koordinat X awal di sebelah kanan saat di-loop")]
    [SerializeField] private float startPositionX = 20f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // Geser objek ke arah kiri secara kontinyu
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        // Pindahkan sejauh lebar sprite agar tidak muncul celah saat looping.
        float loopDistance = spriteRenderer != null
            ? spriteRenderer.bounds.size.x
            : startPositionX - resetPositionX;

        while (transform.position.x <= resetPositionX)
        {
            Vector3 newPos = transform.position;
            newPos.x += loopDistance;
            transform.position = newPos;
        }
    }
}