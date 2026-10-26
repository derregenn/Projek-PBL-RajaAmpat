using System.Collections;
using UnityEngine;

public class BiotaMovement : MonoBehaviour
{
    [Header("Pengaturan Pergerakan")]
    public float moveSpeed = 1.5f;          // Kecepatan renang
    public float wanderRadius = 4f;         // Jangkauan area bergerak dari titik awal
    public float minWaitTime = 1f;          // Waktu diam minimal saat sampai tujuan
    public float maxWaitTime = 3f;          // Waktu diam maksimal

    [Header("Referensi Komponen")]
    public Animator animator;
    public SpriteRenderer[] spriteRenderers;

    [Header("Parameter Animator")]
    // Nama parameter Float pada Animator untuk mengatur kecepatan animasi (bergerak/idle).
    public string speedAnimParameter = "Speed";

    private Vector2 originPosition;
    private Vector2 targetPosition;
    private bool movingRight = true;

    private void Start()
    {
        originPosition = transform.position;

        // Auto-get komponen jika belum diisi di Inspector
        if (animator == null) animator = GetComponent<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        StartCoroutine(WanderRoutine());
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            // Bergerak bergantian ke kanan dan kiri pada sumbu horizontal.
            targetPosition = originPosition + new Vector2(movingRight ? wanderRadius : -wanderRadius, 0f);

            // Flip sprite sesuai arah tujuan (Kiri/Kanan)
            FlipSprite(targetPosition.x - transform.position.x);

            // 2. Bergerak menuju titik tujuan
            while (Vector2.Distance(transform.position, targetPosition) > 0.05f)
            {
                transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                // Kirim nilai kecepatan ke Animator
                if (animator != null)
                {
                    animator.SetFloat(speedAnimParameter, moveSpeed);
                }

                yield return null;
            }

            // 3. Tiba di tujuan: Set animasi ke Idle & Tunggu sejenak
            if (animator != null)
            {
                animator.SetFloat(speedAnimParameter, 0f);
            }

            yield return new WaitForSeconds((minWaitTime + maxWaitTime) * 0.5f);
            movingRight = !movingRight;
        }
    }

    private void FlipSprite(float directionX)
    {
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            // Jika bergerak ke kanan
            if (directionX > 0.01f)
            {
                renderer.flipX = false;
            }
            // Jika bergerak ke kiri
            else if (directionX < -0.01f)
            {
                renderer.flipX = true;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualisasi radius area bergerak di jendela Scene (Lingkaran Hijau)
        Gizmos.color = Color.green;
        Vector2 center = Application.isPlaying ? originPosition : (Vector2)transform.position;
        Gizmos.DrawWireSphere(center, wanderRadius);
    }
}