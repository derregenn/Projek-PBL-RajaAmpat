using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Struktur database yang memisahkan Sprite Halaman Buku dan Opsi Foto Polaroid
[System.Serializable]
public class JournalEntry
{
    public int pageID; 
    [Tooltip("Sprite halaman buku penuh (latar buku + teks dari desainer)")]
    public Sprite pageBackgroundSprite; 

    [Header("Pengaturan Foto Polaroid (Opsional)")]
    [Tooltip("Centang jika halaman ini butuh menampilkan hasil foto jepretan pemain")]
    public bool requiresPhoto = false; 
    [Tooltip("ID foto yang akan dicetak di halaman ini (jika requiresPhoto dicentang)")]
    public int photoIDToDisplay;
}

public class QuestJournalManager : MonoBehaviour
{
    public static QuestJournalManager Instance; 

    [Header("Photography Popup UI")]
    public Image cameraFlash;
    public GameObject polaroidPopup;
    public Image popupPhotoDisplay;

    [Header("Award Pianemo Collectible")]
    public Image pianemoAwardImage;         
    public Sprite pianemoDisableSprite;     
    public Sprite pianemoActiveSprite;      

    [Header("Quest Tab UI (Kiri)")]
    public GameObject questTabPanel;
    public Image folderImage; 
    public TextMeshProUGUI ongoingQuestText; 

    [Header("Aset Tab (Dari Desainer)")]
    public Sprite tabOngoingSprite;   
    public Sprite tabCompletedSprite; 
    
    private bool isOngoingTabActive = true; 

    [Header("Journal UI (Kanan)")]
    public Image journalPageImage;        // Menampilkan sprite halaman buku penuh dari desainer
    public Image journalPhotoImage;       // Wadah foto hasil jepretan (hanya muncul jika halaman butuh foto)
    public GameObject noPhotoWarning; 
    public TextMeshProUGUI pageNumberText; 

    [Header("Database Buku Jurnal (Isi Sesuai GDD)")]
    public JournalEntry[] allJournalDatabase; // Daftar semua halaman buku
    public List<int> unlockedPages = new List<int>(); // Halaman yang sudah terbuka

    // Database terpisah khusus menyimpan koleksi foto jepretan pemain (untuk mekanik fotografi)
    [Header("Database Foto Hasil Jepretan")]
    public Sprite[] allPhotoSpritesDatabase; 
    private Dictionary<int, Sprite> collectedPhotos = new Dictionary<int, Sprite>();

    private int currentJournalIndex = 0;
    private bool isFlashing = false;

    [HideInInspector] public string currentOngoingQuest = "Jelajahi keindahan Raja Ampat!";
    [HideInInspector] public string lastCompletedQuest = "Belum ada misi selesai.";

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (cameraFlash != null) cameraFlash.color = new Color(1, 1, 1, 0);
        if (polaroidPopup != null) polaroidPopup.SetActive(false);
        if (questTabPanel != null) questTabPanel.SetActive(false);

