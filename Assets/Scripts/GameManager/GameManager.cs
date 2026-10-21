using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public GameObject pauseMenuUI;
    private bool isPaused = false;

    void Awake()
    {
        // Memastikan hanya ada 1 GameManager (Singleton)
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); // Lepas dari parent agar DontDestroyOnLoad berjalan sempurna
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Daftarkan event setiap kali scene baru selesai dimuat
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Hapus event saat object dihancurkan
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 🔄 FUNGSI INI OTOMATIS DIPANGGIL SETIAP KALI MASUK SCENE BARU
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Jika referensi pauseMenuUI hilang/null di scene baru, cari ulang secara otomatis
        if (pauseMenuUI == null)
        {
            GameObject foundPause = GameObject.Find("PauseMenuUI");
            if (foundPause != null) 
            {
                pauseMenuUI = foundPause;
            }
        }

        // 2. Paksa matikan Pause Menu & reset waktu game setiap kali masuk scene baru
        ResumeGame();
    }

    private void Update()
    {
        // Toggle Pause Menu dengan tombol Escape
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
            Debug.LogWarning("GameManager: Objek Player tidak ditemukan. Posisi tidak disimpan.");
        }

        PlayerPrefs.Save(); 
        Debug.Log("Game Berhasil Disimpan di Scene: " + currentScene);
    }

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
                Debug.Log("GameManager: Posisi Player Berhasil Dimuat!");
            }
            else
            {
                Debug.LogWarning("GameManager: Tidak ada data posisi yang tersimpan.");
            }
        }
        else
        {
            Debug.LogWarning("GameManager: Objek Player tidak ditemukan untuk diposisikan ulang.");
        }
    }
}