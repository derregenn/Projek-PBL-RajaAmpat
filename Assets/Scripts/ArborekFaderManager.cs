using System.Collections;
using UnityEngine;

public class ArborekFaderManager : MonoBehaviour
{
    private bool isTransitioning = false; 

    [Header("Pengaturan Durasi Fade")]
    [Tooltip("Waktu dari layar terang sampai benar-benar hitam pekat.")]
    public float durasiMenujuHitam = 1.0f; 
    [Tooltip("Waktu dari layar hitam pekat sampai terang kembali.")]
    public float durasiMenujuTerang = 1.0f;

    [Header("Transisi 1 (Awal -> Terima Karang)")]
    public GameObject npcMatiTransisi1;
    public GameObject npcHidupTransisi1;

    [Header("Transisi Sasi (Sasi Before / Titik Sasi -> Sasi After)")]
    public GameObject npcSasiBefore;
    public GameObject npcSasiAfter;

    [Header("Transisi 2 (Sasi -> Fotografi)")]
    public GameObject npcMatiTransisi2;
    public GameObject npcHidupTransisi2;

    [Header("Transisi 3 (Fotografi -> Tour Guide)")]
    public GameObject npcFotografi; // NPC_TetuaAdat_Fotografi (TETAP VISIBEL)
    public GameObject npcTourGuide; // GameObject NPC Tour Guide

    [Header("Transisi 4 (Foto Selesai -> Hilangkan Kedua NPC)")]
    public GameObject npcFotoToHide;      // Drag NPC_TetuaAdat_Fotografi
    public GameObject npcTourGuideToHide; // Drag TourGuide

    // --- METHOD PEMANGGIL TRANSISI ---

    public void PlayTransition1()
    {
        if (!isTransitioning) StartCoroutine(FadeAndSwap(npcMatiTransisi1, npcHidupTransisi1, true));
    }

    public void PlayTransitionSasi()
    {
        if (!isTransitioning) StartCoroutine(FadeAndSwap(npcSasiBefore, npcSasiAfter, true));
    }

    public void PlayTransition2()
    {
        if (!isTransitioning) StartCoroutine(FadeAndSwap(npcMatiTransisi2, npcHidupTransisi2, true));
    }

    public void PlayTransition3()
    {
        if (!isTransitioning) StartCoroutine(FadeAndSwap(npcFotografi, npcTourGuide, false));
    }

    // PANGGIL METHOD INI SETELAH QUEST FOTO / DIALOG TOUR GUIDE SELESAI
    public void PlayTransition4()
    {
        if (!isTransitioning) StartCoroutine(FadeAndHideBoth(npcFotoToHide, npcTourGuideToHide));
    }

    // --- COROUTINES UTAMA ---

    private IEnumerator FadeAndSwap(GameObject npcToDisable, GameObject npcToEnable, bool disableOldNpc)
    {
        isTransitioning = true; 

        if (ScreenFader.Instance != null)
        {
            StartCoroutine(ScreenFader.Instance.FadeOutRoutine()); 
        }

        yield return new WaitForSecondsRealtime(durasiMenujuHitam);

        CloseDialogueUI();

        if (npcToDisable != null)
        {
            ResetNPCCamera(npcToDisable);

            if (disableOldNpc)
            {
                npcToDisable.SetActive(false);
            }
            else
            {
                Collider2D col = npcToDisable.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
                
                Dialogue oldDialogue = npcToDisable.GetComponentInChildren<Dialogue>();
                if (oldDialogue != null) oldDialogue.enabled = false;
            }
        }

        if (npcToEnable != null)
        {
            npcToEnable.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(0.5f); 

        if (ScreenFader.Instance != null)
        {
            StartCoroutine(ScreenFader.Instance.FadeInRoutine());
        }

        yield return new WaitForSecondsRealtime(durasiMenujuTerang);
        isTransitioning = false; 
    }

    private IEnumerator FadeAndHideBoth(GameObject npc1, GameObject npc2)
    {
        isTransitioning = true;

        // 1. Fade ke hitam
        if (ScreenFader.Instance != null)
        {
            StartCoroutine(ScreenFader.Instance.FadeOutRoutine());
        }

        yield return new WaitForSecondsRealtime(durasiMenujuHitam);

        // 2. Hilangkan kedua NPC & reset kamera saat layar hitam pekat
        CloseDialogueUI();

        if (npc1 != null)
        {
            ResetNPCCamera(npc1);
            npc1.SetActive(false);
        }

        if (npc2 != null)
        {
            ResetNPCCamera(npc2);
            npc2.SetActive(false);
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // 3. Fade kembali terang
        if (ScreenFader.Instance != null)
        {
            StartCoroutine(ScreenFader.Instance.FadeInRoutine());
        }

        yield return new WaitForSecondsRealtime(durasiMenujuTerang);
        isTransitioning = false;
    }

    private void ResetNPCCamera(GameObject npcObj)
    {
        Dialogue dialogue = npcObj.GetComponentInChildren<Dialogue>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (dialogue != null && dialogue.dialogueCamera != null)
        {
            dialogue.dialogueCamera.Priority.Value = 0;

            if (playerObj != null)
            {
                dialogue.dialogueCamera.Target.TrackingTarget = playerObj.transform;
            }
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