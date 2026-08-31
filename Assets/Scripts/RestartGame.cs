using UnityEngine;

public class Restart : MonoBehaviour
{
    public void LoadCurrentScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        Time.timeScale = 1;
    }
}
