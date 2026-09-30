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

    private void Update()
    {
        // Geser objek ke arah kiri secara kontinyu
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        // Jika objek sudah melewai batas kiri, kembalikan ke posisi kanan (Looping)
        if (transform.position.x <= resetPositionX)
        {
            Vector3 newPos = transform.position;
            newPos.x = startPositionX;
            transform.position = newPos;
        }
    }
}