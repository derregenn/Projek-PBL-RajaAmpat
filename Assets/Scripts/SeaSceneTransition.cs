using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SeaSceneTransition : MonoBehaviour
{
    [Header("Pengaturan Scene")]
    public string targetSceneName = "07_Laut";
    public float fadeDuration = 1.0f;

    [Header("UI Fade Settings")]
    public Image fadeBlackScreen; // Tarik objek Image Hitam ke slot ini

    private bool isTransitioning = false;

    private void Start()
    {
        // Memastikan tirai hitam transparan (Alpha = 0) saat awal scene
        if (fadeBlackScreen != null)
        {
            Color c = fadeBlackScreen.color;
            c.a = 0f;
            fadeBlackScreen.color = c;
            fadeBlackScreen.gameObject.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Cegah pemicu berjalan berulang kali saat proses transisi
        if (isTransitioning) return;

        if (collision.CompareTag("Player") || collision.gameObject.name.Contains("Player"))
        {
            Debug.Log("--> Player menyentuh pemicu. Melakukan transisi ke: " + targetSceneName);
            StartCoroutine(FadeAndLoadScene());
        }
    }

    private IEnumerator FadeAndLoadScene()
    {
        isTransitioning = true;

        // 1. Efek Memudar ke Layar Hitam (Fade Out)
        if (fadeBlackScreen != null)
        {
            float timer = 0f;
            Color c = fadeBlackScreen.color;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                fadeBlackScreen.color = c;
                yield return null;
            }

            c.a = 1f;
            fadeBlackScreen.color = c;
        }

        // 2. Simpan Progres Otomatis sebelum Pindah Scene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
        }

        // 3. Pindah ke Scene Tujuan
        SceneManager.LoadScene(targetSceneName);
    }
}