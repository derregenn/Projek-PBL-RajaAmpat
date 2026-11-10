using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("BGM Audio Sources (Need 2 for Crossfade)")]
    public AudioSource bgmSource1; 
    public AudioSource bgmSource2; 

    private AudioSource activeBGMSource;
    private AudioSource backgroundBGMSource;

    [Header("Audio Clips")]
    public AudioClip defaultBGM;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Tooltip("Durasi fade-out/fade-in untuk looping BGM agar transisi halus.")]
    [Range(0.5f, 5f)] public float fadeDurationForLoop = 2f; 

    [Header("Additional Audio Layers")]
    public List<AudioLayer> audioLayers = new List<AudioLayer>();

    private bool isMuted = false;
    private Coroutine crossfadeCoroutine;
    private Coroutine loopingBGMFadeCoroutine;
    private bool isLoopCrossfading = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Inisialisasi status BGM source
            activeBGMSource = bgmSource1;
            backgroundBGMSource = bgmSource2;
            
            // Pastikan kedua BGM sources tidak looping by default dan memiliki volume 0
            if (bgmSource1 != null) {
                bgmSource1.loop = false;
                bgmSource1.volume = 0f;
            }
            if (bgmSource2 != null) {
                bgmSource2.loop = false;
                bgmSource2.volume = 0f;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (defaultBGM != null)
        {
            // Panggil metode baru untuk BGM looping dengan fade
            PlayBGMWithLoopFade(defaultBGM, bgmVolume, fadeDurationForLoop);
        }

        foreach (AudioLayer layer in audioLayers)
        {
            if (layer == null || layer.source == null)
                continue;

            if (layer.clip != null)
                layer.source.clip = layer.clip;

            ApplyLayerVolume(layer);
            if (layer.playOnStart)
                layer.source.Play();
        }
    }

    // --- FUNGSI CROSSFADE BGM BARU ---
    public void CrossfadeBGM(AudioClip newClip, float fadeDuration)
    {
        if (activeBGMSource.clip == newClip) return; 

        // Hentikan crossfade yang sedang berjalan (jika ada)
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }

        // Tukar peran AudioSource
        AudioSource temp = activeBGMSource;
        activeBGMSource = backgroundBGMSource;
        backgroundBGMSource = temp;

        // Siapkan lagu baru di source yang aktif
        activeBGMSource.clip = newClip;
        activeBGMSource.Play();

        // Mulai transisi volume
        crossfadeCoroutine = StartCoroutine(FadeVolume(activeBGMSource, backgroundBGMSource, fadeDuration));
    }

    // Fungsi baru untuk memulai BGM dengan looping fade halus
    public void PlayBGMWithLoopFade(AudioClip clip, float volume, float fadeDuration)
    {
        if (clip == null)
            return;

        if (loopingBGMFadeCoroutine != null)
        {
            StopCoroutine(loopingBGMFadeCoroutine);
        }

        if (activeBGMSource == null || backgroundBGMSource == null)
        {
            activeBGMSource = bgmSource1;
            backgroundBGMSource = bgmSource2;
        }

        // Pastikan source yang aktif mulai dari awal
        activeBGMSource.clip = clip;
        activeBGMSource.time = 0f;
        activeBGMSource.volume = 0f;
        activeBGMSource.Play();

        // Fade awal ke volume target
        StartCoroutine(FadeToTargetVolume(activeBGMSource, volume, fadeDuration));

        // Mulai monitoring loop berikutnya
        loopingBGMFadeCoroutine = StartCoroutine(HandleBGMLoopingFade(clip, volume, fadeDuration));
    }

    private IEnumerator FadeToTargetVolume(AudioSource source, float targetVolume, float duration)
    {
        if (source == null)
            yield break;

        float startVolume = source.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }

        source.volume = targetVolume;
    }

    private IEnumerator HandleBGMLoopingFade(AudioClip clip, float targetVolume, float fadeDuration)
    {
        while (true)
        {
            if (activeBGMSource == null || activeBGMSource.clip == null)
            {
                yield return null;
                continue;
            }

            // Jika sudah mulai fade sebelumnya, tunggu sampai selesai lalu lanjut
            if (isLoopCrossfading)
            {
                yield return null;
                continue;
            }

            float timeRemaining = activeBGMSource.clip.length - activeBGMSource.time;

            if (timeRemaining <= fadeDuration)
            {
                isLoopCrossfading = true;

                AudioSource nextSource = (activeBGMSource == bgmSource1) ? bgmSource2 : bgmSource1;
                nextSource.clip = clip;
                nextSource.time = 0f;
                nextSource.volume = 0f;
                nextSource.Play();

                // Fade-out source lama dan fade-in source baru
                float startOldVolume = activeBGMSource.volume;
                float startNewVolume = nextSource.volume;
                float timer = 0f;

                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    float t = Mathf.Clamp01(timer / fadeDuration);
                    activeBGMSource.volume = Mathf.Lerp(startOldVolume, 0f, t);
                    nextSource.volume = Mathf.Lerp(startNewVolume, targetVolume, t);
                    yield return null;
                }

                activeBGMSource.volume = 0f;
                nextSource.volume = targetVolume;

                activeBGMSource.Stop();
                activeBGMSource = nextSource;
                isLoopCrossfading = false;
            }

            yield return null;
        }
    }

    private IEnumerator FadeVolume(AudioSource fadeInSource, AudioSource fadeOutSource, float duration)
    {
        float startFadeOutVolume = fadeOutSource.volume;
        float startFadeInVolume = fadeInSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            fadeInSource.volume = Mathf.Lerp(startFadeInVolume, bgmVolume, timer / duration);
            fadeOutSource.volume = Mathf.Lerp(startFadeOutVolume, 0f, timer / duration);
            yield return null;
        }

        fadeInSource.volume = bgmVolume;
        fadeOutSource.volume = 0f;
        fadeOutSource.Stop();
    }

    public void SetLayerVolume(string layerName, float volume)
    {
        AudioLayer layer = audioLayers.Find(item => item != null && item.layerName == layerName);
        if (layer == null || layer.source == null)
            return;

        layer.volume = Mathf.Clamp01(volume);
        ApplyLayerVolume(layer);
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (activeBGMSource != null)
            activeBGMSource.volume = bgmVolume; 
    }

    public void PlayLayer(string layerName)
    {
        AudioLayer layer = audioLayers.Find(item => item != null && item.layerName == layerName);
        if (layer == null || layer.source == null)
            return;

        if (layer.clip != null)
            layer.source.clip = layer.clip;

        ApplyLayerVolume(layer);
        layer.source.Play();
    }

    public void StopLayer(string layerName)
    {
        AudioLayer layer = audioLayers.Find(item => item != null && item.layerName == layerName);
        if (layer != null && layer.source != null)
            layer.source.Stop();
    }

    private void ApplyLayerVolume(AudioLayer layer)
    {
        layer.source.volume = Mathf.Clamp01(layer.volume);
    }

    private void ApplyBGMVolume()
    {
        if (activeBGMSource != null)
            activeBGMSource.volume = Mathf.Clamp01(bgmVolume);
    }

    public void ToggleAudio()
    {
        isMuted = !isMuted;
        AudioListener.volume = isMuted ? 0f : 1f;
    }
}

// DEFINISI KELAS AUDIOLAYER YANG HILANG DITAMBAHKAN DI SINI
[System.Serializable]
public class AudioLayer
{
    public string layerName;
    public AudioSource source;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool playOnStart = false;
}