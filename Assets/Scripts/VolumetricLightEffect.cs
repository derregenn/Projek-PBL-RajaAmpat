using UnityEngine;

public class VolumetricLightEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    [Header("Pengaturan Kelap-Kelip (Pulse)")]
    [Tooltip("Kecepatan pendaran cahaya")]
    public float pulseSpeed = 1.5f;
    
    [Tooltip("Batas transparansi minimum (0 = transparan total, 1 = pekat)")]
    public float minAlpha = 0.3f;
    
    [Tooltip("Batas transparansi maksimum")]
    public float maxAlpha = 0.7f;

    [Header("Pengaturan Goyangan Lembut (Sway)")]
    [Tooltip("Aktifkan jika ingin cahaya bergeser perlahan mengikuti gelombang")]
    public bool enableSway = true;
    public float swaySpeed = 0.8f;
    public float swayAmount = 0.15f;

    private Vector3 startPosition;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        startPosition = transform.localPosition;
    }

    private void Update()
    {
        if (spriteRenderer == null) return;

        // 1. Efek Kelap-Kelip Halus (Smooth Fade Alpha)
        float lerpTime = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f; // Nilai 0 sampai 1
        float newAlpha = Mathf.Lerp(minAlpha, maxAlpha, lerpTime);

        Color newColor = originalColor;
        newColor.a = newAlpha;
        spriteRenderer.color = newColor;

        // 2. Efek Goyangan Lembut Kiri-Kanan (Opsional)
        if (enableSway)
        {
            float offsetX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
            transform.localPosition = startPosition + new Vector3(offsetX, 0f, 0f);
        }
    }
}