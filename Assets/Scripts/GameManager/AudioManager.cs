using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class AudioLayer
    {
        public string layerName;
        public AudioSource source;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool playOnStart;
    }

    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;

    [Header("Audio Clips")]
    public AudioClip defaultBGM;
    [Range(0f, 1f)] public float bgmVolume = 1f;

    [Header("Additional Audio Layers")]
    public List<AudioLayer> audioLayers = new List<AudioLayer>();

    private bool isMuted = false;

    private void Awake()
    {
        DontDestroyOnLoad(transform.root.gameObject);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (bgmSource != null && defaultBGM != null)
        {
            bgmSource.clip = defaultBGM;
            ApplyBGMVolume();
            bgmSource.Play();
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
        ApplyBGMVolume();
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
        if (bgmSource != null)
            bgmSource.volume = Mathf.Clamp01(bgmVolume);
    }

    public void ToggleAudio()
    {
        isMuted = !isMuted;
        AudioListener.volume = isMuted ? 0f : 1f;
    }
}