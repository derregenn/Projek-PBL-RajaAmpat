using UnityEngine;

public class NextLevel : MonoBehaviour
{
    public string nextLevelName;
    public void LoadNextLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Level2");
        Time.timeScale = 1;
    }
}
