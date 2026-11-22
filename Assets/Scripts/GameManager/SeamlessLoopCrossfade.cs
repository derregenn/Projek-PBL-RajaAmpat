using UnityEngine;
using UnityEngine.Audio;

public class SeamlessLoopCrossfade : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip audioClip;
    public AudioMixerGroup outputMixerGroup; // Sambungkan ke Mixer BGM/Ambience agar ikut slider volume
    [Range(0f, 1f)] public float maxVolume = 0.6f;
    public float crossfadeDuration = 2.0f;
    public bool playOnStart = true;

    private AudioSource[] sources = new AudioSource[2];
    private int activeIndex = 0;
    private double nextStartTime;
    private bool isPlaying = false;

    void Start()
    {
        for (int i = 0; i < 2; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].clip = audioClip;
            sources[i].outputAudioMixerGroup = outputMixerGroup;
            sources[i].playOnAwake = false;
            sources[i].loop = false;
            sources[i].volume = 0f;
        }

        if (playOnStart && audioClip != null)
        {
            // Pastikan durasi fade tidak melebihi setengah panjang klip
            crossfadeDuration = Mathf.Min(crossfadeDuration, audioClip.length * 0.4f);
            StartLoop();
        }
    }

    public void StartLoop()
    {
        if (audioClip == null) return;

        double startTime = AudioSettings.dspTime + 0.1;
        sources[0].volume = maxVolume;
        sources[0].PlayScheduled(startTime);

        nextStartTime = startTime + audioClip.length - crossfadeDuration;
        activeIndex = 0;
        isPlaying = true;
    }

    void Update()
    {
        if (!isPlaying || audioClip == null) return;

        double currentTime = AudioSettings.dspTime;

        // Jadwalkan AudioSource kedua sebelum titik crossfade dimulai
        if (currentTime >= nextStartTime - 0.5 && !sources[1 - activeIndex].isPlaying)
        {
            int nextIndex = 1 - activeIndex;
            sources[nextIndex].volume = 0f;
            sources[nextIndex].PlayScheduled(nextStartTime);
        }

        // Lakukan fade-out pada source lama dan fade-in pada source baru
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