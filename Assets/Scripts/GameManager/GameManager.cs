using System.Reflection;
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
        SceneManager.LoadScene("01_MainMenu");
    }

    public void SaveGame()
    {
        Player player = FindFirstObjectByType<Player>();

        if (player != null)
        {
            // Simpan posisi Player (X, Y, Z)
            PlayerPrefs.SetFloat("PlayerX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerZ", player.transform.position.z);

            // Simpan data kesehatan jika Player memiliki field/property health.
            PlayerPrefs.SetInt("PlayerHealth", GetPlayerHealth(player, 100));

            PlayerPrefs.Save();
            Debug.Log("Game Berhasil Disimpan!");
        }
        else
        {
            Debug.LogWarning("Objek Player tidak ditemukan untuk disimpan.");
        }

        PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);

        // Menyimpan fase cerita saat ini
        if (StoryManager.Instance != null)
        {
            PlayerPrefs.SetInt("SavedStoryPhase", (int)StoryManager.Instance.currentPhase);
        }

        // Wajib dipanggil agar data benar-benar tersimpan, terutama untuk WebGL
        PlayerPrefs.Save();

        Debug.Log("Game Berhasil Disimpan beserta status Scene & Story Phase!");
    }

    public void LoadGame()
    {
        Player player = FindFirstObjectByType<Player>();

        if (player != null)
        {
            if (PlayerPrefs.HasKey("PlayerX"))
            {
                float x = PlayerPrefs.GetFloat("PlayerX");
                float y = PlayerPrefs.GetFloat("PlayerY");
                float z = PlayerPrefs.GetFloat("PlayerZ");

                player.transform.position = new Vector3(x, y, z);
                SetPlayerHealth(player, PlayerPrefs.GetInt("PlayerHealth", 100));

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

    private int GetPlayerHealth(Player player, int defaultValue)
    {
        if (player == null) return defaultValue;

        FieldInfo field = typeof(Player).GetField("health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(int))
        {
            return (int)field.GetValue(player);
        }

        PropertyInfo property = typeof(Player).GetProperty("health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null && property.PropertyType == typeof(int) && property.CanRead)
        {
            return (int)property.GetValue(player);
        }

        return defaultValue;
    }

    private void SetPlayerHealth(Player player, int value)
    {
        if (player == null) return;

        FieldInfo field = typeof(Player).GetField("health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(int))
        {
            field.SetValue(player, value);
            return;
        }

        PropertyInfo property = typeof(Player).GetProperty("health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null && property.PropertyType == typeof(int) && property.CanWrite)
        {
            property.SetValue(player, value);
        }
    }
}