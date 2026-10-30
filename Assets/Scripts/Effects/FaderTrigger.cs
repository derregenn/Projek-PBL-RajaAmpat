using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class FaderTrigger : MonoBehaviour
{
    [Header("Screen Fader Settings")]
    [Tooltip("Drag GameObject FadePanel (yang punya script ScreenFader) ke sini!")]
    public ScreenFader customScreenFader;

    [Header("Referensi Objek Scene")]
    public GameObject perahuLepas;
    public GameObject perahuKeiket;
    public Transform startingPoint;
    public GameObject qteUI;
    public GameObject triggerQTEPerahu;
    public GameObject npcWarlok;

    // Helper untuk mengambil Fader tanpa gagal
    private ScreenFader GetFader()
    {
        if (customScreenFader != null) return customScreenFader;
        if (ScreenFader.Instance != null) return ScreenFader.Instance;
        return FindFirstObjectByType<ScreenFader>();
    }

    // --- FUNGSI FADE & HIDE NPC (WAISAI) ---
    public void TriggerFadeAndHide(GameObject targetNPC)
    {
        ScreenFader fader = GetFader();

        if (fader != null)
        {
            // PENTING: Jalankan Coroutine di objek ScreenFader!
            // Supaya saat targetNPC mati (SetActive false), Coroutine TIDAK ikut terbunuh di tengah jalan.
            fader.StartCoroutine(FadeAndHideRoutine(targetNPC, fader));
        }
        else
        {
            StartCoroutine(FadeAndHideRoutine(targetNPC, null));
        }
    }

    private IEnumerator FadeAndHideRoutine(GameObject targetNPC, ScreenFader fader)
    {
        // 1. Fade ke layar hitam
        if (fader != null)
        {
            yield return fader.FadeOutRoutine();
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        // 2. Tutup UI Dialog jika masih terbuka
        GameObject dialogueBox = GameObject.Find("DialogueBox");
        if (dialogueBox != null) dialogueBox.SetActive(false);

        GameObject playerDialogueBox = GameObject.Find("PlayerDialogueBox");
        if (playerDialogueBox != null) playerDialogueBox.SetActive(false);

        // 3. Reset Kamera ke Player & Matikan NPC di balik layar hitam
        if (targetNPC != null)
        {
            Dialogue dialogue = targetNPC.GetComponentInChildren<Dialogue>();
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("Player");

            if (dialogue != null && dialogue.dialogueCamera != null)
            {
                dialogue.dialogueCamera.Priority.Value = 0;
                if (playerObj != null)
                {
                    dialogue.dialogueCamera.Target.TrackingTarget = playerObj.transform;
                }
            }

            // NPC dimatikan di sini (Coroutine tetap aman running di ScreenFader)
            targetNPC.SetActive(false);
        }

        yield return new WaitForSeconds(0.3f);

        // 4. Fade kembali terang
        if (fader != null)
        {
            yield return fader.FadeInRoutine();
        }
    }

    // --- 1. ALUR GAGAL (Double Fade) ---
    public void PlayFailSequence()
    {
        ScreenFader fader = GetFader();
        if (fader != null)
        {
            fader.StartCoroutine(FailSequenceRoutine(fader));
        }
    }

    private IEnumerator FailSequenceRoutine(ScreenFader fader)
    {
        if (fader != null)
            yield return fader.FadeOutRoutine();

        if (perahuKeiket != null) perahuKeiket.SetActive(false);
        if (perahuLepas != null) perahuLepas.SetActive(true);
        if (qteUI != null) qteUI.SetActive(false);

        if (fader != null)
            yield return fader.FadeInRoutine();

        yield return new WaitForSeconds(2.0f);

        if (fader != null)
            yield return fader.FadeOutRoutine();

        TeleportPlayerToPoint(startingPoint);
        if (perahuLepas != null) perahuLepas.SetActive(false);
        if (perahuKeiket != null) perahuKeiket.SetActive(true);

        if (fader != null)
            yield return fader.FadeInRoutine();
    }

    // --- 2. ALUR BERHASIL (Single Fade) ---
    public void PlaySuccessSequence()
    {
        ScreenFader fader = GetFader();
        if (fader != null)
        {
            fader.StartCoroutine(SuccessSequenceRoutine(fader));
        }
    }

    private IEnumerator SuccessSequenceRoutine(ScreenFader fader)
    {
        if (fader != null)
            yield return fader.FadeOutRoutine();

        if (perahuLepas != null) perahuLepas.SetActive(false);
        if (perahuKeiket != null) perahuKeiket.SetActive(true);
        if (qteUI != null) qteUI.SetActive(false);
        if (npcWarlok != null) npcWarlok.SetActive(true);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (fader != null)
            yield return fader.FadeInRoutine();

        if (npcWarlok != null)
        {
            var dialogue = npcWarlok.GetComponentInChildren<Dialogue>();
            if (dialogue != null)
            {
                dialogue.StartDialogue();
            }
        }

        if (triggerQTEPerahu != null && triggerQTEPerahu != gameObject)
        {
            triggerQTEPerahu.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void TeleportPlayerToPoint(Transform targetPoint)
    {
        if (targetPoint == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");

        if (player != null)
        {
            player.transform.position = targetPoint.position;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }
}