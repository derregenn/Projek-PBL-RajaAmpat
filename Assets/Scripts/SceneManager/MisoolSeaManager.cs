using UnityEngine;

public class MisoolSeaManager : MonoBehaviour
{
    [Header("Objek Poin 3 (Monolog MC)")]
    [SerializeField] private GameObject monologueTrigger;

    [Header("Objek Poin 5 (Hama Bulu Seribu)")]
    [SerializeField] private GameObject[] buluSeribuGroup;

    private void Start()
    {
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);

        // Poin 3: Trigger Monolog MC HANYA aktif saat Stage 2 (Menyelam Pertama)
        if (monologueTrigger != null)
        {
            monologueTrigger.SetActive(stage == 2);
        }

        // Poin 5: Objek 3 Bulu Seribu HANYA aktif saat Stage 4 (Petugas Minta Bantuan)
        bool isCollectingStage = (stage == 4);
        if (buluSeribuGroup != null)
        {
            foreach (var item in buluSeribuGroup)
            {
                if (item != null) item.SetActive(isCollectingStage);
            }
        }
    }
}