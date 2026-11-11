using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class WelcomeBanner : MonoBehaviour
{
    [Header("Referensi UI")]
    public CanvasGroup bannerGroup;
    public RectTransform bannerTransform;
    
    [Header("Pengaturan Animasi")]
    public float displayDuration = 2.5f;   // Berapa lama banner tampil di layar
    public float fadeDuration = 0.6f;      // Kecepatan animasi
    public Vector2 slideOffset = new Vector2(0, -60f); // Jarak melayang dari bawah

    private Vector2 originalPosition;
    private bool isBannerPlaying = false;
    private GameObject cachedPlayerObj;

    private void Start()
    {
        // Otomatis mengambil komponen
        if (bannerGroup == null) bannerGroup = GetComponent<CanvasGroup>();
        if (bannerTransform == null) bannerTransform = GetComponent<RectTransform>();

        // Simpan posisi asli yang kamu atur di Unity Editor
        originalPosition = bannerTransform.anchoredPosition;
        
        // Atur ke posisi awal animasi (di bawah posisi asli & transparan)
        bannerTransform.anchoredPosition = originalPosition + slideOffset;
        bannerGroup.alpha = 0f;

        // KUNCI GERAKAN PLAYER SELAMA BANNER BERLANGSUNG
        isBannerPlaying = true;
        SetPlayerMovementLock(true);

        // Jalankan animasi
        StartCoroutine(PlayBannerAnimation());
    }

    private void Update()
    {
        // Pastikan player tetap terkunci selama animasi banner belum selesai
        if (isBannerPlaying)
        {
            EnsurePlayerLocked();
        }
    }

    private void OnDisable()
    {
        if (isBannerPlaying)
        {
            isBannerPlaying = false;
            SetPlayerMovementLock(false);
        }
    }

    private void OnDestroy()
    {
        if (isBannerPlaying)
        {
            isBannerPlaying = false;
            SetPlayerMovementLock(false);
        }
    }

    private void EnsurePlayerLocked()
    {
        SetPlayerMovementLock(true);
    }

    private void SetPlayerMovementLock(bool isLocked)
    {
        if (cachedPlayerObj == null)
        {
            cachedPlayerObj = GameObject.FindGameObjectWithTag("Player");
            if (cachedPlayerObj == null) cachedPlayerObj = GameObject.Find("Player");
        }

        if (cachedPlayerObj != null)
        {
            Player player = cachedPlayerObj.GetComponent<Player>();
            if (player != null)
            {
                player.isMovementLocked = isLocked;
                player.enabled = true;
            }

            PlayerSwim swim = cachedPlayerObj.GetComponent<PlayerSwim>();
            if (swim != null)
            {
                swim.isMovementLocked = isLocked;
            }

            Rigidbody2D rb = cachedPlayerObj.GetComponent<Rigidbody2D>();
            if (rb != null && isLocked)
            {
                rb.linearVelocity = Vector2.zero;
            }

            Animator anim = cachedPlayerObj.GetComponent<Animator>();
            if (anim != null && isLocked)
            {
                if (player != null && player.isSwimming)
                    anim.Play("Player_swim");
                else
                    anim.Play("Player_Idle");
            }
        }
    }

    private IEnumerator PlayBannerAnimation()
    {
        // --- FASE 1: MUNCUL (Fade In & Slide Up) ---
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;
            
            // Efek easing agar gerakan melambat halus di akhir
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f); 

            bannerGroup.alpha = Mathf.Lerp(0f, 1f, smoothProgress);
            bannerTransform.anchoredPosition = Vector2.Lerp(originalPosition + slideOffset, originalPosition, smoothProgress);
            
            yield return null;
        }

        bannerGroup.alpha = 1f;
        bannerTransform.anchoredPosition = originalPosition;

        // --- FASE 2: MENUNGGU ---
        yield return new WaitForSeconds(displayDuration);

        // --- FASE 3: MENGHILANG (Fade Out & Slide Up Sedikit) ---
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;

            bannerGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            bannerTransform.anchoredPosition = Vector2.Lerp(originalPosition, originalPosition + new Vector2(0, 30f), progress);
            
            yield return null;
        }

        bannerGroup.alpha = 0f;

        // BUKA KUNCI GERAKAN PLAYER SETELAH BANNER SELESAI MENGHILANG LENGKAP
        isBannerPlaying = false;
        SetPlayerMovementLock(false);

        gameObject.SetActive(false); // Matikan agar tidak memberatkan memori
    }
}
