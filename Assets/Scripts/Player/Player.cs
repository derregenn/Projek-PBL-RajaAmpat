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
    private Collider2D lastSurfaceCollider;
    private bool isCrouching = false;
    private bool isCrouchingGagVariant = false;
    private bool isScriptedMovementActive;

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

        isCrouching = false;
        isCrouchingGagVariant = false;
        if (animator != null)
        {
            if (HasBoolParam("isCrouching")) animator.SetBool("isCrouching", false);
            if (HasBoolParam("isCrouchingGag")) animator.SetBool("isCrouchingGag", false);
            if (animator.HasState(0, Animator.StringToHash("Player_Idle")))
            {
                animator.Play("Player_Idle");
            }
        }

        HideInactivePlayerDialogueBoxes();
    }

    private void HideInactivePlayerDialogueBoxes()
    {
        if (InteractionInputLock.IsDialogueActive) return;

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null) continue;
            if (canvas.gameObject.name.IndexOf("DialogueCanvas", System.StringComparison.OrdinalIgnoreCase) < 0) continue;

            Transform[] children = canvas.GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < children.Length; j++)
            {
                Transform child = children[j];
                if (child == null) continue;
                string childName = child.name;
                if (childName == "DialogueBox" || childName == "NextButton" || childName == "Btn_Next" || childName == "NextPrompt")
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    void Update()
    {
        if (isCrouching || (isMovementLocked && !isScriptedMovementActive))
        {
            if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            }
            SetAnimation(0f);
            UpdateMovementAudio(0f);
            return;
        }

        if (isMovementLocked && isScriptedMovementActive)
        {
            if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
            {
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            return;
        }

        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic && rb.constraints != RigidbodyConstraints2D.FreezeRotation)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
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

    private void FixedUpdate()
    {
        if ((isCrouching || (isMovementLocked && !isScriptedMovementActive))
            && rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void OnDisable()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }
        UpdateMovementAudio(0f);
    }

    public void SetScriptedMovementActive(bool active)
    {
        isScriptedMovementActive = active;
    }

    private void OnEnable()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic && !isMovementLocked && !isCrouching)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    public void SetCrouching(bool crouching, bool gagVariant)
    {
        isCrouching = crouching;
        isCrouchingGagVariant = crouching && gagVariant;

        if (crouching)
        {
            if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            }
            UpdateMovementAudio(0f);
        }
        else if (!isMovementLocked && rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
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
        if (other == lastSurfaceCollider) return;
        lastSurfaceCollider = other;
        CheckSurface(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == lastSurfaceCollider)
        {
            lastSurfaceCollider = null;
        }
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

        if (isCrouching && isCrouchingGagVariant)
        {
            if (animator.HasState(0, Animator.StringToHash("Player_Crouch_Gag")) &&
                !animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Crouch_Gag"))
            {
                animator.Play("Player_Crouch_Gag");
            }
            return;
        }

        if (!isCrouching && HasBoolParam("isCrouchingGag") && animator.GetBool("isCrouchingGag"))
        {
            animator.SetBool("isCrouchingGag", false);
        }

        if (isCrouching || (HasBoolParam("isCrouching") && animator.GetBool("isCrouching")))
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Crouch"))
            {
                animator.Play("Player_Crouch");
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

    private bool HasBoolParam(string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Bool &&
                parameters[i].name == paramName)
            {
                return true;
            }
        }

        return false;
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