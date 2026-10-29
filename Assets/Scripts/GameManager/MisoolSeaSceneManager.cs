using UnityEngine;

public class MisoolSeaSceneManager : MonoBehaviour
{
    [Header("Underwater Triggers")]
    [SerializeField] private GameObject monologTrigger;
    [SerializeField] private GameObject[] buluSeribuGroup;
    [SerializeField] private GameObject pariMantaJaring;

    private void Start()
    {
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);

        // Stage 2: Player pertama kali turun -> Aktifkan Trigger Monolog MC
        if (monologTrigger != null)
            monologTrigger.SetActive(stage == 2);

        // Stage 4: Player turun ambil Bulu Seribu -> Aktifkan Hama
        bool isCollectingCots = (stage == 4);
        if (buluSeribuGroup != null)
        {
            foreach (var cots in buluSeribuGroup)
            {
                if (cots != null) cots.SetActive(isCollectingCots);
            }
        }

        // Stage 7: Player turun menyelamatkan Pari Manta -> Aktifkan Manta + Jaring
        if (pariMantaJaring != null)
            pariMantaJaring.SetActive(stage == 7);
    }
}