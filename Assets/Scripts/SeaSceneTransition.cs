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
    public AudioClip seaTransitionSfx;

    [Header("Quest Progression Gate (Opsional)")]
    [SerializeField] private string requiredActiveObjectiveID = "";
    [SerializeField] private string requiredActiveOrCompletedQuestID = "";
    [SerializeField] private string requiredCompletedQuestID = "";

    private bool isPlayerInRange = false;
    private bool isTransitioning = false;
    private bool isShowingWarning = false;
    private SeaToLandTeleporter returnTeleporter;
    public UnityEvent onInteractComplete;

    private void SetWarningVisible(bool visible)
    {
        if (visible)
        {
            if (warningUI != null && !warningUI.activeSelf) warningUI.SetActive(true);
            isShowingWarning = warningUI != null;
        }
        else
        {
            if (isShowingWarning && warningUI != null && warningUI.activeSelf) warningUI.SetActive(false);
            isShowingWarning = false;
        }
    }

    private static bool IsPlayerCollider(Collider2D collision)
    {
        return collision.CompareTag("Player")
            || (collision.transform.root != null && collision.transform.root.CompareTag("Player"))
            || collision.gameObject.name.ToLower().Contains("player");
    }

    private void ResolveReturnTeleporter(Collider2D collision)
    {
        if (returnTeleporter != null) return;
        returnTeleporter = GetComponent<SeaToLandTeleporter>();
        if (returnTeleporter == null)
        {
            returnTeleporter = GetComponentInParent<SeaToLandTeleporter>();
        }
        if (returnTeleporter == null && collision != null)
        {
            returnTeleporter = collision.GetComponent<SeaToLandTeleporter>();
        }
    }

    private void Start()
    {
        // Memastikan tirai hitam transparan (Alpha = 0) saat awal scene (kecuali sedang transisi)
        if (!isTransitioning && fadeBlackScreen != null)
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
        isShowingWarning = false;
    }

    private void Update()
    {
        // Cek jika pemain berada di area, belum transisi, memenuhi syarat quest, dan tidak ada modal aktif
        if (isPlayerInRange && !isTransitioning)
        {
            if (InteractionInputLock.IsAnyModalBlockingWorldInteract
                || InteractionInputLock.IsSignboardOpen
                || !QuestProgressionGate.Passes(requiredActiveObjectiveID, requiredActiveOrCompletedQuestID, requiredCompletedQuestID))
            {
                SetWarningVisible(false);
                return;
            }

            SetWarningVisible(true);

            bool isReturnTransition = returnTeleporter != null;
            if ((!InteractionInputLock.WasInteractKeyConsumedThisFrame || isReturnTransition)
                && Input.GetKeyDown(interactKey))
            {
                InteractionInputLock.ConsumeInteractKeyThisFrame();
                SetWarningVisible(false);
                TeleportKeSceneTujuan();
            }
        }
        else if (isShowingWarning)
        {
            SetWarningVisible(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTransitioning) return;

        if (IsPlayerCollider(collision))
        {
            isPlayerInRange = true;
            ResolveReturnTeleporter(collision);

            if (InteractionInputLock.IsAnyModalBlockingWorldInteract
                || InteractionInputLock.IsSignboardOpen
                || !QuestProgressionGate.Passes(requiredActiveObjectiveID, requiredActiveOrCompletedQuestID, requiredCompletedQuestID))
            {
                SetWarningVisible(false);
                return;
            }

            SetWarningVisible(true);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isTransitioning) return;

        if (IsPlayerCollider(collision))
        {
            isPlayerInRange = true;
            ResolveReturnTeleporter(collision);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsPlayerCollider(collision))
        {
            isPlayerInRange = false;
            returnTeleporter = null;
            SetWarningVisible(false);
        }
    }

    private void OnDisable()
    {
        isPlayerInRange = false;
        returnTeleporter = null;
        SetWarningVisible(false);
    }

    private IEnumerator FadeAndLoadScene()
    {
        isTransitioning = true;

        // 1. Efek Memudar ke Layar Hitam (Fade Out)
        if (fadeBlackScreen != null)
        {
            fadeBlackScreen.gameObject.SetActive(true);
            CanvasGroup cg = fadeBlackScreen.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;

            float timer = 0f;
            Color c = fadeBlackScreen.color;
            c.a = 0f;
            fadeBlackScreen.color = c;

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
        if (seaTransitionSfx != null)
        {
            AudioSource.PlayClipAtPoint(seaTransitionSfx, transform.position);
            yield return new WaitForSecondsRealtime(seaTransitionSfx.length);
        }

        Debug.Log($"[SeaSceneTransition] Memuat scene tujuan: {targetSceneName}");
        SceneManager.LoadScene(targetSceneName);
    }

    public void SelesaiInteraksi()
    {
        Debug.Log("[SeaSceneTransition] Interaksi Selesai!");
        onInteractComplete?.Invoke();
    }

    public void TeleportKeSceneTujuan()
    {
        if (isTransitioning) return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("[SeaSceneTransition] Nama scene tujuan belum diatur.", this);
            return;
        }

        Debug.Log($"[SeaSceneTransition] Memulai transisi ke: {targetSceneName}");

        if (returnTeleporter != null)
        {
            returnTeleporter.PrepareReturn();
        }

        SetWarningVisible(false);
        StartCoroutine(FadeAndLoadScene());
    }
}
