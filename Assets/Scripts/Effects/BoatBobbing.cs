using UnityEngine;

public class BoatBobbing : MonoBehaviour
{
    [Header("Bobbing Settings")]
    [SerializeField] private float floatSpeed = 2f;    // Kecepatan membal
    [SerializeField] private float floatAmount = 0.1f;  // Ketinggian membal naik-turun
    [SerializeField] private float tiltAmount = 1.5f;   // Kemiringan rotasi kapal

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        // Gerakan naik-turun menggunakan Gelombang Sinus
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Sedikit rotasi membal
        float tilt = Mathf.Sin(Time.time * floatSpeed * 0.8f) * tiltAmount;
        transform.rotation = Quaternion.Euler(0, 0, tilt);
    }
}