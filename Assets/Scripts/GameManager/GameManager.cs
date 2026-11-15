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
    }

    private void Start()
    {
        pauseMenuUI = FindPauseMenuUI();
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        isPaused = false;
        Time.timeScale = 1f;
    }

    private GameObject FindPauseMenuUI()
    {
        GameObject pauseCanvas = GameObject.Find("Canvas_Pause");
        if (pauseCanvas != null)
        {
            Transform[] descendants = pauseCanvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in descendants)
            {
                if (child.name == "PauseMenuUI" || child.name == "PauseMenuPanel")
                    return child.gameObject;
            }
        }

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            Transform[] descendants = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in descendants)
            {
                if (child.name == "PauseMenuUI" || child.name == "PauseMenuPanel")
                    return child.gameObject;
            }
        }

        return null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
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
