using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BoatTransitionController : MonoBehaviour
{
    [Header("Referensi Sistem")]
    public Dialogue dialogueSystem;
    public TriviaQuizUI triviaSystem;

    [Header("Lokasi Spawn Background")]
    [Tooltip("Pilih objek Environment sebagai tempat munculnya background parallax")]
    public Transform environmentParent; // Drag objek 'Environment' dari Hierarchy ke sini

    [Header("Data Pulau (Isi di Inspector)")]
    public IslandTransitionData[] islandData;

    private string currentTarget;
    private IslandTransitionData currentData;

    private void Start()
    {
        if (triviaSystem == null) triviaSystem = TriviaQuizUI.Instance;
        if (dialogueSystem == null) dialogueSystem = FindFirstObjectByType<Dialogue>();

        // 1. Ambil target pulau dari GamePhaseManager
        if (GamePhaseManager.Instance != null && !string.IsNullOrEmpty(GamePhaseManager.Instance.targetIslandName))
        {
            currentTarget = GamePhaseManager.Instance.targetIslandName;
            Debug.Log($"[BoatTransition] Target pulau terdeteksi dari GamePhaseManager: '{currentTarget}'");
        }
        // 2. Ambil target dari MapController
        else if (!string.IsNullOrEmpty(MapController.targetIslandScene))
        {
            currentTarget = MapController.targetIslandScene;
            Debug.Log($"[BoatTransition] Target pulau terdeteksi dari MapController: '{currentTarget}'");
        }
        // 3. Fallback: Cek dari PlayerPrefs
        else if (PlayerPrefs.HasKey("TargetIsland") && !string.IsNullOrEmpty(PlayerPrefs.GetString("TargetIsland")))
        {
            currentTarget = PlayerPrefs.GetString("TargetIsland");
            Debug.Log($"[BoatTransition] Target dari PlayerPrefs: '{currentTarget}'");
        }
        else
        {
            // Jika testing langsung di scene kapal, gunakan default 03_Pianemo
            currentTarget = "03_Pianemo";
            Debug.LogWarning($"[BoatTransition] Target tidak ditemukan. Menggunakan fallback: '{currentTarget}'");
        }

        LoadIslandDataAndStart();
    }

    private void LoadIslandDataAndStart()
    {
        string targetKey = NormalizeIslandKey(currentTarget);

        if (islandData != null)
        {
            foreach (var data in islandData)
            {
                if (data == null || string.IsNullOrEmpty(data.islandName)) continue;

                string dataKey = NormalizeIslandKey(data.islandName);

                // Pencocokan fleksibel (misal "06_Sawinggrai" cocok dengan "Sawinggrai")
                if (targetKey == dataKey || targetKey.Contains(dataKey) || dataKey.Contains(targetKey))
                {
                    currentData = data;
                    Debug.Log($"[BoatTransition] Menggunakan data dialog untuk: '{data.islandName}' (Key: {dataKey})");
                    break;
                }
            }
        }

        if (currentData == null)
        {
            Debug.LogWarning($"[BoatTransition] Data pulau '{currentTarget}' tidak ditemukan di Inspector islandData. Membuat fallback data.");
            currentData = new IslandTransitionData
            {
                islandName = currentTarget,
                npcDialogueLines = new string[] { $"Kapal kita sedang berlayar menuju {currentTarget}. Nikmati pemandangannya!" }
            };
        }

        SpawnParallaxBackground();
        PlayIntroDialogue();
    }

    private string NormalizeIslandKey(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        string clean = name.ToLower().Trim();
        // Hapus prefix angka seperti 01_, 02_, dst
        clean = Regex.Replace(clean, @"^\d+_", "");
        // Normalisasi ejaan Pianemo / Piaynemo
        clean = clean.Replace("piaynemo", "pianemo");
        clean = clean.Replace(" ", "").Replace("_", "");
        return clean;
    }

    private void SpawnParallaxBackground()
    {
        if (currentData != null && currentData.islandBackgroundPrefab != null && environmentParent != null)
        {
            GameObject spawnedBG = Instantiate(currentData.islandBackgroundPrefab, environmentParent);
            spawnedBG.transform.localPosition = Vector3.zero;
            spawnedBG.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[BoatTransition] Prefab Background atau Environment Parent belum di-assign di Inspector.");
        }
    }

    private void PlayIntroDialogue()
    {
        if (dialogueSystem == null) dialogueSystem = FindFirstObjectByType<Dialogue>();

        if (dialogueSystem == null)
        {
            Debug.LogWarning("[BoatTransition] dialogueSystem tidak ditemukan di scene! Langsung lanjut ke trivia/transisi.", this);
            StartTrivia();
            return;
        }

        if (currentData == null || currentData.npcDialogueLines == null || currentData.npcDialogueLines.Length == 0)
        {
            Debug.LogWarning($"[BoatTransition] npcDialogueLines untuk '{currentTarget}' kosong, langsung lanjut.");
            StartTrivia();
            return;
        }

        dialogueSystem.StartDialogue(currentData.npcDialogueLines, StartTrivia);
    }

    private void StartTrivia()
    {
        if (TriviaQuizUI.Instance == null) triviaSystem = FindFirstObjectByType<TriviaQuizUI>();
        else triviaSystem = TriviaQuizUI.Instance;

        if (triviaSystem != null && currentData != null && currentData.islandTriviaQuestions != null && currentData.islandTriviaQuestions.Length > 0)
        {
            triviaSystem.StartQuiz(currentData.islandTriviaQuestions, FinishTransition);
        }
        else
        {
            Debug.LogWarning("[BoatTransition] Soal trivia kosong / TriviaQuizUI null, langsung pindah scene.");
            FinishTransition();
        }
    }

    private void FinishTransition()
    {
        PlayerPrefs.SetInt("Visited_" + currentTarget, 1);
        PlayerPrefs.SetString("SavedScene", currentTarget);
        PlayerPrefs.Save();

        // Update status cerita
        UpdateStoryPhaseForTarget(currentTarget);

        int buildIndex = FindTargetSceneBuildIndex();
        if (buildIndex >= 0)
        {
            Debug.Log($"[BoatTransition] Memuat target scene via BuildIndex {buildIndex}: '{currentTarget}'");
            SceneManager.LoadScene(buildIndex);
        }
        else
        {
            Debug.LogWarning($"[BoatTransition] Scene '{currentTarget}' tidak ditemukan di Build Settings via name exact. Mencoba LoadScene langsung.");
            try
            {
                SceneManager.LoadScene(currentTarget);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BoatTransition] Gagal memuat '{currentTarget}': {ex.Message}. Fallback ke 02_Waisai.");
                SceneManager.LoadScene("02_Waisai");
            }
        }
    }

    private void UpdateStoryPhaseForTarget(string sceneName)
    {
        string norm = NormalizeIslandKey(sceneName);

        if (StoryManager.Instance != null)
        {
            if (norm.Contains("waisai") || norm.Contains("airstrip")) StoryManager.Instance.currentPhase = StoryManager.StoryPhase.Arrival_Airstrip;
            else if (norm.Contains("pianemo")) StoryManager.Instance.currentPhase = StoryManager.StoryPhase.Pianemo;
            else if (norm.Contains("misool")) StoryManager.Instance.currentPhase = StoryManager.StoryPhase.Misool;
            else if (norm.Contains("arborek")) StoryManager.Instance.currentPhase = StoryManager.StoryPhase.Arborek;
            else if (norm.Contains("sawinggrai")) StoryManager.Instance.currentPhase = StoryManager.StoryPhase.Sawinggrai;
            else if (norm.Contains("friwen")) StoryManager.Instance.currentPhase = StoryManager.StoryPhase.Friwen;
            else if (norm.Contains("gag")) StoryManager.Instance.currentPhase = StoryManager.StoryPhase.Gag_Climax;
            
            StoryManager.Instance.SaveProgress();
        }
    }

    private int FindTargetSceneBuildIndex()
    {
        string targetName = currentTarget.Trim();
        int separatorIndex = Mathf.Max(targetName.LastIndexOf('/'), targetName.LastIndexOf('\\'));
        if (separatorIndex >= 0)
            targetName = targetName.Substring(separatorIndex + 1);
        if (targetName.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            targetName = targetName.Substring(0, targetName.Length - 6);

        string targetNorm = NormalizeIslandKey(targetName);

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            int pathSeparatorIndex = Mathf.Max(scenePath.LastIndexOf('/'), scenePath.LastIndexOf('\\'));
            string sceneName = scenePath.Substring(pathSeparatorIndex + 1);
            if (sceneName.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                sceneName = sceneName.Substring(0, sceneName.Length - 6);

            if (string.Equals(sceneName, targetName, StringComparison.OrdinalIgnoreCase))
                return i;

            if (NormalizeIslandKey(sceneName) == targetNorm)
                return i;
        }

        return -1;
    }
}

[System.Serializable]
public class IslandTransitionData
{
    public string islandName;
    
    [Header("Visual Environment (Parallax)")]
    [Tooltip("Masukkan Prefab Parallax Background (contoh: BG_Arborek) dari panel Project")]
    public GameObject islandBackgroundPrefab; // Slot prefab background

    [TextArea(2, 5)]
    public string[] npcDialogueLines;
    public TriviaQuestion[] islandTriviaQuestions;
}
