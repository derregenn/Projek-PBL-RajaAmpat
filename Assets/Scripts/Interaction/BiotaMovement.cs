using System;
using System.Collections;
using UnityEngine;

public class BiotaMovement : MonoBehaviour
{
    public enum SwimDirection
    {
        KananKeKiri = -1,
        KiriKeKanan = 1,
        Acak = 0
    }

    [Header("Batas Area Kolam/Laut (Global)")]
    [Tooltip("Tarik Empty GameObject penanda ujung kiri perairan ke sini")]
    public Transform leftBoundary;
    [Tooltip("Tarik Empty GameObject penanda ujung kanan perairan ke sini")]
    public Transform rightBoundary;
    [Tooltip("Dipakai otomatis jika Left/Right Boundary dikosongkan")]
    public float fallbackRadius = 8f;

    [Header("Pengaturan Loop (Gaya Feeding Frenzy)")]
    public SwimDirection arahRenang = SwimDirection.Acak;
    public float moveSpeed = 2.2f;
    public float verticalLaneRange = 1.5f;

    [Header("Gelombang Renang Alami")]
    public float waveAmplitude = 0.2f;
    public float waveFrequency = 2.5f;

    [Header("Pengaturan Makan Umpan")]
    public float eatDuration = 3f;

    [Header("Referensi Komponen")]
    public Animator animator;
    public SpriteRenderer[] spriteRenderers;
    public string speedAnimParameter = "Speed";

    private Vector2 originPosition;
    private int currentDirX = 1;
    private float baseSwimY;
    private float waveTimer;
    private float minX, maxX;

    private Coroutine currentRoutine;
    private bool isFeeding = false;

    private void Start()
    {
        originPosition = transform.position;
        baseSwimY = originPosition.y;
        waveTimer = UnityEngine.Random.Range(0f, 10f);

        // Tentukan batas kiri (minX) & kanan (maxX) secara akurat
        if (leftBoundary != null && rightBoundary != null)
        {
            minX = Mathf.Min(leftBoundary.position.x, rightBoundary.position.x);
            maxX = Mathf.Max(leftBoundary.position.x, rightBoundary.position.x);
        }
        else
        {
            minX = originPosition.x - fallbackRadius;
            maxX = originPosition.x + fallbackRadius;
        }

        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (arahRenang == SwimDirection.Acak)
            currentDirX = UnityEngine.Random.value > 0.5f ? 1 : -1;
        else
            currentDirX = (int)arahRenang;

        currentRoutine = StartCoroutine(LoopSwimRoutine());
    }

    private IEnumerator LoopSwimRoutine()
    {
        FlipSprite(currentDirX);

        while (!isFeeding)
        {
            if (animator != null)
                animator.SetFloat(speedAnimParameter, moveSpeed);

            float nextX = transform.position.x + (currentDirX * moveSpeed * Time.deltaTime);
            waveTimer += Time.deltaTime * waveFrequency;
            float nextY = baseSwimY + Mathf.Sin(waveTimer) * waveAmplitude;

            transform.position = new Vector2(nextX, nextY);

            // Cek terhadap batas global (minX & maxX), bukan posisi awal ikan
            if (currentDirX > 0 && transform.position.x > maxX)
            {
                RespawnOppositeSide(minX);
            }
            else if (currentDirX < 0 && transform.position.x < minX)
            {
                RespawnOppositeSide(maxX);
            }

            yield return null;
        }
    }

    private void RespawnOppositeSide(float spawnX)
    {
        baseSwimY = originPosition.y + UnityEngine.Random.Range(-verticalLaneRange, verticalLaneRange);
        transform.position = new Vector2(spawnX, baseSwimY);
    }

    public void GoToBait(Vector3 baitPosition, Action onComplete)
    {
        isFeeding = true;
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(FeedRoutine(baitPosition, onComplete));
    }

    private IEnumerator FeedRoutine(Vector3 baitPosition, Action onComplete)
    {
        Vector2 finalBaitTarget = baitPosition;

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

        // Pastikan posisi tepat di titik umpan dan berhenti bergerak (Idle)
        transform.position = finalBaitTarget;
        if (animator != null)
        {
            animator.SetFloat(speedAnimParameter, 0f);
        }

        yield return new WaitForSeconds(eatDuration);

        onComplete?.Invoke();
    }

    private void FlipSprite(float directionX)
    {
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer == null) continue;
            if (directionX > 0.01f) renderer.flipX = false;
            else if (directionX < -0.01f) renderer.flipX = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        float left = (leftBoundary != null) ? leftBoundary.position.x : transform.position.x - fallbackRadius;
        float right = (rightBoundary != null) ? rightBoundary.position.x : transform.position.x + fallbackRadius;

        Vector3 center = new Vector3((left + right) * 0.5f, transform.position.y, 0f);
        Vector3 size = new Vector3(Mathf.Abs(right - left), verticalLaneRange * 2f, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}