using UnityEngine;

public class PlayerDiving : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;
    private float defaultGravity;

    [Header("Diving Settings")]
    public float swimSpeed = 5f;
    public float waterGravity = 0.5f; // Gravitasi lebih ringan agar mengambang

    [HideInInspector]
    public bool isDiving = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        defaultGravity = rb.gravityScale; // Simpan gravitasi asli saat di darat
    }

    private void Update()
    {
        if (isDiving)
        {
            // Logika kontrol renang (bisa digerakkan bebas ke atas/bawah/kiri/kanan)
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            rb.linearVelocity = new Vector2(moveX * swimSpeed, moveY * swimSpeed);
        }
    }

    public void StartDiving()
    {
        isDiving = true;
        rb.gravityScale = waterGravity; // Ubah gravitasi saat masuk air
        if (animator != null)
        {
            animator.Play("Player_Diving");
        }
        Debug.Log("Player masuk ke air: Mode Berenang Aktif");
    }

    public void StopDiving()
    {
        isDiving = false;
        rb.gravityScale = defaultGravity; // Kembalikan gravitasi normal
        Debug.Log("Player naik ke darat: Mode Berjalan Aktif");
    }
}