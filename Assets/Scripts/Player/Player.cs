using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    public bool hasMap = false;
    public float moveSpeed = 3f;
    public bool isMovementLocked = false;

    [Header("Mode Berenang")]
    public bool isSwimming = false;

    // Struct untuk input audio per jenis tanah di Inspector
    [System.Serializable]
    public struct SurfaceAudioData
    {
        public SurfaceKind surfaceKind;
        public AudioClip runClip;
    }

    [Header("Audio Player")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swimClip;
    [SerializeField] private AudioClip defaultRunClip; // Suara cadangan jika tanah tidak terdaftar
    [SerializeField] private List<SurfaceAudioData> surfaceAudioList; // Setup daftar suara di Inspector
    [SerializeField, Range(0f, 1f)] private float movementVolume = 0.7f;
    [SerializeField, Range(0.1f, 3f)] private float audioPitch = 1f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Dictionary pencarian cepat (Key: Jenis Tanah -> Value: Audio Clip)
    private Dictionary<SurfaceKind, AudioClip> surfaceAudioDict = new Dictionary<SurfaceKind, AudioClip>();
    private AudioClip currentRunClip;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 1. INSIALISASI DICTIONARY DARI LIST INSPECTOR
        surfaceAudioDict.Clear();
        foreach (var data in surfaceAudioList)
        {
            if (!surfaceAudioDict.ContainsKey(data.surfaceKind) && data.runClip != null)
            {
                surfaceAudioDict.Add(data.surfaceKind, data.runClip);
            }
        }

        currentRunClip = defaultRunClip;

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
        if (isMovementLocked)
        {
            if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            SetAnimation(0f);
            UpdateMovementAudio(0f);
            return;
        }

        float moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput > 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }
        else if (moveInput < 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = true;
        }

        if (!isSwimming && rb != null)
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }

        SetAnimation(moveInput);
        UpdateMovementAudio(moveInput);
    }

    private void UpdateMovementAudio(float moveInput)
    {
        if (audioSource == null) return;

        AudioClip targetClip = null;
        if (moveInput != 0)
        {
            targetClip = isSwimming ? swimClip : (currentRunClip != null ? currentRunClip : defaultRunClip);
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

    // DETEKSI PERMUKAAN MENGGUNAKAN SCRIPT SurfaceType
    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckSurface(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckSurface(other.gameObject);
    }

    private void CheckSurface(GameObject obj)
    {
        SurfaceType surface = obj.GetComponent<SurfaceType>();
        if (surface == null)
        {
            surface = obj.GetComponentInParent<SurfaceType>();
        }

        if (surface != null && surfaceAudioDict.TryGetValue(surface.surfaceKind, out AudioClip clip))
        {
            currentRunClip = clip;
        }
    }

    private void SetAnimation(float moveInput)
    {
        if (animator == null) return;

        if (isSwimming)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Player_swim"))
            {
                animator.Play("Player_swim");
            }
            return;
        }

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