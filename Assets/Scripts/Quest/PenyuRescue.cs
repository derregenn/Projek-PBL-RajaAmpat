using UnityEngine;

public class PenyuRescue : MonoBehaviour
{
    public GameObject penyuSprite;
    public Dialogue tourGuideDialogue;

    public void OnQTESuccess()
    {
        Debug.Log("[PenyuRescue] OnQTESuccess dipanggil.");

        if (UniversalFader.Instance != null)
        {
            Debug.Log("[PenyuRescue] UniversalFader ditemukan; memulai fade transition.");
            UniversalFader.Instance.FadeTransition(() =>
            {
                ExecuteQuestSuccess();
            });
        }
        else
        {
            Debug.LogWarning("[PenyuRescue] UniversalFader.Instance tidak ditemukan; langsung memproses quest tanpa fade.");
            ExecuteQuestSuccess();
        }
    }

    private void ExecuteQuestSuccess()
    {
        Debug.Log("[PenyuRescue] Memperbarui status quest dan memunculkan Tour Guide.");

        // 1. Matikan sprite penyu
        if (penyuSprite != null) penyuSprite.SetActive(false);

        // 2. Naikkan stage ke 1
        PlayerPrefs.SetInt("Misool_QuestStage", 1);
        PlayerPrefs.Save();

        // 3. Panggil Manager untuk memunculkan Tour Guide & Petugas
        if (MisoolLandSceneManager.Instance != null)
        {
            MisoolLandSceneManager.Instance.RefreshLandState();
        }
        else
        {
            Debug.LogError("[PenyuRescue] MisoolLandSceneManager.Instance KOSONG! Pastikan ada skrip ini di GameManager.");
        }

        // 4. Paksa mulai dialog
        if (tourGuideDialogue != null)
        {
            // Pastikan objeknya aktif sebelum dialog dipanggil
            tourGuideDialogue.gameObject.SetActive(true);
            tourGuideDialogue.StartDialogue();
        }
        else
        {
            Debug.LogError("[PenyuRescue] Kolom 'Tour Guide Dialogue' di Inspector belum diisi!");
        }
    }
}