        // Otomatis buka halaman pertama (ID 0) saat game mulai agar buku tidak kosong melompong
        if (allJournalDatabase.Length > 0 && !unlockedPages.Contains(allJournalDatabase[0].pageID))
        {
            unlockedPages.Add(allJournalDatabase[0].pageID);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !isFlashing)
        {
            bool isOpen = questTabPanel.activeSelf;
            questTabPanel.SetActive(!isOpen);
            Time.timeScale = isOpen ? 1 : 0; 

            if (!isOpen)
            {
                UpdateJournalUI();
            }
        }
    }

    public void ToggleQuestTab()
    {
        isOngoingTabActive = !isOngoingTabActive; 
        if (folderImage != null)
        {
            folderImage.sprite = isOngoingTabActive ? tabOngoingSprite : tabCompletedSprite;
        }
        RefreshQuestTabText(); 
    }

    public void RefreshQuestTabText()
    {
        if (ongoingQuestText != null)
        {
            if (isOngoingTabActive)
            {
                ongoingQuestText.text = "Objective:\n" + currentOngoingQuest;
            }
            else
            {
                ongoingQuestText.text = "Selesai:\n" + lastCompletedQuest;
            }
        }
    }

    // --- MEKANIK FOTOGRAFI ---
    public void TakeSpecificPhoto(int photoID)
    {
        if (isFlashing) return; 

        // Cari sprite foto dari database foto
        Sprite photoSprite = GetPhotoSpriteByID(photoID);
        if (photoSprite != null)
        {
            if (!collectedPhotos.ContainsKey(photoID))
            {
                collectedPhotos.Add(photoID, photoSprite);
            }
            StartCoroutine(PhotoSequence(photoSprite));
        }
    }

    private IEnumerator PhotoSequence(Sprite illustration)
    {
        isFlashing = true;
        
        if (cameraFlash != null) cameraFlash.color = new Color(1, 1, 1, 1);
        if (polaroidPopup != null) polaroidPopup.SetActive(true);
        if (popupPhotoDisplay != null) popupPhotoDisplay.sprite = illustration;

        float alpha = 1f;
        while (alpha > 0)
        {
            alpha -= Time.unscaledDeltaTime * 2.5f;
            if (cameraFlash != null) cameraFlash.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(2f); 

        if (polaroidPopup != null) polaroidPopup.SetActive(false);
        isFlashing = false;
    }

    // --- UPDATE TAMPILAN JURNAL ---
    private void UpdateJournalUI()
    {
        RefreshQuestTabText();

        if (unlockedPages.Count > 0)
        {
            int currentPageID = unlockedPages[currentJournalIndex];
            JournalEntry entry = GetPageEntryByID(currentPageID);

            if (entry != null)
            {
                // 1. Tampilkan Sprite Halaman Buku Full dari Desainer
                if (journalPageImage != null)
                {
                    journalPageImage.sprite = entry.pageBackgroundSprite;
                    journalPageImage.gameObject.SetActive(true);
                }

                // 2. CEK APAKAH HALAMAN INI BUTUH FOTO POLAROID
                if (entry.requiresPhoto)
                {
                    // Cek apakah pemain sudah memotret foto untuk halaman ini
                    if (collectedPhotos.ContainsKey(entry.photoIDToDisplay))
                    {
                        journalPhotoImage.sprite = collectedPhotos[entry.photoIDToDisplay];
                        journalPhotoImage.gameObject.SetActive(true); // Munculkan foto
                        if (noPhotoWarning != null) noPhotoWarning.SetActive(false);
                    }
                    else
                    {
                        // Belum dipotret, sembunyikan wadah foto / tampilkan peringatan
                        journalPhotoImage.gameObject.SetActive(false); 
                        if (noPhotoWarning != null) noPhotoWarning.SetActive(true);
                    }
                }
                else
                {
                    // Halaman ini murni teks/cerita dari desainer, matikan wadah foto!
                    if (journalPhotoImage != null) journalPhotoImage.gameObject.SetActive(false);
                    if (noPhotoWarning != null) noPhotoWarning.SetActive(false);
                }

                // Update Nomor Halaman
                if (pageNumberText != null)
                {
                    pageNumberText.text = "Page " + (currentJournalIndex + 1);
                }
            }
        }
        else
        {
            if (journalPageImage != null) journalPageImage.gameObject.SetActive(false);
            if (journalPhotoImage != null) journalPhotoImage.gameObject.SetActive(false);
            if (noPhotoWarning != null) noPhotoWarning.SetActive(true);
            if (pageNumberText != null) pageNumberText.text = "Page 0";
        }
    }

    public void NextPage()
    {
        if (unlockedPages.Count > 1)
        {
            currentJournalIndex = (currentJournalIndex + 1) % unlockedPages.Count;
            UpdateJournalUI();
        }
    }

    public void PrevPage()
    {
        if (unlockedPages.Count > 1)
        {
            currentJournalIndex--;
            if (currentJournalIndex < 0) currentJournalIndex = unlockedPages.Count - 1;
            UpdateJournalUI();
        }
    }

    // Fungsi untuk membuka halaman baru (bisa dipanggil dari Quest Selesai)
    public void UnlockNewPage(int pageID)
    {
        if (!unlockedPages.Contains(pageID))
        {
            unlockedPages.Add(pageID);
            Debug.Log("Halaman Jurnal Baru Terbuka! ID: " + pageID);
        }
    }

    public void UnlockPianemoAward()
    {
        if (pianemoAwardImage != null && pianemoActiveSprite != null)
        {
            pianemoAwardImage.gameObject.SetActive(true);
            pianemoAwardImage.sprite = pianemoActiveSprite;
            Debug.Log("Award Pianemo Berhasil Terbuka dan Menyala!");
        }
    }

    private JournalEntry GetPageEntryByID(int id)
    {
        foreach (var entry in allJournalDatabase)
        {
            if (entry.pageID == id) return entry;
        }
        return null;
    }

    private Sprite GetPhotoSpriteByID(int id)
    {
        if (id >= 0 && id < allPhotoSpritesDatabase.Length)
        {
            return allPhotoSpritesDatabase[id];
        }
        return null;
    }

    public void CloseJournal()
    {
        if (questTabPanel != null) questTabPanel.SetActive(false);
        Time.timeScale = 1f; 
    }
}