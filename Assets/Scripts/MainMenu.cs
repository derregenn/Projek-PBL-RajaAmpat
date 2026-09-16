using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("02_Waisai");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
