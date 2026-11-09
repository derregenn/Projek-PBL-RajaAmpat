using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pause manager per-scene.
/// PENTING: Komponen ini menempel di root prefab System_PauseAudio (yang berisi Canvas_Pause).
/// Dulu memanggil DontDestroyOnLoad sehingga seluruh Canvas_Pause ikut terbawa saat teleport pulau
/// dan menumpuk dengan Canvas_Pause milik scene baru (pause menu terduplikasi).
/// Sekarang komponen ini hanya hidup di scene-nya sendiri, dan jika GameManager (persistent) ada,
/// input Escape diserahkan sepenuhnya ke GameManager agar tidak ada dua handler yang toggle bersamaan.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI Reference")]
    [Tooltip("Drag PauseMenuPanel ke sini")]
    public GameObject pauseMenuUI; 

    private bool isPaused = false;

    private void Awake()
    {
        // Instance scene-lokal: instance dari scene sebelumnya sudah ter-destroy saat scene berganti.
        if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene)
        {
            // Duplikat di scene yang sama: cukup matikan komponen ini, JANGAN destroy gameObject
            // (gameObject ini adalah root System_PauseAudio yang berisi Canvas_Pause & AudioManager).
            enabled = false;
            return;
        }

        Instance = this;
        ResolvePauseMenuReference();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        ResolvePauseMenuReference();
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
    }

    private void ResolvePauseMenuReference()
    {
        if (pauseMenuUI == null || pauseMenuUI.GetComponent<Canvas>() == null)
            return;

        Transform[] descendants = pauseMenuUI.GetComponentsInChildren<Transform>(true);
        Transform pausePanel = null;

        foreach (Transform child in descendants)
        {
            if (child.name == "PauseMenuUI" || child.name == "PauseMenuPanel")
            {
                pausePanel = child;
                break;
            }
        }

        if (pausePanel == null)
        {
            Debug.LogError(
                "[PauseManager] pauseMenuUI points to a Canvas, but no PauseMenuUI / PauseMenuPanel child was found. Assign the pause panel instead.",
                this);
            pauseMenuUI = null;
            return;
        }

        pauseMenuUI = pausePanel.gameObject;
    }

    private bool GameManagerHandlesPause => GameManager.Instance != null && GameManager.Instance.isActiveAndEnabled;

    private void Update()
    {
        // GameManager sudah menangani Escape secara global; hindari toggle ganda.
        if (GameManagerHandlesPause) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        if (GameManagerHandlesPause)
        {
            GameManager.Instance.PauseGame();
            return;
        }

        isPaused = true;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (GameManagerHandlesPause)
        {
            GameManager.Instance.ResumeGame();
            return;
        }

        isPaused = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
    }
}
