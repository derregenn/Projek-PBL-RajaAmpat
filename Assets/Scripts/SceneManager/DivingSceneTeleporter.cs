using UnityEngine;

public class DivingSceneTeleporter : MonoBehaviour
{
    [Header("Nama Scene Laut")]
    [SerializeField] private string sceneLautUtama = "08_Laut_Misool"; // Poin 3 & Poin 5
    [SerializeField] private string sceneLautManta = "08_Laut_Manta";  // Poin 8

    /// <summary>
    /// Panggil fungsi ini dari Event E / FaderTrigger saat pemain menyelam
    /// </summary>
    public void StartDivingTransition()
    {
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);
        string targetScene = sceneLautUtama;

        // Jika pemain di Stage 7 (setelah dialog Ibu-ibu di Poin 7), arahkan ke Scene Manta
        if (stage == 7)
        {
            targetScene = sceneLautManta;
        }
        else
        {
            // Stage 2 (Monolog), Stage 4 (Bulu Seribu), atau Free Roam
            targetScene = sceneLautUtama;
        }

        if (UniversalFader.Instance != null)
        {
            UniversalFader.Instance.FadeToScene(targetScene);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }
    }
}