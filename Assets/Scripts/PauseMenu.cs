using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject container;
void Update()
{
    // Ubah dari KeyCode.F menjadi KeyCode.Escape
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        container.SetActive(true);
        Time.timeScale = 0; // Menghentikan waktu saat pause[cite: 2]
    }
}

    public void ResumeButton()
    {
        container.SetActive(false);
        Time.timeScale = 1;
    }

    public void MainMenuButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
