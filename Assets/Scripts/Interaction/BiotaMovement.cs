using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiotaMovement : MonoBehaviour
{
    [Header("Pengaturan Pergerakan")]
    public float moveSpeed = 1.5f;          // Kecepatan renang
    public float wanderRadius = 6f;         // Jangkauan area bergerak dari titik awal
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
    private Coroutine currentRoutine;
    private bool isFeeding = false;
    private float seedOffset;

    // Untuk menggerakkan anak-anak ikan di dalam kelompok/kawanan
    private struct ChildFishData
    {
        public Transform transform;
        public Vector3 baseLocalPos;
        public float animOffset;
    }
    private readonly List<ChildFishData> childFishes = new List<ChildFishData>();

    private void Awake()
    {
        seedOffset = UnityEngine.Random.Range(0f, 100f);
    }

    private void Start()
    {
        originPosition = transform.position;

        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        // Daftarkan child-child ikan (jika ada seperti pada KawananIkan)
        foreach (Transform child in transform)
        {
            if (child.GetComponent<SpriteRenderer>() != null)
            {
                childFishes.Add(new ChildFishData
                {
                    transform = child,
                    baseLocalPos = child.localPosition,
                    animOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f)
                });
            }
        }

        currentRoutine = StartCoroutine(WanderRoutine());
    }

    private void Update()
    {
        // Berikan gerakan renang meliuk halus untuk anak-anak ikan di dalam kawanan
        if (childFishes.Count > 0)
        {
            float t = Time.time * 2.5f;
            for (int i = 0; i < childFishes.Count; i++)
            {
                var cf = childFishes[i];
                if (cf.transform != null)
                {
                    float offsetY = Mathf.Sin(t + cf.animOffset) * 0.15f;
                    float offsetX = Mathf.Cos(t * 0.7f + cf.animOffset) * 0.1f;
                    cf.transform.localPosition = cf.baseLocalPos + new Vector3(offsetX, offsetY, 0f);
                }
            }
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (!isFeeding)
        {
            // Pilih waypoint tujuan secara acak 2D (horizontal dan vertikal) di sekitar habitat
            float randX = UnityEngine.Random.Range(-wanderRadius, wanderRadius);
            float randY = UnityEngine.Random.Range(-wanderRadius * 0.45f, wanderRadius * 0.45f);
            targetPosition = originPosition + new Vector2(randX, randY);

            FlipSprite(targetPosition.x - transform.position.x);

            float currentSpeed = UnityEngine.Random.Range(moveSpeed * 0.8f, moveSpeed * 1.2f);

            while (Vector2.Distance(transform.position, targetPosition) > 0.1f)
            {
                if (isFeeding) yield break;

                // Gerakan renang sinusoidal alami
                float wave = Mathf.Sin(Time.time * 3f + seedOffset) * 0.008f;
                Vector2 nextStep = Vector2.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);
                nextStep.y += wave;

                transform.position = nextStep;

                if (animator != null && !string.IsNullOrEmpty(speedAnimParameter))
                {
                    animator.SetFloat(speedAnimParameter, currentSpeed);
                }

                yield return null;
            }

            if (animator != null && !string.IsNullOrEmpty(speedAnimParameter))
            {
                animator.SetFloat(speedAnimParameter, 0f);
            }

            float wait = UnityEngine.Random.Range(Mathf.Max(0.5f, minWaitTime), Mathf.Max(1.5f, maxWaitTime));
            yield return new WaitForSeconds(wait);
        }
    }

    /// <summary>
    /// Dipanggil oleh FishFeedZone saat tombol E ditekan untuk memberi umpan.
    /// </summary>
    public void GoToBait(Vector3 baitPosition, Action onComplete)
    {
        isFeeding = true;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(FeedRoutine(baitPosition, onComplete));
    }

    private IEnumerator FeedRoutine(Vector3 baitPosition, Action onComplete)
    {
        // Berikan offset acak kecil agar ikan tidak menumpuk tepat di 1 titik
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 0.8f;
        Vector2 finalBaitTarget = (Vector2)baitPosition + randomOffset;

        // 1. Gerakkan ikan menuju umpan
        FlipSprite(finalBaitTarget.x - transform.position.x);

        while (Vector2.Distance(transform.position, finalBaitTarget) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(transform.position, finalBaitTarget, (moveSpeed * 1.3f) * Time.deltaTime);

            if (animator != null && !string.IsNullOrEmpty(speedAnimParameter))
            {
                animator.SetFloat(speedAnimParameter, moveSpeed);
            }

            yield return null;
        }

        // 2. Sampai di lokasi umpan: Set animasi ke Idle untuk memakan umpan
        if (animator != null && !string.IsNullOrEmpty(speedAnimParameter))
        {
            animator.SetFloat(speedAnimParameter, 0f);
        }

        yield return new WaitForSeconds(eatDuration);

        // 3. Kabarkan ke FishFeedZone bahwa ikan ini selesai makan
        onComplete?.Invoke();

        // 4. Kembali berenang bebas santai
        isFeeding = false;
        currentRoutine = StartCoroutine(WanderRoutine());
    }

    private void FlipSprite(float directionX)
    {
        if (spriteRenderers == null) return;

        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer == null) continue;

            if (directionX > 0.05f)
            {
                renderer.flipX = true;
            }
            else if (directionX < -0.05f)
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