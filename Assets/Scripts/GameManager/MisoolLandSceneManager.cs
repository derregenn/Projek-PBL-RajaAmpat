using UnityEngine;

public class MisoolLandSceneManager : MonoBehaviour
{
    [Header("NPC References")]
    [SerializeField] private GameObject penyuInteractive;
    [SerializeField] private GameObject petugasKonservasi;
    [SerializeField] private GameObject ibuIbuGroup;
    [SerializeField] private GameObject tourGuide;
    [SerializeField] private Transform tourGuideEndPosition; // Titik di sebelah kanan player

    private void Start()
    {
        int stage = PlayerPrefs.GetInt("Misool_QuestStage", 0);

        // Atur visibilitas NPC berdasarkan progress
        if (penyuInteractive != null) 
            penyuInteractive.SetActive(stage == 0);

        if (ibuIbuGroup != null) 
            ibuIbuGroup.SetActive(stage >= 6 && stage <= 8); // Hilang pada stage 9

        // Jika quest sudah selesai total (Stage 9)
        if (stage >= 9)
        {
            if (tourGuide != null && tourGuideEndPosition != null)
            {
                tourGuide.transform.position = tourGuideEndPosition.position;
                tourGuide.SetActive(true);
            }
        }
    }

    // Dipanggil saat selesai dialog dengan Ibu-Ibu di akhir Quest 3
    public void FinishQuest3Report()
    {
        // 1. Jalankan Screen Fade
        if (UniversalFader.Instance != null)
        {
            UniversalFader.Instance.FadeTransition(() =>
            {
                // 2. Saat layar gelap: Hilangkan Ibu-Ibu & Pindahkan Tour Guide
                if (ibuIbuGroup != null) ibuIbuGroup.SetActive(false);

                if (tourGuide != null && tourGuideEndPosition != null)
                {
                    tourGuide.transform.position = tourGuideEndPosition.position;
                    tourGuide.SetActive(true);
                }

                // 3. Simpan status quest selesai
                PlayerPrefs.SetInt("Misool_QuestStage", 9);
                PlayerPrefs.Save();

                // 4. Panggil Dialog Akhir Tour Guide
                Dialogue tourGuideDialogue = tourGuide.GetComponent<Dialogue>();
                if (tourGuideDialogue != null)
                {
                    tourGuideDialogue.StartDialogue();
                }
            });
        }
    }
}