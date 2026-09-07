using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerSwim : MonoBehaviour
{
    [Header("Base Stats & UI")]
    public int health = 100;
    public int coins = 0;
    public Image healthImage;
    public AudioClip hurtClip;

    [Header("Zero-G Movement")]
    public float moveSpeed = 7f;

    [Header("Diving / Dash Mechanic")]
    public float diveSpeed = 20f;
    public float diveDuration = 0.2f;
    public float diveCooldown = 0.5f;
    public AudioClip diveClip;

    [Header("Fall & Respawn Settings")]
    public float fallThresholdY = -50f;
    private Vector2 startPosition;

    // Components & States
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    private Vector2 moveInput;
    private Vector2 diveDirection;
    private bool isDiving = false;
    private bool canDive = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // Enable zero gravity for free swimming / floating
        rb.gravityScale = 0f;

        startPosition = transform.position;
    }

    void Update()
    {
        // 1. Lock normal input during dive burst
        if (isDiving) return;

        // 2. Read 8-Directional Input (WASD / Arrow Keys)
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        // 3. Trigger Diving (Spacebar)
        if (Input.GetKeyDown(KeyCode.Space) && canDive)
        {
            StartCoroutine(PerformDive());
        }

        // 4. Fall Check & Health UI
        if (transform.position.y < fallThresholdY)
        {
            ResetToStartPosition();
        }

        if (healthImage != null)
        {
            healthImage.fillAmount = health / 100f;
        }

        // 5. Rotate Player Sprite to face movement direction
        RotateTowardsMovement();
    }

    private void FixedUpdate()
    {
        if (isDiving) return;

        // Apply smooth velocity based on 2D input
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private IEnumerator PerformDive()
    {
        canDive = false;
        isDiving = true;

        // Dive in input direction, or forward direction if standing still
        diveDirection = moveInput != Vector2.zero ? moveInput : (spriteRenderer.flipX ? Vector2.left : Vector2.right);

        rb.linearVelocity = diveDirection * diveSpeed;
        PlaySFX(diveClip);

        if (animator != null)
        {
            animator.Play("Player_Dive");
        }

        yield return new WaitForSeconds(diveDuration);
        isDiving = false;

        // Cooldown timer before diving again
        yield return new WaitForSeconds(diveCooldown);
        canDive = true;
    }

    private void RotateTowardsMovement()
    {
        if (moveInput.x != 0)
        {
            spriteRenderer.flipX = moveInput.x < 0;
        }
    }

    private void ResetToStartPosition()
    {
        transform.position = startPosition;
        rb.linearVelocity = Vector2.zero;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Damage"))
        {
            PlaySFX(hurtClip);
            health -= 25;

            // Knockback effect
            Vector2 knockbackDir = (transform.position - collision.transform.position).normalized;
            rb.linearVelocity = knockbackDir * 8f;

            StartCoroutine(BlinkRed());

            if (health <= 0)
            {
                Die();
            }
        }
    }

    private IEnumerator BlinkRed()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void PlaySFX(AudioClip audioClip)
    {
        if (audioSource != null && audioClip != null)
        {
            audioSource.PlayOneShot(audioClip);
        }
    }
}