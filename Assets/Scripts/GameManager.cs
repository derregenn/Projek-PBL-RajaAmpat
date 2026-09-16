using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;
    private bool isPaused = false;

    private void Start()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
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
        SceneManager.LoadScene("01_MainMenu"); // Sesuaikan nama scene
    }

    public void SaveGame()
    {
        // Cari objek Player di dalam Scene
        Player player = FindFirstObjectByType<Player>();

        if (player != null)
        {
            // Simpan posisi Player (X, Y, Z)
            PlayerPrefs.SetFloat("PlayerX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerZ", player.transform.position.z);

            // Simpan data koin dan kesehatan
            PlayerPrefs.SetInt("PlayerHealth", player.health);

            // Tulis permanen ke sistem penyimpanan laptop
            PlayerPrefs.Save();

            Debug.Log("Game Berhasil Disimpan!");
        }
        else
        {
            Debug.LogWarning("Objek Player tidak ditemukan untuk disimpan.");
        }
    }

    public void LoadGame()
    {
        // Cari objek Player di dalam Scene
        Player player = FindFirstObjectByType<Player>();

        if (player != null)
        {
            // Cek apakah ada data tersimpan
            if (PlayerPrefs.HasKey("PlayerX"))
            {
                float x = PlayerPrefs.GetFloat("PlayerX");
                float y = PlayerPrefs.GetFloat("PlayerY");
                float z = PlayerPrefs.GetFloat("PlayerZ");

                player.transform.position = new Vector3(x, y, z);
                player.health = PlayerPrefs.GetInt("PlayerHealth", 100);

                Debug.Log("Game Berhasil Dimuat!");
            }
            else
            {
                Debug.LogWarning("Tidak ada data tersimpan untuk dimuat.");
            }
        }
        else
        {
            Debug.LogWarning("Objek Player tidak ditemukan untuk dimuat.");
        }
    }
}