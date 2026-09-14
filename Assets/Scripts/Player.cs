using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public int health = 100;
    public int coins = 0;
    public bool hasMap = false; 
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public Image healthImage;
    public AudioClip jumpClip;
    public AudioClip hurtClip;

    [Header("Fall & Respawn Settings")]
    public float fallThresholdY = -10f;
    private Vector2 startPosition;

    private Rigidbody2D rb;
    private bool isGrounded;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    public int extraJumpsValue = 1;
    private int extraJumps;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        extraJumps = extraJumpsValue;
        startPosition = transform.position;

        // --- Cek apakah ada data tersimpan dari tombol Save ---
        if (PlayerPrefs.HasKey("PlayerX"))
        {
            float x = PlayerPrefs.GetFloat("PlayerX");
            float y = PlayerPrefs.GetFloat("PlayerY");
            float z = PlayerPrefs.GetFloat("PlayerZ");

            transform.position = new Vector3(x, y, z);
            coins = PlayerPrefs.GetInt("PlayerCoins", 0);
            health = PlayerPrefs.GetInt("PlayerHealth", 100);
            
            Debug.Log("Data Player Berhasil Dimuat!");
        }
    }

    void Update()
    {
        float moveInput = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (isGrounded)
        {
            extraJumps = extraJumpsValue;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                PlaySFX(jumpClip);
            }
            else if (extraJumps > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                extraJumps--;
                PlaySFX(jumpClip);
            }
        }

        if (transform.position.y < fallThresholdY)
        {
            ResetToStartPosition();
        }

        SetAnimation(moveInput);

        // --- Proteksi Null agar tidak error jika healthImage belum dipasang ---
        if (healthImage != null)
        {
            healthImage.fillAmount = health / 100f;
        }    
    }

    private void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void ResetToStartPosition()
    {
        transform.position = startPosition;
        rb.linearVelocity = Vector2.zero;
    }

    private void SetAnimation(float moveInput)
    {
        if (isGrounded)
        {
            if (moveInput == 0)
            {
                animator.Play("Player_Idle");
            }
            else
            {
                animator.Play("Player_Run");
            }
        }
        else
        {
            if (rb.linearVelocityY > 0)
            {
                animator.Play("Player_Jump");
            }
            else
            {
                animator.Play("Player_Fall");
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Damage")
        {
            PlaySFX(hurtClip);
            health -= 25;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
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

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    public void PlaySFX(AudioClip audioClip)
    {
        if (audioSource != null && audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.Play();
        }
    }

    public void SaveGame()
    {
        Player player = FindFirstObjectByType<Player>();

        if (player != null)
        {
            PlayerPrefs.SetFloat("PlayerX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerZ", player.transform.position.z);

            PlayerPrefs.SetInt("PlayerCoins", player.coins);
            PlayerPrefs.SetInt("PlayerHealth", player.health);

            PlayerPrefs.Save();

            Debug.Log("Game Berhasil Disimpan!");
        }
        else
        {
            Debug.LogWarning("Objek Player tidak ditemukan untuk disimpan.");
        }
    }
}