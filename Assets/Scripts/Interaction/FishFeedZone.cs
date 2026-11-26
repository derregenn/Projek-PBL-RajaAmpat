using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class FishFeedZone : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode feedKey = KeyCode.E;
    [SerializeField] private List<Sprite> baitSprites = new List<Sprite>(); // Daftar sprite umpan yang akan muncul saat memberi makan ikan
    [SerializeField] private string baitSortingLayer = "Default";
    [SerializeField] private int baitSortingOrder = 100;
    [SerializeField] private Vector3 baitScale = Vector3.one;

    [Header("Prompt Panel")]
    [SerializeField] private GameObject promptPanel;

    [Header("Daftar Titik Umpan (Otomatis terisi dari Child jika kosong)")]
    [SerializeField] private List<FishFeedSpot> feedSpots = new List<FishFeedSpot>();

    [Header("Events")]
    public UnityEvent onFeedingStarted;
    public UnityEvent onAllFishFed; // Dipanggil setelah semua ikan selesai diberi umpan & berhenti

    private readonly List<FishFeedSpot> spotsInRange = new List<FishFeedSpot>();
    private bool feedingStartedInvoked = false;
    private bool allFishFedEventInvoked = false;
    private int fedCount = 0;
    private bool isShowingPrompt = false;
    private int nextBaitSpriteIndex = 0;

    private void Awake()
    {
        // Ambil otomatis semua FishFeedSpot di dalam child jika list di Inspector belum diisi
        if (feedSpots.Count == 0)
        {
            GetComponentsInChildren(true, feedSpots);
        }

        foreach (FishFeedSpot spot in feedSpots)
        {
            if (spot != null)
            {
                spot.Initialize(this);
            }
        }
    }

    private void Start()
    {
        EnsurePromptReference();
    }

    private void Update()
    {
        FishFeedSpot targetSpot = GetClosestAvailableSpot();

        if (targetSpot != null)
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
                ExecuteSingleFeeding(targetSpot);
            }
        }
        else if (isShowingPrompt)
        {
            SetPromptVisible(false);
        }
    }

    private FishFeedSpot GetClosestAvailableSpot()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPos = playerObj != null ? playerObj.transform.position : transform.position;

        FishFeedSpot closest = null;
        float minDist = float.MaxValue;

        for (int i = spotsInRange.Count - 1; i >= 0; i--)
        {
            FishFeedSpot spot = spotsInRange[i];
            if (spot == null || spot.IsFed || !spot.gameObject.activeInHierarchy)
            {
                spotsInRange.RemoveAt(i);
                continue;
            }

            float dist = Vector2.Distance(playerPos, spot.GetBaitPosition());
            if (dist < minDist)
            {
                minDist = dist;
                closest = spot;
            }
        }

        return closest;
    }

    private void ExecuteSingleFeeding(FishFeedSpot spot)
    {
        spot.MarkAsFed();
        spotsInRange.Remove(spot);

        if (!feedingStartedInvoked)
        {
            feedingStartedInvoked = true;
            onFeedingStarted?.Invoke();
        }

        Vector3 spawnPos = spot.GetBaitPosition();
        SpawnBaitSprite(spawnPos);

        if (spot.targetFish != null)
        {
            Debug.Log($"[FishFeedZone] Memberi makan {spot.targetFish.name} di titik {spot.name}");
            spot.targetFish.GoToBait(spawnPos, OnSingleFishFinishedEating);
        }
        else
        {
            Debug.LogWarning($"[FishFeedZone] targetFish pada {spot.name} kosong, menghitung titik selesai.");
            OnSingleFishFinishedEating();
        }

        // Sembunyikan prompt jika tidak ada titik lain di dekat pemain
        if (GetClosestAvailableSpot() == null)
        {
            SetPromptVisible(false);
        }
    }

    private void OnSingleFishFinishedEating()
    {
        fedCount++;

        int totalValidSpots = 0;
        foreach (FishFeedSpot spot in feedSpots)
        {
            if (spot != null) totalValidSpots++;
        }

        Debug.Log($"[FishFeedZone] Ikan selesai makan ({fedCount}/{totalValidSpots})");

        if (!allFishFedEventInvoked && totalValidSpots > 0 && fedCount >= totalValidSpots)
        {
            allFishFedEventInvoked = true;
            if (QuestController.Instance != null)
            {
                QuestController.Instance.ProgressObjective("makan_ikan", 1);
            }
            onAllFishFed?.Invoke();
        }
    }

    private void SpawnBaitSprite(Vector3 spawnPos)
    {
        if (baitSprites == null || baitSprites.Count == 0)
        {
            Debug.LogWarning("[FishFeedZone] baitSprites kosong. Tambahkan sprite umpan pada Inspector.", this);
            return;
        }

        // Gunakan sprite berikutnya agar variasi umpan bergantian.
        for (int i = 0; i < baitSprites.Count; i++)
        {
            int spriteIndex = (nextBaitSpriteIndex + i) % baitSprites.Count;
            Sprite sprite = baitSprites[spriteIndex];
            if (sprite == null) continue;

            GameObject baitObject = new GameObject($"Bait_{sprite.name}");
            baitObject.transform.position = spawnPos;
            baitObject.transform.localScale = baitScale;
            SpriteRenderer spriteRenderer = baitObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = true;
            spriteRenderer.sortingLayerName = baitSortingLayer;
            spriteRenderer.sortingOrder = baitSortingOrder;
            nextBaitSpriteIndex = (spriteIndex + 1) % baitSprites.Count;
            return;
        }

        Debug.LogWarning("[FishFeedZone] Semua elemen baitSprites kosong. Isi sprite yang valid pada Inspector.", this);
    }

    public void NotifyPlayerEnteredSpot(FishFeedSpot spot)
    {
        if (spot != null && !spot.IsFed && !spotsInRange.Contains(spot))
        {
            spotsInRange.Add(spot);
        }
    }

    public void NotifyPlayerExitedSpot(FishFeedSpot spot)
    {
        if (spot != null)
        {
            spotsInRange.Remove(spot);
        }
    }

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

    private void OnDisable()
    {
        spotsInRange.Clear();
        SetPromptVisible(false);
    }
}