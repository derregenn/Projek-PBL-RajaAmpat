using UnityEngine;
using UnityEngine.Audio;

public class SeamlessLoopCrossfade : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip audioClip;
    public AudioMixerGroup outputMixerGroup;
    [Range(0f, 1f)] public float maxVolume = 0.55f;
    public float crossfadeDuration = 1.5f;
    public bool playOnStart = true;

    [Header("Mid-Scene Trigger Settings (Khusus Pulau Gag)")]
    [Tooltip("Otomatis mendeteksi Audio Source lama di objek ini jika dikosongkan")]
    public AudioSource watchLegacyBgmSource;
    [Tooltip("Durasi fade-in saat BGM pertama kali muncul di tengah scene")]
    public float initialFadeInDuration = 1.5f;

    [Header("3D Spatial Settings (Centang Khusus Ambience)")]
    public bool is3DSpatial = false;
    public float minDistance = 5f;
    public float maxDistance = 30f;

    private AudioSource[] sources = new AudioSource[2];
    private int activeIndex = 0;
    private double startTime;
    private double nextStartTime;
    private bool isPlaying = false;
    private bool hasStarted = false;
    private bool waitingForClipLoad = false;

    void Awake()
    {
        // Otomatis ambil komponen AudioSource lama yang sudah ada di GameObject ini
        if (watchLegacyBgmSource == null)
        {
            watchLegacyBgmSource = GetComponent<AudioSource>();
        }

        if (audioClip != null && audioClip.loadState == AudioDataLoadState.Unloaded)
        {
            audioClip.LoadAudioData();
        }
    }

    void Start()
    {
        // Bisukan AudioSource lama agar tidak bunyi dobel saat diaktifkan trigger programmer
        if (watchLegacyBgmSource != null)
        {
            watchLegacyBgmSource.mute = true;
        }

        // Buat 2 AudioSource baru; jika playOnStart mati, nonaktifkan centangnya di awal
        for (int i = 0; i < 2; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].clip = audioClip;
            sources[i].outputAudioMixerGroup = outputMixerGroup;
            sources[i].playOnAwake = false;
            sources[i].loop = false;
            sources[i].volume = 0f;
            sources[i].enabled = playOnStart;

            if (is3DSpatial)
            {
                sources[i].spatialBlend = 1f;
                sources[i].rolloffMode = AudioRolloffMode.Logarithmic;
                sources[i].minDistance = minDistance;
                sources[i].maxDistance = maxDistance;
            }
        }

        if (playOnStart && audioClip != null)
        {
            initialFadeInDuration = 0f;
            StartLoop();
        }
    }

    public void StartLoop()
    {
        if (audioClip == null || hasStarted) return;

        if (audioClip.loadState == AudioDataLoadState.Unloaded)
        {
            audioClip.LoadAudioData();
        }

        if (audioClip.loadState != AudioDataLoadState.Loaded)
        {
            if (audioClip.loadState != AudioDataLoadState.Failed)
            {
                waitingForClipLoad = true;
            }
            return;
        }

        waitingForClipLoad = false;
        hasStarted = true;

        // Aktifkan centang kedua AudioSource baru tepat saat momen trigger terjadi
        for (int i = 0; i < 2; i++)
        {
            if (sources[i] != null)
            {
                sources[i].enabled = true;
            }
        }

        crossfadeDuration = Mathf.Clamp(crossfadeDuration, 0.05f, audioClip.length * 0.4f);
        initialFadeInDuration = Mathf.Min(initialFadeInDuration, audioClip.length - crossfadeDuration);

        startTime = AudioSettings.dspTime + 0.05;
        sources[0].volume = (initialFadeInDuration > 0f) ? 0f : maxVolume;
        sources[0].PlayScheduled(startTime);

        nextStartTime = startTime + audioClip.length - crossfadeDuration;
        activeIndex = 0;
        isPlaying = true;
    }

    void Update()
    {
        if (waitingForClipLoad && !hasStarted && audioClip != null)
        {
            if (audioClip.loadState == AudioDataLoadState.Loaded)
            {
                waitingForClipLoad = false;
                StartLoop();
            }
            else if (audioClip.loadState == AudioDataLoadState.Failed)
            {
                waitingForClipLoad = false;
            }
        }

        // Pantau AudioSource lama: begitu dicentang oleh trigger View Panning, langsung mulai!
        if (!hasStarted && !waitingForClipLoad && !playOnStart && watchLegacyBgmSource != null && watchLegacyBgmSource.enabled)
        {
            StartLoop();
        }

        if (!isPlaying || audioClip == null) return;

        double currentTime = AudioSettings.dspTime;

        // Fade-in halus saat pertama kali muncul di tengah scene
        if (initialFadeInDuration > 0f && currentTime >= startTime && currentTime < startTime + initialFadeInDuration)
        {
            float fadeInProgress = Mathf.Clamp01((float)((currentTime - startTime) / initialFadeInDuration));
            sources[0].volume = Mathf.Lerp(0f, maxVolume, fadeInProgress);
        }
        else if (currentTime < nextStartTime && sources[activeIndex].volume < maxVolume && activeIndex == 0)
        {
            sources[0].volume = maxVolume;
        }

        // Jadwalkan AudioSource kedua sebelum titik crossfade dimulai
        if (currentTime >= nextStartTime - 0.5 && !sources[1 - activeIndex].isPlaying)
        {
            int nextIndex = 1 - activeIndex;
            sources[nextIndex].volume = 0f;
            sources[nextIndex].PlayScheduled(nextStartTime);
        }

        // Crossfade looping
        if (currentTime >= nextStartTime)
        {
            float progress = Mathf.Clamp01((float)((currentTime - nextStartTime) / crossfadeDuration));
            sources[activeIndex].volume = Mathf.Lerp(maxVolume, 0f, progress);
            sources[1 - activeIndex].volume = Mathf.Lerp(0f, maxVolume, progress);

            if (progress >= 1f)
            {
                sources[activeIndex].Stop();
                activeIndex = 1 - activeIndex;
                nextStartTime += (audioClip.length - crossfadeDuration);
            }
        }
    }
}