using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SeaSceneTransition : MonoBehaviour
{
    [Header("Pengaturan Scene")]
    public string targetSceneName = "07_Laut";
    public float fadeDuration = 1.0f;

    [Header("UI & Input Settings")]
    public GameObject warningUI; // Tarik objek Teks Warning / Prompt "Tekan E untuk Masuk Laut"
    public KeyCode interactKey = KeyCode.E;
    public Image fadeBlackScreen; // Tarik objek Image Hitam ke slot ini

    private bool isPlayerInRange = false;
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

        // Pastikan UI warning mati di awal game
        if (warningUI != null)
        {
            warningUI.SetActive(false);
        }
    }

    private void Update()
    {
        // Cek jika pemain berada di area, belum transisi, dan menekan tombol E
        if (isPlayerInRange && !isTransitioning && Input.GetKeyDown(interactKey))
        {
            if (warningUI != null)
            {
                warningUI.SetActive(false);
            }

            Debug.Log("--> Player menekan " + interactKey + ". Melakukan transisi ke: " + targetSceneName);
            StartCoroutine(FadeAndLoadScene());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTransitioning) return;

        if (collision.CompareTag("Player") || collision.gameObject.name.Contains("Player"))
        {
            isPlayerInRange = true;

            // Munculkan teks warning saat pemain menyentuh batas laut
            if (warningUI != null)
            {
                warningUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.gameObject.name.Contains("Player"))
        {
            isPlayerInRange = false;

            // Sembunyikan teks warning saat pemain menjauh
            if (warningUI != null)
            {
                warningUI.SetActive(false);
            }
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