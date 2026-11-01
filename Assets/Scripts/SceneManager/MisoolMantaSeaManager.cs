using UnityEngine;

public class MisoolMantaSeaManager : MonoBehaviour
{
    [Header("Objek Poin 8 (Pari Manta)")]
    [SerializeField] private GameObject pariMantaJaring; // Objek Manta tersangkut + QTE
    [SerializeField] private GameObject pariMantaBebas;  // Visual Manta berenang bebas (opsional)

    private void Start()
    {
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);

        // Poin 8: Manta tersangkut Jaring hanya aktif pada Stage 7 (Disuruh Ibu-ibu)
        if (pariMantaJaring != null)
        {
            pariMantaJaring.SetActive(stage == 7);
        }

        // Jika quest selesai / Free Roam (Stage >= 8), tampilkan Pari Manta berenang bebas
        if (pariMantaBebas != null)
        {
            pariMantaBebas.SetActive(stage >= 8);
        }
    }
}