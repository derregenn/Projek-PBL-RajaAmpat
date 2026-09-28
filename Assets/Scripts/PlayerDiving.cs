using UnityEngine;

public class PlayerDiving : MonoBehaviour
{
    private Rigidbody2D rb;
    private float defaultGravity;
    private Player playerScript;

    [Header("Diving Settings")]
    public float swimSpeed = 5f;
    public float waterGravity = 0f; // Dibuat 0 agar melayang/tidak jatuh terus
    
    [HideInInspector]
    public bool isDiving = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerScript = GetComponent<Player>();
        defaultGravity = rb.gravityScale; // Simpan gravitasi asli saat di darat
    }

    private void Update()
    {
        if (isDiving)
        {
            // Kontrol renang bebas (kiri, kanan, atas, bawah)
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            rb.linearVelocity = new Vector2(moveX * swimSpeed, moveY * swimSpeed);
        }
    }

    public void StartDiving()
    {
        isDiving = true;
        rb.gravityScale = waterGravity; // Matikan gravitasi agar tidak jatuh
        
        if (playerScript != null)
        {
            playerScript.StartSwimming();
        }

        Debug.Log("Player masuk ke air: Mode Berenang Aktif");
    }

    public void StopDiving()
    {
        isDiving = false;
        rb.gravityScale = defaultGravity; // Kembalikan gravitasi darat
        
        if (playerScript != null)
        {
            playerScript.StopSwimming();
        }

        Debug.Log("Player naik ke darat: Mode Berjalan Aktif");
    }
}