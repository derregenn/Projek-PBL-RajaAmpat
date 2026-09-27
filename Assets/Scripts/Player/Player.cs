using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public bool hasMap = false;
    public float moveSpeed = 5f;
    public AudioClip hurtClip;

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

    // Update is called once per frame
    void Update()
    {
        float moveInput = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        SetAnimation(moveInput);

    }

    private void SetAnimation(float moveInput)
    {
        if (moveInput != 0)
        {
            spriteRenderer.flipX = moveInput < 0;
        }

        if (moveInput == 0)
        {
            animator.Play("Player_Idle");
        }
        else
        {
            animator.Play("Player_Run");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Damage")
        {
            PlaySFX(hurtClip);
            StartCoroutine(BlinkRed());
        }
    }

    private IEnumerator BlinkRed()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;

    }

    public void PlaySFX(AudioClip audioClip)
    {
        audioSource.clip = audioClip;
        audioSource.Play();
    }
}
