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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
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
    }

    public void SetPausedState(bool paused)
    {
        isPaused = paused;
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuUI == null)
        {
            pauseMenuUI = FindPauseMenuUI();
        }

        if (pauseMenuUI != null)
        {
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

        if (pauseMenuUI != null)
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
            if (PauseManager.Instance.pauseMenuUI != null)
            {
                PauseManager.Instance.pauseMenuUI.SetActive(false);
            }
        }
    }

    private void DeactivateAllPauseUIInActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded) return;

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
