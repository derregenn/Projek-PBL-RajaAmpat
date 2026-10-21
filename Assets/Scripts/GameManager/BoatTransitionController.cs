using UnityEngine;
using UnityEngine.SceneManagement;

public class BoatTransitionController : MonoBehaviour
{
    [Header("Referensi Sistem")]
    public Dialogue dialogueSystem;       // Skrip dialog NPC di perahu
    public TriviaQuizUI triviaSystem;     // Skrip UI Trivia (yang baru saja kita perbaiki)

    [Header("Data Pulau (Isi di Inspector)")]
    public IslandTransitionData[] islandData;

    private string currentTarget;
    private IslandTransitionData currentData;

    private void Start()
    {
        // 1. Ambil UI Trivia secara dinamis via Singleton TriviaQuizUI.Instance
        if (triviaSystem == null)
        {
            triviaSystem = TriviaQuizUI.Instance;
        }

        // 2. Ambil Dialogue System jika belum di-assign
        if (dialogueSystem == null)
        {
            dialogueSystem = FindFirstObjectByType<Dialogue>();
        }

        // 3. Ambil target pulau dari Singleton GamePhaseManager
        if (GamePhaseManager.Instance != null && !string.IsNullOrEmpty(GamePhaseManager.Instance.targetIslandName))
        {
            currentTarget = GamePhaseManager.Instance.targetIslandName;
        }
        else
        {
            currentTarget = "Arborek"; // Fallback testing
        }

        // Jalankan logika transisi...
        LoadIslandDataAndStart();
    }

    private void LoadIslandDataAndStart()
    {
        // 2. Cari data dialog & trivia yang cocok untuk pulau ini
        if (islandData != null)
        {
            foreach (var data in islandData)
            {
                if (data != null && string.Equals(data.islandName?.Trim(), currentTarget.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    currentData = data;
                    break;
                }
            }
        }

        // 3. Mulai alur: Mainkan Dialog dulu
        if (currentData != null)
        {
            PlayIntroDialogue();
        }
        else
        {
            Debug.LogError($"BoatTransitionController: data pulau '{currentTarget}' tidak ditemukan. Periksa elemen islandData di Inspector dan samakan islandName.", this);
        }
    }

    private void PlayIntroDialogue()
    {
        if (dialogueSystem == null)
        {
            Debug.LogError("BoatTransitionController: dialogueSystem belum di-assign di Inspector.", this);
            return;
        }

        if (currentData.npcDialogueLines == null || currentData.npcDialogueLines.Length == 0)
        {
            Debug.LogError($"BoatTransitionController: npcDialogueLines untuk '{currentTarget}' kosong.", this);
            return;
        }

        // Masukkan naskah khusus pulau ini ke sistem dialog
        // Setelah dialog selesai, panggil fungsi StartTrivia()
        dialogueSystem.StartDialogue(currentData.npcDialogueLines, StartTrivia);
    }

    private void StartTrivia()
    {
        // Gunakan TriviaQuizUI.Instance langsung agar selalu mengarah ke UI yang aktif
        if (TriviaQuizUI.Instance != null && currentData != null && currentData.islandTriviaQuestions != null)
        {
            TriviaQuizUI.Instance.StartQuiz(currentData.islandTriviaQuestions, FinishTransition);
        }
        else
        {
            Debug.LogWarning("[BoatTransitionController] TriviaQuizUI.Instance tidak ditemukan atau soal kosong!");
            FinishTransition(); // Langsung lanjut jika kuis tidak ada
        }
    }

    private void FinishTransition()
    {
        // Simpan ke PlayerPrefs bahwa pemain SUDAH melewati perahu untuk pulau ini
        PlayerPrefs.SetInt("Visited_" + currentTarget, 1);
        PlayerPrefs.Save();

        // Load scene pulau tujuan
        SceneManager.LoadScene(currentTarget);
    }
}

// Struct untuk mempermudah pengaturan data di Inspector
[System.Serializable]
public class IslandTransitionData
{
    public string islandName; // Misal: "Arborek", "Misool"
    [TextArea(2, 5)]
    public string[] npcDialogueLines;
    public TriviaQuestion[] islandTriviaQuestions;
}