using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject container;
    private void Update()
    {
        if (GameManager.Instance != null || PauseManager.Instance != null)
            return;

        if (container != null && Input.GetKeyDown(KeyCode.Escape))
        {
            bool shouldPause = !container.activeSelf;
            container.SetActive(shouldPause);
            Time.timeScale = shouldPause ? 0f : 1f;
        }
    }

    public void ResumeButton()
    {
        if (container != null) container.SetActive(false);
        Time.timeScale = 1;
    }

    public void MainMenuButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
