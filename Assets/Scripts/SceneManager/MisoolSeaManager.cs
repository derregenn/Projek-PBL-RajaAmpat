using UnityEngine;

public class MisoolSeaManager : MonoBehaviour
{
    [Header("Objek Poin 3 (Monolog MC)")]
    [SerializeField] private GameObject monologueTrigger;

    [Header("Objek Poin 5 (Hama Bulu Seribu)")]
    [SerializeField] private GameObject[] buluSeribuGroup;

    [Header("Objek Poin 7 (Pari Manta)")]
    [SerializeField] private GameObject pariManta;

    private int lastStage = int.MinValue;

    private void Start()
    {
        RefreshSeaState();
    }

    private void Update()
    {
        if (MasterQuestManager.Instance != null &&
            MasterQuestManager.Instance.CurrentStage != lastStage)
        {
            RefreshSeaState();
        }
    }

    public void RefreshSeaState()
    {
        if (MasterQuestManager.Instance == null)
        {
            Debug.LogWarning("[MisoolSeaManager] MasterQuestManager tidak ditemukan di scene ini!");
            return;
        }

        int stage = MasterQuestManager.Instance.CurrentStage;
        lastStage = stage;

        // Poin 3: Trigger Monolog MC HANYA aktif saat Stage 2 (Menyelam Pertama)
        if (monologueTrigger != null)
        {
            monologueTrigger.SetActive(stage == 2);
        }

        // Poin 5: Objek 3 Bulu Seribu HANYA aktif saat Stage 5 (Petugas Minta Bantuan)
        bool isCollectingStage = (stage >= 2 && stage < 6);
        if (buluSeribuGroup != null)
        {
            foreach (var item in buluSeribuGroup)
            {
                if (item != null) item.SetActive(isCollectingStage);
            }
        }

        // Manta yang sama aktif sejak Stage 7 hingga quest selesai.
        if (pariManta != null)
        {
            pariManta.SetActive(stage >= 7);
        }
    }
}