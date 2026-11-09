using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Events;

public class SeaSceneTransition : MonoBehaviour
{
    [Header("Pengaturan Scene")]
    public string targetSceneName = "07_Laut";
    public float fadeDuration = 1.0f;

    [Header("UI & Input Settings")]
    public GameObject warningUI; // Objek Teks Warning / Prompt "Tekan E untuk Masuk Laut"
    public KeyCode interactKey = KeyCode.E;
    public Image fadeBlackScreen; // Objek Image Hitam

    private bool isPlayerInRange = false;
    private bool isTransitioning = false;
    private SeaToLandTeleporter returnTeleporter;
    public UnityEvent onInteractComplete;

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

            Debug.Log($"[SeaSceneTransition] Player menekan {interactKey}. Transisi ke: {targetSceneName}");
            
            // Persiapkan exit point dan stage pada teleporter tanpa memanggil LoadScene ganda
            if (returnTeleporter != null)
            {
                returnTeleporter.PrepareReturn();
            }

            StartCoroutine(FadeAndLoadScene());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTransitioning) return;

        bool isPlayer = collision.CompareTag("Player") ||
                        (collision.transform.root != null && collision.transform.root.CompareTag("Player")) ||
                        collision.gameObject.name.ToLower().Contains("player");

        if (isPlayer)
        {
            isPlayerInRange = true;

            returnTeleporter = GetComponent<SeaToLandTeleporter>();
            if (returnTeleporter == null)
            {
                returnTeleporter = GetComponentInParent<SeaToLandTeleporter>();
            }
            if (returnTeleporter == null)
            {
                returnTeleporter = collision.GetComponent<SeaToLandTeleporter>();
            }

            if (warningUI != null)
            {
                warningUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        bool isPlayer = collision.CompareTag("Player") ||
                        (collision.transform.root != null && collision.transform.root.CompareTag("Player")) ||
                        collision.gameObject.name.ToLower().Contains("player");

        if (isPlayer)
        {
            isPlayerInRange = false;
            returnTeleporter = null;

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
                timer += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(timer / fadeDuration);
                fadeBlackScreen.color = c;
                yield return null;
            }

            c.a = 1f;
            fadeBlackScreen.color = c;
        }

        if (QuestController.Instance != null)
        {
            if (!string.IsNullOrEmpty(targetSceneName) && targetSceneName.Contains("Misool"))
            {
                QuestController.Instance.ProgressObjective("laut_misool", 1);
            }
            QuestController.Instance.CompleteAllActiveQuests();
        }

        // 2. Simpan Progres Otomatis sebelum Pindah Scene (hindari koordinat tepat di trigger exit)
        PlayerPrefs.SetString("SavedScene", targetSceneName);
        PlayerPrefs.DeleteKey("PlayerX");
        PlayerPrefs.DeleteKey("PlayerY");
        PlayerPrefs.DeleteKey("PlayerZ");
        PlayerPrefs.Save();

        // 3. Pindah ke Scene Tujuan
        Debug.Log($"[SeaSceneTransition] Memuat scene tujuan: {targetSceneName}");
        SceneManager.LoadScene(targetSceneName);
    }

    public void SelesaiInteraksi()
    {
        Debug.Log("[SeaSceneTransition] Interaksi Selesai!");
        onInteractComplete?.Invoke();
    }
}
