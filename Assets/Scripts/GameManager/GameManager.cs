using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject pauseMenuUI;
    private bool isPaused = false;

    private void Awake()
    {
        // Logik Singleton: Pastikan hanya ada 1 GameManager di seluruh permainan
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Kekalkan GameObject ini apabila berpindah scene
        }
        else
        {
            Destroy(gameObject); // Musnahkan GameObject duplikat di scene baharu
            return;
        }
    }

    private void OnEnable()
    {
        // Daftar event untuk kesan pertukaran scene
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Dipanggil secara automatik setiap kali scene baharu selesai dimuatkan
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Cari UI Pause di scene baharu jika rujukan terputus
        if (pauseMenuUI == null)
        {
            GameObject canvas = GameObject.Find("Canvas_Pause") ?? GameObject.Find("Canvas");
            if (canvas != null)
            {
                Transform pausePanel = canvas.transform.Find("PauseMenuPanel") ?? canvas.transform.Find("PromptPanel");
                if (pausePanel != null)
                {
                    pauseMenuUI = pausePanel.gameObject;
                }
            }
        }

        // Reset semula masa dan status pause apabila berpindah scene
        ResumeGame();
    }

    private void Start()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // Toggle Pause Menu dengan punat Escape
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

    // 🔴 FUNGSI SAVE
    public void SaveGame()
    {
        string currentScene = SceneManager.GetActiveScene().name;
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
        else
        {
            Debug.LogWarning("GameManager: Objek Player tidak dijumpai. Kedudukan tidak disimpan.");
        }

        PlayerPrefs.Save();
        Debug.Log("Game Berjaya Disimpan di Scene: " + currentScene);
    }

    // 🟢 FUNGSI LOAD
    public void LoadGame()
    {
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.currentPhase = (StoryManager.StoryPhase)PlayerPrefs.GetInt("SavedStoryPhase", 0);
        }

        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            if (PlayerPrefs.HasKey("PlayerX"))
            {
                float x = PlayerPrefs.GetFloat("PlayerX");
                float y = PlayerPrefs.GetFloat("PlayerY");
                float z = PlayerPrefs.GetFloat("PlayerZ");

                player.transform.position = new Vector3(x, y, z);
                Debug.Log("GameManager: Kedudukan Player Berjaya Dimuatkan!");
            }
            else
            {
                Debug.LogWarning("GameManager: Tiada data kedudukan disimpan.");
            }
        }
        else
        {
            Debug.LogWarning("GameManager: Objek Player tidak dijumpai untuk disusun semula.");
        }
    }
}