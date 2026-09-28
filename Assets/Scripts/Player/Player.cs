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
        // 1. KONTROL GERAKAN
        if (!isSwimming)
        {
            // Gerakan di darat
            float moveInput = Input.GetAxis("Horizontal");
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
            SetAnimation(moveInput);
        }
        else
        {
            // Gerakan di air dikontrol sepenuhnya oleh PlayerDiving.cs
            // Di sini kita hanya mengurus pembalikan arah gambar (flipX) dan animasi
            float moveInput = Input.GetAxisRaw("Horizontal");
            if (moveInput != 0)
            {
                spriteRenderer.flipX = moveInput < 0;
            }
            SetAnimation(moveInput);
        }

        // 2. HEALTH UI
        if (healthImage != null)
        {
            healthImage.fillAmount = health / 100f;
        }
    }

    private void SetAnimation(float moveInput)
    {
        if (isSwimming)
        {
            // Hanya Play jika animasi yang sedang berjalan BUKAN Player_swim
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Player_swim"))
            {
                animator.Play("Player_swim");
            }
        }
        else if (moveInput == 0)
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
    }

    public void StopSwimming()
    {
        isSwimming = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Damage")
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
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void PlaySFX(AudioClip audioClip)
    {
        if (audioSource != null)
        {
            audioSource.clip = audioClip;
            audioSource.Play();
        }
    }
}