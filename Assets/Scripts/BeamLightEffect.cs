using UnityEngine;

public class BeamLightEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float timeOffset;

    [Header("Pengaturan Pulse (Kelap-Kelip)")]
    [Tooltip("Kecepatan pendaran cahaya")]
    public float pulseSpeed = 1.2f;
    [Tooltip("Batas transparansi minimum (0 = transparan total)")]
    public float minAlpha = 0.15f;
    [Tooltip("Batas transparansi maksimum")]
    public float maxAlpha = 0.65f;

    [Header("Pengaturan Sway (Goyangan Halus)")]
    public bool enableSway = true;
    [Tooltip("Kecepatan ayunan miring")]
    public float swaySpeed = 0.8f;
    [Tooltip("Derajat kemiringan ayunan sinar")]
    public float swayAngle = 2.5f;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Acak offset waktu & kecepatan agar tiap sorotan bergetar di waktu yang berbeda
        timeOffset = Random.Range(0f, 100f);
        pulseSpeed *= Random.Range(0.85f, 1.15f);
        swaySpeed *= Random.Range(0.85f, 1.15f);
    }

    private void Update()
    {
        if (spriteRenderer == null) return;

        float currentTime = Time.time + timeOffset;

        // 1. Efek Kelap-Kelip Alpha (Pulse)
        float alphaLerp = (Mathf.Sin(currentTime * pulseSpeed) + 1f) / 2f;
        Color c = originalColor;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, alphaLerp);
        spriteRenderer.color = c;

        // 2. Efek Goyangan Miring Lembut (Sway)
        if (enableSway)
        {
            float zRotation = Mathf.Sin(currentTime * swaySpeed) * swayAngle;
            transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        }
    }
}