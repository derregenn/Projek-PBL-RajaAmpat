using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public int health = 100;
    public bool hasMap = false;
    public float moveSpeed = 5f;
    public Image healthImage;
    public AudioClip hurtClip;

    [Header("Mode Berenang")]
    public bool isSwimming = false;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // 1. AMBIL INPUT GERAKAN HANYA SEKALI DI ATAS
        float moveInput = Input.GetAxisRaw("Horizontal"); // -1 untuk Kiri, 1 untuk Kanan, 0 untuk Diam

        // 2. OTOMATIS FLIP SPRITE SESUAI ARAH GERAK
        if (moveInput > 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = false; // Menghadap Kanan
        }
        else if (moveInput < 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = true;  // Menghadap Kiri
        }

        // 3. KONTROL FISIKA GERAKAN (Hanya saat di darat)
        if (!isSwimming && rb != null)
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }

        // 4. UPDATE ANIMASI & UI
        SetAnimation(moveInput);

        if (healthImage != null)
        {
            healthImage.fillAmount = health / 100f;
        }
    }

    private void SetAnimation(float moveInput)
    {
        if (animator == null) return;

        // JIKA SEDANG BERENANG
        if (isSwimming)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Player_swim"))
            {
                animator.Play("Player_swim");
            }
            return; // KUNCI UTAMA: Langsung return agar animasi Idle/Run di bawah tidak dipanggil!
        }

        // JIKA SEDANG DI DARAT
        if (moveInput == 0)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Idle"))
            {
                animator.Play("Player_Idle");
            }
        }
        else
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Run"))
            {
                animator.Play("Player_Run");
            }
        }
    }

    public void StartSwimming()
    {
        isSwimming = true;
        if (animator != null)
        {
            animator.SetBool("isSwimming", true);
            animator.Play("Player_swim");
        }
    }

    public void StopSwimming()
    {
        isSwimming = false;
        if (animator != null)
        {
            animator.SetBool("isSwimming", false);
            animator.Play("Player_Idle");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Damage"))
        {
            PlaySFX(hurtClip);
            health -= 25;
            StartCoroutine(BlinkRed());

            if (health <= 0)
            {
                Die();
            }
        }
    }

    private IEnumerator BlinkRed()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
        }
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