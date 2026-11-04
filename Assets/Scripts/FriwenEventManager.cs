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
            // Pastikan GameObject-nya aktif
            qtePanel.SetActive(true); 
            
            // Panggil logika mulai QTE
            SequenceQTE seq = qtePanel.GetComponent<SequenceQTE>();
            if (seq != null)
            {
                seq.StartQTE();
            }
        }
    }

    public void QTESelesai()
    {
        StartCoroutine(RoutineQTESelesai());
    }

    private IEnumerator RoutineQTESelesai()
    {
        if (qtePanel != null) qtePanel.SetActive(false);
        SetPlayerCrouch(false);

        if (fader != null) yield return StartCoroutine(fader.FadeOutRoutine());

        if (perahuLepas != null) perahuLepas.SetActive(false);
        if (perahuKeikat != null) perahuKeikat.SetActive(true);
        if (npcWarlok1 != null) npcWarlok1.SetActive(true);

        if (camViewPerahu != null) camViewPerahu.Priority.Value = 0;
        if (playerCamera != null) playerCamera.Priority.Value = 10;

        if (fader != null) yield return StartCoroutine(fader.FadeInRoutine());
        
        FreezePlayer(false);
    }

    // ==========================================
    // ALUR GAGAL: FADE OUT -> RESET POSISI -> FADE IN
    // ==========================================
    public void QTEGagal()
    {
        StartCoroutine(RoutineQTEGagal());
    }

    private IEnumerator RoutineQTEGagal()
    {
        // 1. Matikan UI QTE & Animasi Jongkok
        if (qtePanel != null) qtePanel.SetActive(false);
        SetPlayerCrouch(false);

        // 2. Layar Fade Out ke Hitam Pekat
        if (fader != null) yield return StartCoroutine(fader.FadeOutRoutine());

        // 3. Teleport Posisi Player ke Reset Point saat Layar Masih Hitam
        if (playerScript != null && playerResetPoint != null)
        {
            playerScript.transform.position = playerResetPoint.position;
        }

        // 4. Balikkan Kamera ke Player
        if (camViewPerahu != null) camViewPerahu.Priority.Value = 0;
        if (playerCamera != null) playerCamera.Priority.Value = 10;

        yield return new WaitForSeconds(0.2f);

        // 5. Layar Fade In Kembali Terang
        if (fader != null) yield return StartCoroutine(fader.FadeInRoutine());

        // 6. Lepaskan Bekukan Player & Reset Flag Trigger
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
}