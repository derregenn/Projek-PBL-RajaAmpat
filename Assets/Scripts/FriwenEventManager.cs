using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class FriwenEventManager : MonoBehaviour
{
    [Header("Tahap 1: Referensi Perahu")]
    public GameObject perahuLepas;
    public GameObject perahuKeikat;
    public GameObject perahuMenjauh;

    [Header("Tahap 1: Kamera & Animasi")]
    public CinemachineCamera camViewPerahu;
    public CinemachineCamera playerCamera;
    public Animator playerAnimator;
    public string crouchBoolName = "isCrouching";

    [Header("Tahap 1: UI, NPC & Reset Point")]
    public GameObject qtePanel;
    public GameObject npcWarlok1;
    [Tooltip("Drag Starting point dari Hierarchy ke sini untuk titik reset saat QTE gagal")]
    public Transform playerResetPoint;
    [Tooltip("Waktu tunggu (detik) agar SFX QTE Berhasil sempat terputar sebelum UI ditutup")]
    [SerializeField] private float qteSuccessDelay = 0.6f; // 👈 Jeda suara sukses

    [Header("Tahap 2: Referensi Tour Guide (Pasir Timbul)")]
    public GameObject tourGuide;
    public Transform spotTourGuideSpawn; 

    private ScreenFader fader;
    private Player playerScript;
    private FriwenSequenceTrigger currentTrigger;
    private bool hasTriggeredPerahu = false;

    private void Start()
    {
        fader = ScreenFader.Instance != null ? ScreenFader.Instance : FindFirstObjectByType<ScreenFader>();
        playerScript = FindFirstObjectByType<Player>();

        if (perahuLepas) perahuLepas.SetActive(true);
        if (perahuKeikat) perahuKeikat.SetActive(false);
        if (perahuMenjauh) perahuMenjauh.SetActive(false);
        if (npcWarlok1) npcWarlok1.SetActive(false);
        if (tourGuide) tourGuide.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggeredPerahu) return;

        if (other.CompareTag("Player"))
        {
            hasTriggeredPerahu = true;
            StartSequencePerahu();
        }
    }

    public void StartSequencePerahu(FriwenSequenceTrigger trigger = null)
    {
        currentTrigger = trigger;
        StartCoroutine(RoutineSequencePerahu());
    }

    private IEnumerator RoutineSequencePerahu()
    {
        FreezePlayer(true);

        if (camViewPerahu != null) camViewPerahu.Priority.Value = 30;
        yield return new WaitForSeconds(2.0f);

        if (fader != null) yield return StartCoroutine(fader.FadeOutRoutine());
        if (fader != null) yield return StartCoroutine(fader.FadeInRoutine());

        SetPlayerCrouch(true);
        
        if (qtePanel != null) 
        {
            qtePanel.SetActive(true); 
            
            SequenceQTE seq = qtePanel.GetComponent<SequenceQTE>();
            if (seq != null)
            {
                seq.StartQTE();
            }
        }
    }

    public void QTESelesai()
    {
        if (QuestController.Instance != null)
        {
            QuestController.Instance.ProgressObjective("perahu_friwen", 1);
        }
        StartCoroutine(RoutineQTESelesai());
    }

    // ==========================================
    // ALUR SUKSES QTE (DENGAN JEDA SFX)
    // ==========================================
    private IEnumerator RoutineQTESelesai()
    {
        // 1. Beri jeda waktu agar SFX sukses QTE terputar mulus sampai habis
        yield return new WaitForSeconds(qteSuccessDelay);

        // 2. Baru matikan UI QTE dan animasi crouch
        if (qtePanel != null) qtePanel.SetActive(false);
        SetPlayerCrouch(false);

        // 3. Lanjutkan Fade Out ke perahu terikat
        if (fader != null) yield return StartCoroutine(fader.FadeOutRoutine());

        if (perahuLepas != null) perahuLepas.SetActive(false);
        if (perahuKeikat != null) perahuKeikat.SetActive(true);
        if (npcWarlok1 != null) npcWarlok1.SetActive(true);

        if (camViewPerahu != null) camViewPerahu.Priority.Value = 0;
        if (playerCamera != null) playerCamera.Priority.Value = 10;

        if (fader != null) yield return StartCoroutine(fader.FadeInRoutine());
        
        FreezePlayer(false);
    }

    public void QTEGagal()
    {
        StartCoroutine(RoutineQTEGagal());
    }

    private IEnumerator RoutineQTEGagal()
    {
        if (qtePanel != null) qtePanel.SetActive(false);
        SetPlayerCrouch(false);

        if (fader != null) yield return StartCoroutine(fader.FadeOutRoutine());

        if (playerScript != null && playerResetPoint != null)
        {
            playerScript.transform.position = playerResetPoint.position;
        }

        if (camViewPerahu != null) camViewPerahu.Priority.Value = 0;
        if (playerCamera != null) playerCamera.Priority.Value = 10;

        yield return new WaitForSeconds(0.2f);

        if (fader != null) yield return StartCoroutine(fader.FadeInRoutine());

        FreezePlayer(false);
        hasTriggeredPerahu = false;
        if (currentTrigger != null) currentTrigger.ResetTrigger();
    }

    private void FreezePlayer(bool freeze)
    {
        if (playerScript == null) playerScript = FindFirstObjectByType<Player>();
        if (playerScript == null) return;

        playerScript.enabled = !freeze;

        Rigidbody2D rb = playerScript.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; 
        }

        if (freeze)
        {
            Animator anim = playerScript.GetComponent<Animator>();
            if (anim == null) anim = playerScript.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.Play("Player_Idle");
            }
        }
    }

    private void SetPlayerCrouch(bool isCrouching)
    {
        if (playerAnimator == null && playerScript != null)
        {
            playerAnimator = playerScript.GetComponentInChildren<Animator>();
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetBool(crouchBoolName, isCrouching);
        }
    }

    public void GantiPerahuMenjauh()
    {
        if (perahuLepas != null) perahuLepas.SetActive(false);
        if (perahuKeikat != null) perahuKeikat.SetActive(false);
        if (perahuMenjauh != null) perahuMenjauh.SetActive(true);
    }

    public void MunculkanTourGuide()
    {
        StartCoroutine(RoutineMunculTourGuide());
    }

    private IEnumerator RoutineMunculTourGuide()
    {
        FreezePlayer(true);

        if (fader != null) yield return StartCoroutine(fader.FadeOutRoutine());

        if (tourGuide != null && spotTourGuideSpawn != null)
        {
            tourGuide.transform.position = spotTourGuideSpawn.position;
            tourGuide.SetActive(true);
        }

        if (fader != null) yield return StartCoroutine(fader.FadeInRoutine());
        
        FreezePlayer(false);
        
        if (camViewPerahu != null) camViewPerahu.Priority.Value = 0;
        if (playerCamera != null) playerCamera.Priority.Value = 10;
    }

    [Header("Tahap 2: Trigger Foto")]
    [SerializeField] private GameObject areaFotoPasirTimbul;

    public void OnPhotoTakenSequence()
    {
        StartCoroutine(RoutinePostFotoSequence());
    }

    private IEnumerator RoutinePostFotoSequence()
    {
        FreezePlayer(true);

        if (fader != null) yield return StartCoroutine(fader.FadeOutRoutine());

        if (tourGuide != null)
        {
            if (spotTourGuideSpawn != null)
            {
                tourGuide.transform.position = spotTourGuideSpawn.position;
                tourGuide.transform.rotation = spotTourGuideSpawn.rotation;
            }
            tourGuide.SetActive(true);
        }

        if (areaFotoPasirTimbul != null)
        {
            areaFotoPasirTimbul.SetActive(false);
        }

        yield return new WaitForSeconds(0.3f);

        if (fader != null) yield return StartCoroutine(fader.FadeInRoutine());

        if (tourGuide != null)
        {
            Dialogue dialogueTourGuide = tourGuide.GetComponent<Dialogue>();
            if (dialogueTourGuide != null)
            {
                dialogueTourGuide.StartDialogue();
            }
        }

        FreezePlayer(false);
    }
}