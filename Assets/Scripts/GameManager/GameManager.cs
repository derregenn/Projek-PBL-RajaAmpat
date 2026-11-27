using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("UI References")]
    public GameObject pauseMenuUI;
    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            TriviaQuizUI sceneTrivia = GetComponent<TriviaQuizUI>();
            if (sceneTrivia != null)
            {
                TriviaQuizUI.AdoptOrAttachTo(_instance.gameObject, sceneTrivia);
            }
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        pauseMenuUI = FindPauseMenuUI();
        ResumeGame();

        if (PlayerPrefs.GetInt("ShouldRestoreSavedPosition", 0) == 1 &&
            PlayerPrefs.GetString("SavedScene", "") == scene.name)
        {
            PlayerPrefs.SetInt("ShouldRestoreSavedPosition", 0);
            PlayerPrefs.Save();
            LoadGame();
        }
    }

    private void Start()
    {
        pauseMenuUI = FindPauseMenuUI();
        ResumeGame();
    }

    public GameObject FindPauseMenuUI()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject foundUI = null;
        GameObject pauseCanvas = null;

        if (scene.isLoaded)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in descendants)
                {
                    if (child.name == "PauseMenuUI" || child.name == "PauseMenuPanel")
                    {
                        foundUI = child.gameObject;
                        break;
                    }

                    if (child.name == "Canvas_Pause")
                    {
                        pauseCanvas = child.gameObject;
                    }
                }

                if (foundUI != null) break;
            }
        }

        if (foundUI == null && pauseCanvas != null)
        {
            foundUI = pauseCanvas;
        }

        if (foundUI != null)
        {
            NormalizePauseCanvasLayout(foundUI);

            // Pastikan PauseMenuBinder terpasang agar event tombol tidak hilang saat ganti scene
            if (foundUI.GetComponent<PauseMenuBinder>() == null)
            {
                foundUI.AddComponent<PauseMenuBinder>();
            }

            Transform parentCanvas = foundUI.transform.parent;
            if (parentCanvas != null && parentCanvas.GetComponent<PauseMenuBinder>() == null)
            {
                parentCanvas.gameObject.AddComponent<PauseMenuBinder>();
            }
        }

        return foundUI;
    }

    private void NormalizePauseCanvasLayout(GameObject target)
    {
        if (target == null) return;

        Canvas canvas = target.GetComponentInParent<Canvas>(true);
        if (canvas != null && canvas.name.Contains("Canvas_Pause"))
        {
            UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        Transform scope = canvas != null ? canvas.transform : target.transform;
        foreach (Transform t in scope.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("TitleBackground") || t.name == "ControlHints" || t.name == "ControlsPanel")
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        bool currentlyPaused = isPaused || (PauseManager.Instance != null && PauseManager.Instance.IsPaused);
        if (currentlyPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void SetPausedState(bool paused)
    {
        isPaused = paused;
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        DeactivateAllPauseUIInActiveScene();

        if (pauseMenuUI == null || pauseMenuUI.name == "Canvas" || pauseMenuUI.name == "Canvas_HUD")
        {
            pauseMenuUI = FindPauseMenuUI();
        }

        if (pauseMenuUI != null)
        {
            NormalizePauseCanvasLayout(pauseMenuUI);
            Transform parentCanvas = pauseMenuUI.transform.parent;
            if (parentCanvas != null && parentCanvas.GetComponent<Canvas>() != null)
            {
                parentCanvas.gameObject.SetActive(true);
            }
            pauseMenuUI.SetActive(true);
        }

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.SetPausedState(true);
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuUI != null && pauseMenuUI.name != "Canvas" && pauseMenuUI.name != "Canvas_HUD")
        {
            pauseMenuUI.SetActive(false);
            Transform parentCanvas = pauseMenuUI.transform.parent;
            if (parentCanvas != null && parentCanvas.name.Contains("Canvas_Pause"))
            {
                parentCanvas.gameObject.SetActive(false);
            }
        }

        DeactivateAllPauseUIInActiveScene();

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.SetPausedState(false);
            if (PauseManager.Instance.pauseMenuUI != null &&
                PauseManager.Instance.pauseMenuUI.name != "Canvas" &&
                PauseManager.Instance.pauseMenuUI.name != "Canvas_HUD")
            {
                PauseManager.Instance.pauseMenuUI.SetActive(false);
            }
        }
    }

    private void DeactivateAllPauseUIInActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.isLoaded)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in descendants)
                {
                    if (child.name == "PauseMenuUI" || child.name == "PauseMenuPanel")
                    {
                        child.gameObject.SetActive(false);
                    }
                    else if (child.name == "Canvas_Pause")
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        // Matikan Canvas_Pause nyasar di DontDestroyOnLoad agar tidak pernah double
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c != null && c.name == "Canvas_Pause" && c.gameObject.scene != scene)
            {
                c.gameObject.SetActive(false);
            }
        }

        // Matikan prefab pause hijau lama (Assets/Prefabs_Reg/PauseMenu.prefab) jika masih ada di scene
        foreach (PauseMenu legacyPause in FindObjectsByType<PauseMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (legacyPause != null)
            {
                if (legacyPause.container != null) legacyPause.container.SetActive(false);
                legacyPause.gameObject.SetActive(false);
            }
        }
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (PauseManager.Instance != null) PauseManager.Instance.SetPausedState(false);
        SceneManager.LoadScene("01_MainMenu");
    }

    // FUNGSI SAVE
    public void SaveGame()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Jangan simpan scene transisi atau menu sebagai checkpoint
        if (currentScene.Contains("MainMenu") || currentScene.Contains("Cutscene_Boat"))
        {
            return;
        }

        if (currentScene == "11_Waisai Ending" || currentScene == "12_CutsceneAirstripEnding" || currentScene == "02_CutsceneAirstrip")
        {
            if (FreeRoamManager.IsFreeRoamUnlocked())
            {
                PlayerPrefs.SetString("SavedScene", "02_Waisai");
                PlayerPrefs.SetInt("ShouldRestoreSavedPosition", 0);
                PlayerPrefs.DeleteKey("PlayerX");
                PlayerPrefs.DeleteKey("PlayerY");
                PlayerPrefs.DeleteKey("PlayerZ");
                PlayerPrefs.Save();
            }
            return;
        }

        PlayerPrefs.SetString("SavedScene", currentScene);

        if (StoryManager.Instance != null)
        {
            PlayerPrefs.SetInt("SavedStoryPhase", (int)StoryManager.Instance.currentPhase);
        }

        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            PlayerPrefs.SetFloat("PlayerX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerZ", player.transform.position.z);
        }

        PlayerPrefs.Save();
        Debug.Log($"[GameManager] Game Berhasil Disimpan di Scene: {currentScene}");
    }

    // FUNGSI LOAD
    public void LoadGame()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.currentPhase = (StoryManager.StoryPhase)PlayerPrefs.GetInt("SavedStoryPhase", 0);
        }

        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            if (PlayerPrefs.HasKey("PlayerX") && PlayerPrefs.HasKey("PlayerY"))
            {
                float x = PlayerPrefs.GetFloat("PlayerX");
                float y = PlayerPrefs.GetFloat("PlayerY");
                float z = PlayerPrefs.GetFloat("PlayerZ", 0f);

                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.position = new Vector2(x, y);
                }
                player.transform.position = new Vector3(x, y, z);
                Debug.Log("[GameManager] Kedudukan Player Berhasil Dimuat!");
            }
        }
    }
}
