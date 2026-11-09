using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

public class QuestController : MonoBehaviour
{
    private static QuestController _instance;
    public static QuestController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<QuestController>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    // ========================================
    // EVENT FOR PROGRESS & BARRIER UNLOCK
    // ========================================
    // Event ini disiarkan saat quest selesai (mengirimkan questID)
    public static event Action<string> OnQuestCompleted;

    [Header("Quest UI Reference")]
    [Tooltip("Opsional: assign manual jika QuestUI ada di scene yang sama dengan QuestController. " +
             "Jika kosong, akan dicari otomatis setiap kali scene baru dimuat.")]
    [SerializeField] private QuestUI questUI;

    [Header("Active Quests")]
    public List<Quest> ActiveQuests { get; private set; }
        = new List<Quest>();

    // Referensi untuk animasi panel
    [SerializeField] private QuestSlider questSliderAnim;

    // ========================================
    // UNITY LIFECYCLE
    // ========================================

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ReconnectUINextFrame());
    }

    private IEnumerator ReconnectUINextFrame()
    {
        yield return null; // tunggu satu frame

        if (questUI == null)
        {
            questUI = FindFirstObjectByType<QuestUI>();
            if (questUI != null)
                Debug.Log($"[QuestController] QuestUI ditemukan di scene '{SceneManager.GetActiveScene().name}'.");
        }

        if (questSliderAnim == null)
        {
            questSliderAnim = FindFirstObjectByType<QuestSlider>();
        }

        // Jika ada quest aktif saat scene dimuat (misal Continue Game), perbarui HUD
        if (questUI != null && ActiveQuests.Count > 0)
        {
            Quest lastActive = ActiveQuests[ActiveQuests.Count - 1];
            questUI.SetQuest(lastActive);
            questUI.UpdateQuestUI();
            Debug.Log($"[QuestController] HUD di-refresh untuk quest aktif: '{lastActive.QuestTitle}'.");
        }
    }

    // ========================================
    // ACCEPT QUEST
    // ========================================

    public void AcceptQuest(Quest quest)
    {
        if (quest == null)
        {
            Debug.LogWarning("QuestController: Quest is null.");
            return;
        }

        if (string.IsNullOrEmpty(quest.QuestID))
        {
            Debug.LogWarning("QuestController: QuestID kosong.");
            return;
        }

        if (IsQuestActive(quest.QuestID))
        {
            Debug.Log("Quest already active: " + quest.QuestID);
            return;
        }

        // Pastikan quest sebelumnya yang masih menggantung diselesaikan terlebih dahulu
        // agar tidak sekadar tertimpa (ketimpa doang) dan tetap tercatat di Completed Quests!
        if (ActiveQuests.Count > 0)
        {
            List<Quest> previousQuests = new List<Quest>(ActiveQuests);
            foreach (Quest prev in previousQuests)
            {
                if (prev != null && prev.QuestID != quest.QuestID)
                {
                    if (prev.Objectives != null)
                    {
                        foreach (var obj in prev.Objectives)
                        {
                            if (obj != null && !obj.IsComplete)
                            {
                                obj.AddProgress(obj.RequiredAmount);
                            }
                        }
                    }
                    CompleteQuest(prev);
                }
            }
        }

        quest.ResetProgress();
        ActiveQuests.Add(quest);

        Debug.Log("QUEST ACCEPTED: " + quest.QuestTitle);

        if (questUI == null)
            questUI = FindFirstObjectByType<QuestUI>();

        if (questSliderAnim == null)
            questSliderAnim = FindFirstObjectByType<QuestSlider>();

        if (questUI != null)
        {
            questUI.SetQuest(quest);
            questUI.UpdateQuestUI();

            if (questSliderAnim != null)
            {
                questSliderAnim.ShowQuestPopup();
            }
        }
    }

    // ========================================
    // CHECK QUEST ACTIVE & HELPERS
    // ========================================

    public bool IsQuestActive(string questID)
    {
        if (string.IsNullOrEmpty(questID))
            return false;

        return ActiveQuests.Exists(q => q != null && q.QuestID == questID);
    }

    public bool HasActiveQuest()
    {
        return ActiveQuests != null && ActiveQuests.Count > 0;
    }

    public Quest GetCurrentActiveQuest()
    {
        if (ActiveQuests != null && ActiveQuests.Count > 0)
            return ActiveQuests[ActiveQuests.Count - 1];
        return null;
    }

    // ========================================
    // PROGRESS OBJECTIVE
    // ========================================

    public void ProgressObjective(string objectiveID, int amount = 1)
    {
        if (string.IsNullOrEmpty(objectiveID))
        {
            Debug.LogWarning("QuestController: Objective ID kosong.");
            return;
        }

        if (amount <= 0)
        {
            Debug.LogWarning("QuestController: Amount harus lebih dari 0.");
            return;
        }

        // Cari objective di quest aktif (dukung alias ID bila nama objective di scene berbeda dengan asset)
        string[] candidateIDs = GetCandidateObjectiveIDs(objectiveID);

        foreach (Quest quest in ActiveQuests)
        {
            if (quest == null)
                continue;

            QuestObjective objective = null;
            foreach (string cid in candidateIDs)
            {
                objective = quest.GetObjective(cid);
                if (objective != null && !objective.IsComplete)
                    break;
            }
            if (objective == null)
            {
                foreach (string cid in candidateIDs)
                {
                    objective = quest.GetObjective(cid);
                    if (objective != null)
                        break;
                }
            }

            if (objective == null)
                continue;

            // Jangan tambah progress jika sudah selesai
            if (objective.IsComplete)
            {
                Debug.Log("Objective already complete: " + objectiveID);
                return;
            }

            // Tambahkan progress
            objective.AddProgress(amount);

            Debug.Log(
                $"Objective Progress: {objective.ObjectiveID} {objective.CurrentAmount}/{objective.RequiredAmount}"
            );

            // Update UI
            if (questUI != null)
                questUI.UpdateQuestUI();

            // Cek quest selesai
            if (IsQuestFinished(quest))
                CompleteQuest(quest);

            return;
        }

        // Fallback: Jika quest belum aktif di antrean (misal player langsung ambil item sebelum bicara ke NPC atau test isolated scene)
        Quest foundQuest = null;
        QuestGiver[] givers1 = FindObjectsByType<QuestGiver>(FindObjectsSortMode.None);
        foreach (var g in givers1)
        {
            if (g != null && g.QuestToGive != null && g.QuestToGive.GetObjective(objectiveID) != null)
            {
                foundQuest = g.QuestToGive;
                break;
            }
        }

        if (foundQuest == null)
        {
            QuestGiver2[] givers2 = FindObjectsByType<QuestGiver2>(FindObjectsSortMode.None);
            foreach (var g in givers2)
            {
                if (g != null && g.QuestToGive != null && g.QuestToGive.GetObjective(objectiveID) != null)
                {
                    foundQuest = g.QuestToGive;
                    break;
                }
            }
        }

        if (foundQuest != null)
        {
            Debug.Log($"[QuestController] Auto-accepting quest '{foundQuest.QuestTitle}' untuk objective '{objectiveID}' (Isolated scene testing fallback).");
            AcceptQuest(foundQuest);
            ProgressObjective(objectiveID, amount);
            return;
        }

        Debug.LogWarning("Objective not found in active quests: " + objectiveID);
    }

    // ========================================
    // CHECK QUEST FINISHED
    // ========================================

    private bool IsQuestFinished(Quest quest)
    {
        if (quest == null)
            return false;

        if (quest.Objectives == null || quest.Objectives.Count == 0)
        {
            Debug.LogWarning("Quest tidak memiliki objective: " + quest.QuestTitle);
            return false;
        }

        foreach (QuestObjective objective in quest.Objectives)
        {
            if (objective == null)
                continue;

            if (!objective.IsComplete)
                return false;
        }

        return true;
    }

    private string[] GetCandidateObjectiveIDs(string objectiveID)
    {
        switch (objectiveID)
        {
            case "ambil_sagu":
            case "Berikan sagu":
            case "minta_air":
                return new string[] { objectiveID, "ambil_air" };
            case "Antarkan Air":
                return new string[] { objectiveID, "Bicara", "ambil_air" };
            case "trivia_piaynemo":
            case "tangga_piaynemo":
                return new string[] { objectiveID, "temui_tourguide" };
            case "serahkan_foto_pembisnis":
                return new string[] { objectiveID, "foto_sawinggrai" };
            case "foto_arborek":
                return new string[] { objectiveID, "Kembali" };
            default:
                return new string[] { objectiveID };
        }
    }

    public void CompleteAllActiveQuests()
    {
        if (ActiveQuests == null || ActiveQuests.Count == 0) return;
        List<Quest> copy = new List<Quest>(ActiveQuests);
        foreach (Quest q in copy)
        {
            if (q == null) continue;
            if (q.Objectives != null)
            {
                foreach (var obj in q.Objectives)
                {
                    if (obj != null && !obj.IsComplete)
                        obj.AddProgress(obj.RequiredAmount);
                }
            }
            CompleteQuest(q);
        }
    }

    // ========================================
    // COMPLETE QUEST
    // ========================================

    private void CompleteQuest(Quest quest)
    {
        if (quest == null)
            return;

        if (!ActiveQuests.Contains(quest))
            return;

        string completedQuestID = quest.QuestID;
        string completedQuestTitle = quest.QuestTitle;

        Debug.Log("================================");
        Debug.Log("QUEST COMPLETE: " + completedQuestTitle);
        Debug.Log("================================");

        // Catat quest selesai ke QuestJournalManager SEBELUM ClearQuest mengosongkan teks!
        if (QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.RecordCompletedQuest(completedQuestID, completedQuestTitle);
        }

        ActiveQuests.Remove(quest);

        if (ActiveQuests.Count > 0)
        {
            Quest remainingQuest = ActiveQuests[ActiveQuests.Count - 1];
            if (questUI != null)
            {
                questUI.SetQuest(remainingQuest);
                questUI.UpdateQuestUI();
                Debug.Log($"[QuestController] UI dipulihkan ke quest sebelumnya yang belum tuntas: '{remainingQuest.QuestTitle}'");
            }
        }
        else
        {
            if (questUI != null)
                questUI.ClearQuest();
        }

        // ====================================
        // SIARKAN EVENT KE BARRIER & SYSTEM PROGRESS
        // ====================================
        OnQuestCompleted?.Invoke(completedQuestID);

        // Selaraskan ke GamePhaseManager
        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.HandleQuestCompleted(completedQuestID);
        }
    }
}
