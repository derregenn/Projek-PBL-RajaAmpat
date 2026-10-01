using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDiving : MonoBehaviour
{
    private Rigidbody2D rb;
    private float defaultGravity;
    private Player playerScript;

    [Header("Diving Settings")]
    public float swimSpeed = 5f;
    public float waterGravity = 0f;
    
    [HideInInspector]
    public bool isDiving = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerScript = GetComponent<Player>();
        
        if (rb != null)
        {
            defaultGravity = rb.gravityScale;
        }
    }

    private void Update()
    {
        if (isDiving && rb != null)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            rb.linearVelocity = new Vector2(moveX * swimSpeed, moveY * swimSpeed);
        }
    }

    public void StartDiving()
    {
        if (isDiving) return;

        isDiving = true;
        if (rb != null) rb.gravityScale = waterGravity;
        
        if (playerScript != null)
        {
            playerScript.StartSwimming();
        }

        Debug.Log("Player masuk ke air: Mode Berenang Aktif");
    }

    public void StopDiving()
    {
        if (!isDiving) return;

        isDiving = false;
        if (rb != null) rb.gravityScale = defaultGravity;
        
        if (playerScript != null)
        {
            playerScript.StopSwimming();
        }

        Debug.Log("Player naik ke darat: Mode Berjalan Aktif");
    }
}