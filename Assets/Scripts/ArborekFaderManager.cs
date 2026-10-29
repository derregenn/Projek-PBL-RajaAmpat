using System.Collections;
using UnityEngine;

public class ArborekFaderManager : MonoBehaviour
{
    [Header("Transisi 1 (Contoh: Awal -> Sasi)")]
    public GameObject npcMatiTransisi1;
    public GameObject npcHidupTransisi1;

    [Header("Transisi 2 (Contoh: Sasi -> Fotografi)")]
    public GameObject npcMatiTransisi2;
    public GameObject npcHidupTransisi2;

    [Header("Transisi 3 (Fotografi -> Tour Guide)")]
    public GameObject npcFotografi; // Drag NPC_TetuaAdat_Fotografi (TETAP VISIBEL)
    public GameObject npcTourGuide; // Drag GameObject NPC Tour Guide

    public void PlayTransition1()
    {
        StartCoroutine(FadeAndSwap(npcMatiTransisi1, npcHidupTransisi1, true));
    }

    public void PlayTransition2()
    {
        StartCoroutine(FadeAndSwap(npcMatiTransisi2, npcHidupTransisi2, true));
    }

    public void PlayTransition3()
    {
        StartCoroutine(FadeAndSwap(npcFotografi, npcTourGuide, false));
    }

    private IEnumerator FadeAndSwap(GameObject npcToDisable, GameObject npcToEnable, bool disableOldNpc)
    {
        // 1. Fade layar jadi hitam
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeOutRoutine());
        }

        // 2. Saat layar hitam: Tutup UI dialog & atur kamera + NPC
        CloseDialogueUI();

        if (npcToDisable != null)
        {
            Dialogue oldDialogue = npcToDisable.GetComponentInChildren<Dialogue>();
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (oldDialogue != null && oldDialogue.dialogueCamera != null)
            {
                oldDialogue.dialogueCamera.Priority.Value = 0;

                if (playerObj != null)
                {
                    oldDialogue.dialogueCamera.Target.TrackingTarget = playerObj.transform;
                }
            }

            if (disableOldNpc)
            {
                // Transisi 1 & 2: Matikan GameObject NPC lama
                npcToDisable.SetActive(false);
            }
            else
            {
                // Transisi 3: NPC Fotografi TETAP KELIHATAN, 
                // tapi matikan Collider & script Dialogue-nya agar tidak membajak kamera/interaksi!
                Collider2D col = npcToDisable.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                if (oldDialogue != null) oldDialogue.enabled = false;
            }
        }

        if (npcToEnable != null)
        {
            npcToEnable.SetActive(true);
        }

        yield return new WaitForSeconds(0.5f); 

        // 3. Fade layar kembali terang
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeInRoutine());
        }
    }

    private void CloseDialogueUI()
    {
        GameObject dialogueBox = GameObject.Find("DialogueBox");
        if (dialogueBox != null) dialogueBox.SetActive(false);

        GameObject playerDialogueBox = GameObject.Find("PlayerDialogueBox");
        if (playerDialogueBox != null) playerDialogueBox.SetActive(false);
    }
}