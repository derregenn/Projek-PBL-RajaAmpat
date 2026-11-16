using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject container;

    private void Awake()
    {
        EnsureBinder();
    }

    private void Start()
    {
        EnsureBinder();
    }

    private void EnsureBinder()
    {
        if (GetComponent<PauseMenuBinder>() == null)
        {
            gameObject.AddComponent<PauseMenuBinder>();
        }

        if (container != null && container.GetComponent<PauseMenuBinder>() == null)
        {
            container.AddComponent<PauseMenuBinder>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Jika ada GameManager atau PauseManager yang aktif, biarkan mereka yang menangani ESC
            if (GameManager.Instance != null || PauseManager.Instance != null)
                return;

            if (container != null)
            {
                bool shouldPause = !container.activeSelf;
                container.SetActive(shouldPause);
                Time.timeScale = shouldPause ? 0f : 1f;
            }
        }
    }

    public void ResumeButton()
    {
        if (container != null) container.SetActive(false);
        gameObject.SetActive(false);

        if (PauseManager.Instance != null) PauseManager.Instance.ResumeGame();
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();

        Time.timeScale = 1f;
    }

    public void MainMenuButton()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BackToMainMenu();
        }
        else
        {
            SceneManager.LoadScene("01_MainMenu");
        }
    }
}
