using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class FishFeedZone : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode feedKey = KeyCode.E;
    [SerializeField] private Transform baitSpawnPoint;
    [SerializeField] private GameObject baitPrefab;

    [Header("Prompt Panel")]
    [SerializeField] private GameObject promptPanel;

    [Header("Fish Targets")]
    // Ganti tipe list dari FishAgent menjadi BiotaMovement
    [SerializeField] private List<BiotaMovement> targetFishList = new List<BiotaMovement>();

    [Header("Events")]
    public UnityEvent onFeedingStarted;
    public UnityEvent onAllFishFed; // Panggil event ini untuk membuka tombol F / memotret

    private bool playerInZone = false;
    private bool isFed = false;
    private bool allFishFedEventInvoked = false;
    private int fedCount = 0;
    private bool isShowingPrompt = false;

    private void EnsurePromptReference()
    {
        if (promptPanel != null) return;
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return;
        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            if (roots[r] == null) continue;
            Transform[] all = roots[r].GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == "PromptPanel")
                {
                    promptPanel = all[i].gameObject;
                    return;
                }
            }
        }
    }

    private void SetPromptVisible(bool visible)
    {
        if (visible)
        {
            EnsurePromptReference();
            if (promptPanel != null && !promptPanel.activeSelf) promptPanel.SetActive(true);
            isShowingPrompt = promptPanel != null;
        }
        else
        {
            if (isShowingPrompt && promptPanel != null && promptPanel.activeSelf) promptPanel.SetActive(false);
            isShowingPrompt = false;
        }
    }

    private void Start()
    {
        EnsurePromptReference();
    }

    private void Update()
    {
        if (playerInZone && !isFed)
        {
            if (InteractionInputLock.IsAnyModalBlockingWorldInteract || InteractionInputLock.IsSignboardOpen)
            {
                SetPromptVisible(false);
                return;
            }
            SetPromptVisible(true);
            if (!InteractionInputLock.WasInteractKeyConsumedThisFrame && Input.GetKeyDown(feedKey))
            {
                InteractionInputLock.ConsumeInteractKeyThisFrame();
                Debug.Log($"[FishFeedZone] Feed key pressed: {feedKey}");
                ExecuteFeeding();
            }
        }
        else if (isShowingPrompt)
        {
            SetPromptVisible(false);
        }
    }

    private void ExecuteFeeding()
    {
        Debug.Log("[FishFeedZone] ExecuteFeeding started.");
        isFed = true;
        fedCount = 0;
        allFishFedEventInvoked = false;

        SetPromptVisible(false);
        onFeedingStarted?.Invoke();

        Vector3 spawnPos = baitSpawnPoint != null ? baitSpawnPoint.position : transform.position;
        if (baitPrefab != null)
        {
            Debug.Log($"[FishFeedZone] Spawning bait at {spawnPos}");
            Instantiate(baitPrefab, spawnPos, Quaternion.identity);
        }

        int fishCount = 0;
        foreach (BiotaMovement fish in targetFishList)
        {
            if (fish != null)
            {
                fishCount++;
                Debug.Log($"[FishFeedZone] Sending fish {fish.name} to bait at {spawnPos}");
                fish.GoToBait(spawnPos, OnSingleFishFinishedEating);
            }
            else
            {
                Debug.LogWarning("[FishFeedZone] Found null fish in targetFishList.");
            }
        }

        Debug.Log($"[FishFeedZone] Feeding started for {fishCount} fish.");

        // Jangan memicu event foto jika belum ada ikan yang menerima umpan.
        if (fishCount == 0)
        {
            Debug.LogWarning("[FishFeedZone] No valid fish received bait. Resetting isFed to false.");
            isFed = false;
        }
    }

    private void OnSingleFishFinishedEating()
    {
        fedCount++;
        Debug.Log($"[FishFeedZone] One fish finished eating. fedCount={fedCount}");

        int fishCount = 0;
        foreach (BiotaMovement fish in targetFishList)
        {
            if (fish != null) fishCount++;
        }

        Debug.Log($"[FishFeedZone] Total fish count={fishCount}, target fedCount={fedCount}");

        if (!allFishFedEventInvoked && fishCount > 0 && fedCount >= fishCount)
        {
            allFishFedEventInvoked = true;
            Debug.Log("[FishFeedZone] All fish fed. Invoking onAllFishFed event.");
            if (QuestController.Instance != null)
            {
                QuestController.Instance.ProgressObjective("makan_ikan", 1);
            }
            onAllFishFed?.Invoke();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            Debug.Log("[FishFeedZone] Player entered feed zone.");
            if (!isFed && !InteractionInputLock.IsAnyModalBlockingWorldInteract && !InteractionInputLock.IsSignboardOpen)
            {
                SetPromptVisible(true);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            Debug.Log("[FishFeedZone] Player exited feed zone.");
            SetPromptVisible(false);
        }
    }

    private void OnDisable()
    {
        playerInZone = false;
        SetPromptVisible(false);
    }
}