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
    private readonly Dictionary<Collider2D, IInteractable> triggerInteractableCache = new Dictionary<Collider2D, IInteractable>();
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
            HidePrompt();
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
            HidePrompt();
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

    private void RefreshOverlappingInteractables()
    {
        if (playerCollider == null) return;

        overlapResults.Clear();
        playerCollider.Overlap(overlapFilter, overlapResults);

        nearbyInteractables.Clear();
        for (int i = 0; i < overlapResults.Count; i++)
        {
            Collider2D col = overlapResults[i];
            if (col == null || col == playerCollider) continue;

            if (!triggerInteractableCache.TryGetValue(col, out IInteractable interactable))
            {
                interactable = col.GetComponent<IInteractable>() ?? col.GetComponentInParent<IInteractable>();
                triggerInteractableCache[col] = interactable;
            }

            if (interactable != null && !nearbyInteractables.Contains(interactable))
            {
                nearbyInteractables.Add(interactable);
            }
        }
        overlapResults.Clear();
    }

    private void CleanupNearbyInteractables()
    {
        for (int i = nearbyInteractables.Count - 1; i >= 0; i--)
        {
            IInteractable item = nearbyInteractables[i];
            if (item == null || (item is Object uObj && uObj == null))
            {
                nearbyInteractables.RemoveAt(i);
                continue;
            }
            if (item is Behaviour beh && (!beh.enabled || !beh.gameObject.activeInHierarchy))
            {
                nearbyInteractables.RemoveAt(i);
            }
        }
    }

    private IInteractable ResolveBestInteractable(out string promptMessage)
    {
        for (int i = nearbyInteractables.Count - 1; i >= 0; i--)
        {
            IInteractable item = nearbyInteractables[i];
            string msg = item.GetPromptText();
            if (!string.IsNullOrEmpty(msg))
            {
                promptMessage = msg;
                return item;
            }
        }

        promptMessage = null;
        return null;
    }

    private void RegisterTriggerInteractable(Collider2D collision)
    {
        if (!triggerInteractableCache.TryGetValue(collision, out IInteractable interactable))
        {
            interactable = collision.GetComponent<IInteractable>() ?? collision.GetComponentInParent<IInteractable>();
            triggerInteractableCache[collision] = interactable;
        }
        if (interactable == null) return;

        if (!nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }

        if (InteractionInputLock.IsAnyModalBlockingWorldInteract || InteractionInputLock.IsSignboardOpen)
        {
            HidePrompt();
            return;
        }

        string msg = interactable.GetPromptText();
        if (string.IsNullOrEmpty(msg)) return;
        currentInteractable = interactable;
        ShowPrompt(msg);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        RegisterTriggerInteractable(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        RegisterTriggerInteractable(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!triggerInteractableCache.TryGetValue(collision, out IInteractable interactable))
        {
            interactable = collision.GetComponent<IInteractable>() ?? collision.GetComponentInParent<IInteractable>();
        }
        else
        {
            triggerInteractableCache.Remove(collision);
        }

        if (interactable != null)
        {
            nearbyInteractables.Remove(interactable);
            if (interactable == currentInteractable)
            {
                currentInteractable = null;
                HidePrompt();
            }
        }
    }

    private void ShowPrompt(string message)
    {
        if (promptPanel != null && !promptPanel.activeSelf) promptPanel.SetActive(true);
        if (promptText != null && message != lastPromptMessage)
        {
            promptText.text = "[E] " + message;
            lastPromptMessage = message;
        }
    }

    private void HidePrompt()
    {
        lastPromptMessage = null;
        if (promptPanel != null && promptPanel.activeSelf) promptPanel.SetActive(false);
    }
}