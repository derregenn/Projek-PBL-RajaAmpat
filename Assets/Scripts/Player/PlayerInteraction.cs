using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("UI Prompt (Opsional)")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    private readonly List<IInteractable> nearbyInteractables = new List<IInteractable>();
    private IInteractable currentInteractable;
    private string lastPromptMessage;
    private bool isShowingPrompt = false;
    private readonly HashSet<Collider2D> activeTriggerColliders = new HashSet<Collider2D>();
    private readonly List<Collider2D> staleCollidersBuffer = new List<Collider2D>(8);
    private readonly List<IInteractable> interactableLookupBuffer = new List<IInteractable>(4);
    private Collider2D playerCollider;
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(8);
    private ContactFilter2D overlapFilter;

    private void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
        overlapFilter = ContactFilter2D.noFilter;
        overlapFilter.useTriggers = true;
        EnsurePromptPanelReferences();
    }

    private void Start()
    {
        EnsurePromptPanelReferences();
    }

    private void EnsurePromptPanelReferences()
    {
        if (promptPanel == null)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                GameObject[] roots = activeScene.GetRootGameObjects();
                for (int r = 0; r < roots.Length && promptPanel == null; r++)
                {
                    if (roots[r] == null) continue;
                    Transform[] all = roots[r].GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (all[i] != null && all[i].name == "PromptPanel")
                        {
                            promptPanel = all[i].gameObject;
                            break;
                        }
                    }
                }
            }
        }

        if (promptText == null && promptPanel != null)
        {
            promptText = promptPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void Update()
    {
        // Priority 1: SignBoard yang sedang terbuka menerima tombol E untuk menutupnya
        SignboardInteractable openSignboard = SignboardInteractable.ActiveOpenSignboard;
        if (openSignboard != null)
        {
            HidePrompt(true);
            if (!InteractionInputLock.IsAnyModalBlockingWorldInteract
                && !InteractionInputLock.WasInteractKeyConsumedThisFrame
                && Input.GetKeyDown(interactKey))
            {
                openSignboard.Interact();
            }
            return;
        }

        // Priority 2: Blokir interaksi dunia & sembunyikan prompt saat modal lain aktif (Dialog NPC, Trivia, Infografis, Map)
        if (InteractionInputLock.IsAnyModalBlockingWorldInteract)
        {
            HidePrompt(true);
            return;
        }

        // Priority 3: Cari interactable valid terdekat
        RefreshOverlappingInteractables();
        CleanupNearbyInteractables();
        currentInteractable = ResolveBestInteractable(out string promptMessage);

        if (currentInteractable != null)
        {
            ShowPrompt(promptMessage);
            if (!InteractionInputLock.WasInteractKeyConsumedThisFrame && Input.GetKeyDown(interactKey))
            {
                IInteractable target = currentInteractable;
                HidePrompt();
                target.Interact();
                InteractionInputLock.ConsumeInteractKeyThisFrame();
            }
        }
        else
        {
            HidePrompt();
        }
    }

    private static bool BoundsIntersectWithTolerance(Bounds a, Bounds b, float tolerance)
    {
        return a.min.x - tolerance <= b.max.x
            && a.max.x + tolerance >= b.min.x
            && a.min.y - tolerance <= b.max.y
            && a.max.y + tolerance >= b.min.y;
    }

    private void CollectInteractablesFromCollider(Collider2D col)
    {
        if (col == null || col == playerCollider) return;

        interactableLookupBuffer.Clear();
        col.GetComponents(interactableLookupBuffer);
        if (interactableLookupBuffer.Count == 0)
        {
            col.GetComponentsInParent(false, interactableLookupBuffer);
        }

        for (int i = 0; i < interactableLookupBuffer.Count; i++)
        {
            IInteractable interactable = interactableLookupBuffer[i];
            if (interactable != null && !nearbyInteractables.Contains(interactable))
            {
                nearbyInteractables.Add(interactable);
            }
        }
        interactableLookupBuffer.Clear();
    }

    private void RefreshOverlappingInteractables()
    {
        staleCollidersBuffer.Clear();
        bool hasValidPlayerCollider = playerCollider != null && playerCollider.enabled;
        Bounds playerBounds = hasValidPlayerCollider ? playerCollider.bounds : default;

        foreach (Collider2D col in activeTriggerColliders)
        {
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
            {
                staleCollidersBuffer.Add(col);
            }
            else if (hasValidPlayerCollider && !BoundsIntersectWithTolerance(col.bounds, playerBounds, 0.15f))
            {
                staleCollidersBuffer.Add(col);
            }
        }

        for (int i = 0; i < staleCollidersBuffer.Count; i++)
        {
            activeTriggerColliders.Remove(staleCollidersBuffer[i]);
        }
        staleCollidersBuffer.Clear();

        nearbyInteractables.Clear();
        foreach (Collider2D col in activeTriggerColliders)
        {
            CollectInteractablesFromCollider(col);
        }

        if (hasValidPlayerCollider)
        {
            overlapResults.Clear();
            playerCollider.Overlap(overlapFilter, overlapResults);

            for (int i = 0; i < overlapResults.Count; i++)
            {
                CollectInteractablesFromCollider(overlapResults[i]);
            }
            overlapResults.Clear();
        }
    }

    private void CleanupNearbyInteractables()
    {
        for (int i = nearbyInteractables.Count - 1; i >= 0; i--)
        {
            IInteractable item = nearbyInteractables[i];
            if (item == null || (item is UnityEngine.Object uObj && uObj == null))
            {
                nearbyInteractables.RemoveAt(i);
                continue;
            }
            if (item is Behaviour beh)
            {
                if (!beh.gameObject.activeInHierarchy)
                {
                    nearbyInteractables.RemoveAt(i);
                }
                else if (!beh.enabled)
                {
                    if (beh is SignboardInteractable)
                    {
                        beh.enabled = true;
                    }
                    else
                    {
                        nearbyInteractables.RemoveAt(i);
                    }
                }
            }
        }
    }

    private IInteractable ResolveBestInteractable(out string promptMessage)
    {
        IInteractable best = null;
        string bestPrompt = null;
        float bestSqrDist = float.MaxValue;
        Vector2 playerPos = transform.position;

        for (int i = 0; i < nearbyInteractables.Count; i++)
        {
            IInteractable item = nearbyInteractables[i];
            string msg = item.GetPromptText();
            if (string.IsNullOrEmpty(msg)) continue;

            float sqrDist = float.MaxValue * 0.5f;
            if (item is Component comp && comp != null)
            {
                Vector2 itemPos = comp.transform.position;
                sqrDist = (itemPos - playerPos).sqrMagnitude;
            }

            if (best == null || sqrDist < bestSqrDist)
            {
                best = item;
                bestPrompt = msg;
                bestSqrDist = sqrDist;
            }
        }

        promptMessage = bestPrompt;
        return best;
    }

    private void RegisterTriggerInteractable(Collider2D collision)
    {
        CollectInteractablesFromCollider(collision);
        CleanupNearbyInteractables();

        if (InteractionInputLock.IsAnyModalBlockingWorldInteract || InteractionInputLock.IsSignboardOpen)
        {
            HidePrompt(true);
            return;
        }

        currentInteractable = ResolveBestInteractable(out string msg);
        if (currentInteractable != null)
        {
            ShowPrompt(msg);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision != playerCollider)
        {
            activeTriggerColliders.Add(collision);
        }
        RegisterTriggerInteractable(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision != null && collision != playerCollider)
        {
            activeTriggerColliders.Add(collision);
        }
        RegisterTriggerInteractable(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision == null) return;
        activeTriggerColliders.Remove(collision);

        interactableLookupBuffer.Clear();
        collision.GetComponents(interactableLookupBuffer);
        if (interactableLookupBuffer.Count == 0)
        {
            collision.GetComponentsInParent(false, interactableLookupBuffer);
        }

        for (int i = 0; i < interactableLookupBuffer.Count; i++)
        {
            IInteractable interactable = interactableLookupBuffer[i];
            if (interactable == null) continue;

            nearbyInteractables.Remove(interactable);
            if (interactable == currentInteractable)
            {
                currentInteractable = null;
                HidePrompt();
            }
        }
        interactableLookupBuffer.Clear();
    }

    private void ShowPrompt(string message)
    {
        if (promptPanel == null) EnsurePromptPanelReferences();
        if (promptPanel != null)
        {
            if (!promptPanel.activeSelf) promptPanel.SetActive(true);
            isShowingPrompt = true;
        }
        if (promptText != null && message != lastPromptMessage)
        {
            promptText.text = "[E] " + message;
            lastPromptMessage = message;
        }
    }

    private void HidePrompt(bool forceHide = false)
    {
        lastPromptMessage = null;
        if (promptPanel != null && (isShowingPrompt || forceHide) && promptPanel.activeSelf)
        {
            promptPanel.SetActive(false);
        }
        isShowingPrompt = false;
    }
}