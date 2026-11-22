using UnityEngine;
using UnityEngine.Audio;

public class SeamlessLoopCrossfade : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip audioClip;
    public AudioMixerGroup outputMixerGroup;
    [Range(0f, 1f)] public float maxVolume = 0.5f;
    public float crossfadeDuration = 2.0f;
    public bool playOnStart = true;

    [Header("3D Spatial Settings (Centang Khusus Ambience)")]
    public bool is3DSpatial = false;
    public float minDistance = 5f;
    public float maxDistance = 30f;

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

        if (currentTime >= nextStartTime - 0.5 && !sources[1 - activeIndex].isPlaying)
        {
            int nextIndex = 1 - activeIndex;
            sources[nextIndex].volume = 0f;
            sources[nextIndex].PlayScheduled(nextStartTime);
        }

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