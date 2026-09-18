using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerClimb : MonoBehaviour
{
    [Header("Climb Settings")]
    [SerializeField] private float climbSpeed = 3.5f;
    [SerializeField] private KeyCode climbKey = KeyCode.E;

    [Header("Detection")]
    [SerializeField] private Transform wallCheckPoint;
    [SerializeField] private float checkRadius = 0.3f;
    [SerializeField] private LayerMask wallLayer;

    [SerializeField] private Collider2D wallCollider; // Drag Collider Tebing ke sini

    private Rigidbody2D rb;
    private float defaultGravity;
    private float verticalInput;
    private bool isTouchingWall;
    private bool isClimbing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravity = rb.gravityScale;
    }

    private void Update()
    {
        // 1. Cek apakah Player menyentuh dinding/tebing
        isTouchingWall = Physics2D.OverlapCircle(wallCheckPoint.position, checkRadius, wallLayer);

        // 2. Setelah mulai memanjat, pertahankan status sampai tombol dilepas.
        // Jika langsung bergantung pada isTouchingWall, status akan mati saat
        // titik deteksi melewati ujung atas wall sehingga collider kembali solid.
        if (Input.GetKey(climbKey) && (isTouchingWall || isClimbing))
        {
            isClimbing = true;
            verticalInput = Input.GetAxisRaw("Vertical"); // W/S atau Panah Atas/Bawah
        }
        else
        {
            isClimbing = false;
        }

        if (wallCollider != null)
        {
            // Matikan fisik solid saat memanjat agar Player bisa lewat, dan aktifkan kembali saat dilepas
            wallCollider.isTrigger = isClimbing;
        }
    }

    private void FixedUpdate()
    {
        if (isClimbing)
        {
            rb.gravityScale = 0f;

            // Ambil input horizontal (A/D) dan vertikal (W/S)
            float horizontalInput = Input.GetAxisRaw("Horizontal");

            // Pemain bisa bergerak naik/turun sekaligus maju melewati tebing
            rb.linearVelocity = new Vector2(horizontalInput * climbSpeed, verticalInput * climbSpeed);
        }
        else
        {
            rb.gravityScale = defaultGravity;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Menampilkan area deteksi di Scene View (warna hijau)
        if (wallCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(wallCheckPoint.position, checkRadius);
        }
    }
}