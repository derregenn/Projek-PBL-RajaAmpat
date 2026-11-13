using System;
using System.Collections;
using UnityEngine;

public class BiotaMovement : MonoBehaviour
{
    [Header("Pengaturan Pergerakan")]
    public float moveSpeed = 1.5f;          // Kecepatan renang
    public float wanderRadius = 4f;         // Jangkauan area bergerak dari titik awal
    public float minWaitTime = 1f;          // Waktu diam minimal saat sampai tujuan
    public float maxWaitTime = 3f;          // Waktu diam maksimal
    public float eatDuration = 3f;          // Lama waktu ikan makan di titik umpan

    [Header("Referensi Komponen")]
    public Animator animator;
    public SpriteRenderer[] spriteRenderers;

    [Header("Parameter Animator")]
    public string speedAnimParameter = "Speed";

    private Vector2 originPosition;
    private Vector2 targetPosition;
    private bool movingRight = true;

    private Coroutine currentRoutine;
    private bool isFeeding = false;

    private void Start()
    {
        originPosition = transform.position;

        // Pastikan parameter renang aktif dan dinamis di seluruh scene bawah laut
        wanderRadius = Mathf.Max(wanderRadius, 4.5f);
        moveSpeed = Mathf.Max(moveSpeed, 1.8f);
        minWaitTime = 0.3f;
        maxWaitTime = 1.2f;

        if (animator == null) animator = GetComponent<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        currentRoutine = StartCoroutine(WanderRoutine());
    }

    private IEnumerator WanderRoutine()
    {
        while (!isFeeding)
        {
            targetPosition = originPosition + new Vector2(movingRight ? wanderRadius : -wanderRadius, 0f);
            FlipSprite(targetPosition.x - transform.position.x);

            while (Vector2.Distance(transform.position, targetPosition) > 0.05f)
            {
                if (isFeeding) yield break;

                transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                if (animator != null)
                {
                    animator.SetFloat(speedAnimParameter, moveSpeed);
                }

                yield return null;
            }

            if (animator != null)
            {
                animator.SetFloat(speedAnimParameter, 0f);
            }

            yield return new WaitForSeconds((minWaitTime + maxWaitTime) * 0.5f);
            movingRight = !movingRight;
        }
    }

    /// <summary>
    /// Dipanggil oleh FishFeedZone saat tombol E ditekan untuk memberi umpan.
    /// </summary>
    public void GoToBait(Vector3 baitPosition, Action onComplete)
    {
        isFeeding = true;

        // Hentikan pergerakan wander biasa
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(FeedRoutine(baitPosition, onComplete));
    }

    private IEnumerator FeedRoutine(Vector3 baitPosition, Action onComplete)
    {
        // Berikan offset acak kecil agar ikan tidak menumpuk tepat di 1 titik
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 0.6f;
        Vector2 finalBaitTarget = (Vector2)baitPosition + randomOffset;

        // 1. Gerakkan ikan menuju umpan
        FlipSprite(finalBaitTarget.x - transform.position.x);

        while (Vector2.Distance(transform.position, finalBaitTarget) > 0.05f)
        {
            transform.position = Vector2.MoveTowards(transform.position, finalBaitTarget, moveSpeed * Time.deltaTime);

            if (animator != null)
            {
                animator.SetFloat(speedAnimParameter, moveSpeed);
            }

            yield return null;
        }

        // 2. Sampai di lokasi umpan: Set animasi ke Idle untuk memakan umpan
        if (animator != null)
        {
            animator.SetFloat(speedAnimParameter, 0f);
        }

        yield return new WaitForSeconds(eatDuration);

        // 3. Kabarkan ke FishFeedZone bahwa ikan ini selesai makan
        onComplete?.Invoke();
    }

    private void FlipSprite(float directionX)
    {
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (directionX > 0.01f)
            {
                renderer.flipX = true;
            }
            else if (directionX < -0.01f)
            {
                renderer.flipX = false;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector2 center = Application.isPlaying ? originPosition : (Vector2)transform.position;
        Gizmos.DrawWireSphere(center, wanderRadius);
    }
}