using UnityEngine;
using UnityEngine.SceneManagement;

public class MapTeleport : MonoBehaviour
{
    public void PindahKePulau(string namaSceneTujuan)
    {
        Time.timeScale = 1f;

        int hasVisited = PlayerPrefs.GetInt("Visited_" + namaSceneTujuan, 0);

        if (hasVisited == 0)
        {
            if (GamePhaseManager.Instance != null)
            {
                GamePhaseManager.Instance.targetIslandName = namaSceneTujuan;
            }

            SceneManager.LoadScene("Cutscene_BoatToIsland");
        }
        else
        {
            SceneManager.LoadScene(namaSceneTujuan);
        }
    }
}


/*

using UnityEngine;
using UnityEngine.SceneManagement;

public class MapTeleport : MonoBehaviour
{
    public void PindahKePulau(string namaSceneTujuan)
    {
        Time.timeScale = 1f; 
        
        SceneManager.LoadScene(namaSceneTujuan);
    }
}

*/