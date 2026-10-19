using UnityEngine;
using UnityEngine.SceneManagement;

public class BoatTransitionController : MonoBehaviour
{
    [Header("Referensi Sistem")]
    public Dialogue dialogueSystem;

    [Header("Data Perjalanan Tiap Pulau")]
    public IslandTransitionData[] islandData;

    private string currentTarget;
    private IslandTransitionData currentData;

    private void Start()
    {
        // 1. Ambil nama pulau tujuan dari Singleton GamePhaseManager
        if (GamePhaseManager.Instance != null && !string.IsNullOrEmpty(GamePhaseManager.Instance.targetIslandName))
        {
            currentTarget = GamePhaseManager.Instance.targetIslandName;
        }
        else
        {
            currentTarget = "";
        }

        // 2. Cari data dialog & trivia yang cocok untuk pulau ini
        foreach (var data in islandData)
        {
            if (data.islandName == currentTarget)
            {
                currentData = data;
                break;
            }
        }

        // 3. Mulai alur otomatis
        if (currentData != null)
        {
            PlayIntroDialogue();
        }
        else
        {
            Debug.LogError("Data pulau " + currentTarget + " tidak ditemukan di Inspector BoatTransitionController!");
        }
    }

    private void PlayIntroDialogue()
    {
        if (dialogueSystem != null && currentData.npcDialogueLines.Length > 0)
        {
            // Mainkan dialog, saat selesai otomatis jalankan StartTrivia()
            dialogueSystem.StartDialogue(currentData.npcDialogueLines, StartTrivia);
        }
        else
        {
            // Jika tidak ada dialog, langsung kuis
            StartTrivia();
        }
    }

    private void StartTrivia()
    {
        // Panggil TriviaQuizUI secara langsung, layaknya skrip NPCTourGuide
        if (TriviaQuizUI.Instance != null && currentData.islandTriviaQuestions.Length > 0)
        {
            TriviaQuizUI.Instance.StartQuiz(currentData.islandTriviaQuestions, FinishTransition);
        }
        else
        {
            FinishTransition();
        }
    }

    private void FinishTransition()
    {
        Debug.Log("Kuis perahu selesai, menuju pulau: " + currentTarget);

        // Catat bahwa pemain sudah melewati perahu menuju pulau ini
        PlayerPrefs.SetInt("Visited_" + currentTarget, 1);
        PlayerPrefs.Save();

        // Pindah ke scene tujuan sesungguhnya
        SceneManager.LoadScene(currentTarget);
    }
}

// Class ini memastikan array soal TriviaQuestion bisa muncul di Inspector BoatTransitionController
[System.Serializable]
public class IslandTransitionData
{
    public string islandName;

    [TextArea(2, 5)]
    public string[] npcDialogueLines;

    // Tarik referensi class TriviaQuestion yang sama dengan yang dipakai NPCTourGuide
    public TriviaQuestion[] islandTriviaQuestions;
}