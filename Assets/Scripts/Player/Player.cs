using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public bool hasMap = false;
    public float moveSpeed = 3f;

    [Header("Mode Berenang")]
    public bool isSwimming = false;

    [Header("Audio Player")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip runClip;
    [SerializeField] private AudioClip swimClip;
    [SerializeField, Range(0f, 1f)] private float movementVolume = 0.7f;
    [SerializeField, Range(0.1f, 3f)] private float audioPitch = 1f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.pitch = audioPitch;
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

        // 4. UPDATE ANIMASI
        SetAnimation(moveInput);

        // 5. AUDIO GERAKAN
        UpdateMovementAudio(moveInput);
    }

    private void UpdateMovementAudio(float moveInput)
    {
        if (audioSource == null) return;

        AudioClip targetClip = null;
        if (moveInput != 0)
        {
            targetClip = isSwimming ? swimClip : runClip;
        }

        if (targetClip == null)
        {
            if (audioSource.isPlaying) audioSource.Stop();
            audioSource.clip = null;
            return;
        }

        if (audioSource.clip != targetClip || !audioSource.isPlaying)
        {
            audioSource.clip = targetClip;
            audioSource.volume = movementVolume;
            audioSource.pitch = audioPitch;
            audioSource.Play();
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


}