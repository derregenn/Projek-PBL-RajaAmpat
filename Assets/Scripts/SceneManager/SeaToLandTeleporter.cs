using UnityEngine;

public class SeaToLandTeleporter : MonoBehaviour
{
    public enum SeaSceneType
    {
        SeaMain,  // Laut Utama (Monolog / Bulu Seribu)
        SeaManta  // Laut Manta
    }

    [SerializeField] private SeaSceneType currentSeaType;
    [SerializeField] private string targetLandScene = "04_Misool";
    [SerializeField] private SeaSceneTransition seaSceneTransition;

    public void ReturnToLand()
    {
        // 1. Catat asal scene laut sebelum pindah ke darat
        if (currentSeaType == SeaSceneType.SeaMain)
        {
            PlayerPrefs.SetString("Misool_LastExitPoint", "SEA_MAIN");
        }
        else if (currentSeaType == SeaSceneType.SeaManta)
        {
            PlayerPrefs.SetString("Misool_LastExitPoint", "SEA_MANTA");
        }

        PlayerPrefs.Save();

        // 2. Transisi Pindah Scene ke Darat
        if (seaSceneTransition == null)
        {
            seaSceneTransition = GetComponent<SeaSceneTransition>();
        }

        if (seaSceneTransition != null)
        {
            seaSceneTransition.SendMessage("TransitionToScene", targetLandScene, SendMessageOptions.DontRequireReceiver);
            return;
        }

        if (UniversalFader.Instance != null)
        {
            UniversalFader.Instance.FadeToScene(targetLandScene);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetLandScene);
        }
    }

}