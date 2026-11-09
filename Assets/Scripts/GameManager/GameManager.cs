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

    // GameManager duplikat (bawaan prefab System_PauseAudio di scene baru) tidak di-destroy,
    // melainkan menjadi proxy, karena tombol Resume/Save/MainMenu di Canvas_Pause scene tersebut
    // menargetkan komponen ini. Proxy meneruskan panggilan ke instance persistent.
    private bool isProxy = false;

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
            isProxy = true;
            return;
        }
    }

    private void OnEnable()
    {
        if (isProxy) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RemoveStrayPersistentPauseCanvases();
        pauseMenuUI = FindPauseMenuUI();
        ResumeGame();
    }

    private void Start()
    {
        if (isProxy) return;
        if (pauseMenuUI == null) pauseMenuUI = FindPauseMenuUI();
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Pengaman: hapus Canvas_Pause / System_PauseAudio yang "nyangkut" di DontDestroyOnLoad
    /// (penyebab pause menu terduplikasi saat teleport pulau).
    /// </summary>
    private void RemoveStrayPersistentPauseCanvases()
    {
        Scene ddol = gameObject.scene; // scene DontDestroyOnLoad
        foreach (GameObject root in ddol.GetRootGameObjects())
        {
            if (root == null || root == gameObject) continue;
            if (root.name == "System_PauseAudio" || root.name == "Canvas_Pause")
            {
                Debug.Log($"[GameManager] Menghapus pause canvas duplikat yang terbawa antar scene: {root.name}");
                Destroy(root);
            }
        }
    }

    private GameObject FindPauseMenuUI()
    {
        // Utamakan Canvas_Pause milik scene aktif (bukan yang di DontDestroyOnLoad)
        Scene active = SceneManager.GetActiveScene();
        foreach (GameObject root in active.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if ((child.name == "PauseMenuUI" || child.name == "PauseMenuPanel") &&
                    child.GetComponentInParent<Canvas>(true) != null &&
                    child.GetComponentInParent<Canvas>(true).name == "Canvas_Pause")
                    return child.gameObject;
            }
        }

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
        if (isProxy) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isProxy && _instance != null && _instance != this) { _instance.PauseGame(); return; }

        if (pauseMenuUI == null) pauseMenuUI = FindPauseMenuUI();
        isPaused = true;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (isProxy && _instance != null && _instance != this) { _instance.ResumeGame(); return; }

        isPaused = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
    }

    public void BackToMainMenu()
    {
        if (isProxy && _instance != null && _instance != this) { _instance.BackToMainMenu(); return; }

        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("01_MainMenu");
    }

    // FUNGSI SAVE
    public void SaveGame()
    {
        if (isProxy && _instance != null && _instance != this) { _instance.SaveGame(); return; }

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
