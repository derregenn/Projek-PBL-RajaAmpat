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
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; 
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

    // 🔴 FUNGSI SAVE (Dipanggil saat pindah pulau atau checkpoint)
    public void SaveGame()
    {
        // 1. Simpan Data Level/Pulau saat ini
        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("SavedScene", currentScene);
        
        // 2. Simpan Data Fase Cerita
        if (StoryManager.Instance != null)
        {
            PlayerPrefs.SetInt("SavedStoryPhase", (int)StoryManager.Instance.currentPhase);
        }

        // 3. Simpan Posisi Player (Jika ada di Scene)
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

        // 4. Eksekusi Save
        PlayerPrefs.Save(); 
        Debug.Log("Game Berhasil Disimpan di Scene: " + currentScene);
    }

    // 🟢 FUNGSI LOAD (Dipanggil SETELAH scene termuat)
    public void LoadGame()
    {
        // Load data cerita
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.currentPhase = (StoryManager.StoryPhase)PlayerPrefs.GetInt("SavedStoryPhase", 0);
        }

        // Load posisi Player
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            if (PlayerPrefs.HasKey("PlayerX"))
            {
                float x = PlayerPrefs.GetFloat("PlayerX");
                float y = PlayerPrefs.GetFloat("PlayerY");
                float z = PlayerPrefs.GetFloat("PlayerZ");

                // Matikan sementara komponen seperti Rigidbody/CharacterController (jika ada)
                // agar posisi bisa diset dengan paksa tanpa intervensi physics
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