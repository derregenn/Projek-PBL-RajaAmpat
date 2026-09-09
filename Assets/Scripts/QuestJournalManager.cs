using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// --- STRUKTUR DATABASE JURNAL ---
[System.Serializable]
public class JournalEntry
{
    public int photoID; 
    public string title;
    [TextArea(3, 10)] public string triviaDescription; // TextArea agar kotaknya besar di Inspector
    public Sprite photoIllustration; // Tempat menaruh gambar HD dari Visual Designer
}

public class QuestJournalManager : MonoBehaviour
{
    // Singleton: Cara cepat agar skrip Trigger di luar sana bisa "berbicara" langsung ke skrip ini
    public static QuestJournalManager Instance; 

    [Header("Photography Popup UI")]
    public Image cameraFlash;
    public GameObject polaroidPopup;
    public Image popupPhotoDisplay;

    [Header("Quest Tab UI (Kiri)")]
    public GameObject questTabPanel;
    public TextMeshProUGUI ongoingQuestText; 
    
    [Header("Journal UI (Kanan)")]
    public TextMeshProUGUI journalTitleText;
    public TextMeshProUGUI journalTriviaText;
    public Image journalPhotoImage;
    public GameObject noPhotoWarning; 

    [Header("Database (Isi Sesuai GDD)")]
    public JournalEntry[] allJournalDatabase; 
    
    // List ini mencatat ID foto apa saja yang sudah berhasil difoto oleh pemain
    public List<int> unlockedPhotoIDs = new List<int>();

    private int currentJournalIndex = 0;
    private bool isFlashing = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (cameraFlash != null) cameraFlash.color = new Color(1, 1, 1, 0); // Pastikan layar bening di awal
        if (polaroidPopup != null) polaroidPopup.SetActive(false);
        if (questTabPanel != null) questTabPanel.SetActive(false);
    }

    void Update()
    {
        // Tombol Tab: Buka tutup UI Jurnal dan Pause Game
        if (Input.GetKeyDown(KeyCode.Tab) && !isFlashing)
        {
            bool isOpen = questTabPanel.activeSelf;
            questTabPanel.SetActive(!isOpen);
            Time.timeScale = isOpen ? 1 : 0; 

            if (!isOpen)
            {
                UpdateJournalUI(); // Refresh isi jurnal setiap kali dibuka
            }
        }
    }

    // --- FUNGSI JEPRET FOTO (Dipanggil dari Trigger Area) ---
    public void TakeSpecificPhoto(int idToUnlock)
    {
        if (isFlashing) return; // Jangan biarkan pemain jepret berkali-kali saat masih flash

        JournalEntry entry = GetEntryByID(idToUnlock);
        if (entry != null)
        {
            // Tambahkan ke memori jika belum pernah difoto
            if (!unlockedPhotoIDs.Contains(idToUnlock))
            {
                unlockedPhotoIDs.Add(idToUnlock);
            }
            StartCoroutine(PhotoSequence(entry.photoIllustration));
        }
    }

    private IEnumerator PhotoSequence(Sprite illustration)
    {
        isFlashing = true;
        
        // 1. Flash Putih
        if (cameraFlash != null) cameraFlash.color = new Color(1, 1, 1, 1);

        // 2. Munculkan kotak polaroid di tengah layar beserta ilustrasinya
        if (polaroidPopup != null) polaroidPopup.SetActive(true);
        if (popupPhotoDisplay != null) popupPhotoDisplay.sprite = illustration;

        // 3. Pudarkan Flash
        float alpha = 1f;
        while (alpha > 0)
        {
            alpha -= Time.deltaTime * 2.5f;
            if (cameraFlash != null) cameraFlash.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        // 4. Biarkan pemain melihat hasil fotonya selama 2 detik
        yield return new WaitForSeconds(2f); 

        // 5. Tutup pop-up
        if (polaroidPopup != null) polaroidPopup.SetActive(false);
        isFlashing = false;
    }

    // --- UPDATE TAMPILAN BUKU JURNAL ---
    private void UpdateJournalUI()
    {
        // Teks sementara, nanti kamu bisa buat sistem Quest terpisah untuk ini
        ongoingQuestText.text = "Objective:\nJelajahi keindahan Raja Ampat!";

        if (unlockedPhotoIDs.Count > 0)
        {
            // Ambil ID foto yang sedang dilihat saat ini
            int currentID = unlockedPhotoIDs[currentJournalIndex];
            JournalEntry entry = GetEntryByID(currentID);

            if (entry != null)
            {
                // Tampilkan Teks & Foto
                journalTitleText.text = entry.title;
                journalTriviaText.text = entry.triviaDescription;
                journalPhotoImage.sprite = entry.photoIllustration;
                
                journalPhotoImage.gameObject.SetActive(true);
                noPhotoWarning.SetActive(false);
            }
        }
        else
        {
            // Jika pemain belum motret satupun
            journalTitleText.text = "JURNAL KOSONG";
            journalTriviaText.text = "";
            journalPhotoImage.gameObject.SetActive(false);
            noPhotoWarning.SetActive(true);
        }
    }

    // --- FUNGSI TOMBOL NAVIGASI (< & >) ---
    public void NextPage()
    {
        if (unlockedPhotoIDs.Count > 1)
        {
            currentJournalIndex = (currentJournalIndex + 1) % unlockedPhotoIDs.Count;
            UpdateJournalUI();
        }
    }

    public void PrevPage()
    {
        if (unlockedPhotoIDs.Count > 1)
        {
            currentJournalIndex--;
            if (currentJournalIndex < 0) currentJournalIndex = unlockedPhotoIDs.Count - 1;
            UpdateJournalUI();
        }
    }

    // Mencari data di database berdasarkan ID
    private JournalEntry GetEntryByID(int id)
    {
        foreach (var entry in allJournalDatabase)
        {
            if (entry.photoID == id) return entry;
        }
        return null;
    }
}