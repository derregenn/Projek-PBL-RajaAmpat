using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SasiInteractable : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag PromptPanel (Tombol E di atas kepala MC) ke sini")]
    public GameObject uiPrompt;

    [Tooltip("Drag QTE Panel (Lingkaran QTE Biru / QTE1 / QTE2) ke sini")]
    public GameObject qtePanel;

    [Header("Animation Setup")]
    [Tooltip("Drag GameObject Player atau Animator MC ke sini (Boleh kosong, script akan auto-find tag Player)")]
    public Animator playerAnimator;
    [Tooltip("Nama parameter Bool di Animator Player untuk jongkok")]
    public string crouchBoolName = "isCrouching";

    [Header("Events")]
    public UnityEvent OnSasiCompleted; // Dipanggil saat QTE sukses

    private bool isPlayerInArea = false;
    private bool isQTEActive = false;
    private bool isCompleted = false;
    public bool IsCompleted => isCompleted;

    private void Start()
    {
        // Pastikan QTE Biru mati di awal
        if (qtePanel != null) qtePanel.SetActive(false);
    }

    private void Update()
    {
        // Jika player di area, QTE belum jalan, belum selesai, dan menekan tombol E
        if (isPlayerInArea && !isQTEActive && !isCompleted && Input.GetKeyDown(KeyCode.E))
        {
            MulaiQTE();
        }
    }

    private void MulaiQTE()
    {
        isQTEActive = true;

        // 1. Sembunyikan prompt tombol E di atas kepala
        if (uiPrompt != null) uiPrompt.SetActive(false);

        // 2. Pemicu Animasi Jongkok MC
        SetPlayerCrouch(true);

        // 3. Tampilkan UI QTE Biru & sambungkan callback QTE2 secara dinamis
        if (qtePanel != null)
        {
            QTE2 qte2 = qtePanel.GetComponent<QTE2>();
            if (qte2 != null)
            {
                qte2.onSuccess.RemoveListener(FinishQTE);
                qte2.onSuccess.AddListener(FinishQTE);
                qte2.onFail.RemoveListener(CancelQTE);
                qte2.onFail.AddListener(CancelQTE);
            }

            qtePanel.SetActive(true);

            if (qte2 != null)
            {
                qte2.BeginQTE();
            }
        }
    }

    public void CancelQTE()
    {
        if (qtePanel != null)
        {
            QTE2 qte2 = qtePanel.GetComponent<QTE2>();
            if (qte2 != null)
            {
                qte2.onSuccess.RemoveListener(FinishQTE);
                qte2.onFail.RemoveListener(CancelQTE);
            }
            qtePanel.SetActive(false);
        }

        isQTEActive = false;
        SetPlayerCrouch(false);

        if (isPlayerInArea && uiPrompt != null)
        {
            uiPrompt.SetActive(true);
        }
    }

    // Panggil fungsi ini dari Event / Manager QTE saat QTE Selesai / Berhasil
    public void FinishQTE()
    {
        if (isCompleted) return;
        isCompleted = true;
        isQTEActive = false;
        isPlayerInArea = false;

        QTE2 qte2 = qtePanel != null ? qtePanel.GetComponent<QTE2>() : null;
        if (qte2 != null)
        {
            qte2.onSuccess.RemoveListener(FinishQTE);
            qte2.onFail.RemoveListener(CancelQTE);
        }

        // 1. Kembalikan Animasi MC ke berdiri
        SetPlayerCrouch(false);

        // 2. Sembunyikan QTE Biru & Prompt
        if (qtePanel != null) qtePanel.SetActive(false);
        if (uiPrompt != null) uiPrompt.SetActive(false);

        // 3. Matikan Collider2D saja (JANGAN matikan gameObject parent karena Batu Sasi & Sasi Laut1_Quest Item_Midground_0 adalah child dari TitikSasi!)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 4. Aktifkan seluruh child objek visual Sasi (Batu Sasi & Sasi Laut1_Quest Item_Midground_0)
        foreach (Transform child in transform)
        {
            if (child != null)
            {
                child.gameObject.SetActive(true);
            }
        }

        // 5. Jalankan event sukses & objective trigger
        QuestObjectiveTrigger trig = GetComponent<QuestObjectiveTrigger>();
        if (trig != null)
        {
            trig.OnObjectiveTriggered?.Invoke();
            if (qte2 == null && QuestController.Instance != null && !string.IsNullOrEmpty(trig.objectiveID))
            {
                QuestController.Instance.ProgressObjective(trig.objectiveID, trig.amountToAdd);
            }
            trig.OnObjectiveCompleted?.Invoke();
        }

        OnSasiCompleted?.Invoke();

        // Pastikan ulang child tetap aktif jika ada event Inspector yang tidak sengaja mematikan parent
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        foreach (Transform child in transform)
        {
            if (child != null) child.gameObject.SetActive(true);
        }

        // 6. Cek apakah semua titik Sasi di scene sudah selesai
        SasiInteractable[] allSasi = FindObjectsByType<SasiInteractable>(FindObjectsSortMode.None);
        bool allSasiDone = true;
        if (allSasi != null && allSasi.Length > 0)
        {
            foreach (var s in allSasi)
            {
                if (s != null && !s.IsCompleted)
                {
                    allSasiDone = false;
                    break;
                }
            }
        }

        if (allSasiDone)
        {
            // Cari QuestGiver pada TitikSasi ini atau TitikSasi lainnya untuk memberikan Quest06_Arborek
            QuestGiver giver = GetComponent<QuestGiver>();
            if (giver == null && allSasi != null)
            {
                foreach (var s in allSasi)
                {
                    if (s != null)
                    {
                        giver = s.GetComponent<QuestGiver>();
                        if (giver != null) break;
                    }
                }
            }
            if (giver != null)
            {
                giver.GiveQuest();
            }

            ArborekFaderManager fader = FindFirstObjectByType<ArborekFaderManager>();
            if (fader != null)
            {
                fader.CompleteSasiImmediateAndTransition();
            }
        }
    }

    private void SetPlayerCrouch(bool isCrouching)
    {
        // Jika animator belum di-assign di Inspector, cari otomatis di GameObject Tag Player
        if (playerAnimator == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerAnimator = player.GetComponentInChildren<Animator>();
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetBool(crouchBoolName, isCrouching);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isQTEActive && !isCompleted)
        {
            isPlayerInArea = true;
            if (uiPrompt != null) uiPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInArea = false;
            if (uiPrompt != null) uiPrompt.SetActive(false);
        }
    }
}