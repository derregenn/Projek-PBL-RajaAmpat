using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class JournalEntry
{
    public int pageID; 
    public Sprite pageBackgroundSprite; 
    public bool requiresPhoto = false; 
    public int photoIDToDisplay;
}

public class QuestJournalManager : MonoBehaviour
{
    public static QuestJournalManager Instance; 

    public Image cameraFlash;
    public GameObject polaroidPopup;
    public Image popupPhotoDisplay;

    public Image pianemoAwardImage;         
    public Sprite pianemoDisableSprite;     
    public Sprite pianemoActiveSprite;      

    public GameObject questTabPanel;
    public Image folderImage; 
    public TextMeshProUGUI ongoingQuestText; 

    public Sprite tabOngoingSprite;   
    public Sprite tabCompletedSprite; 
    
    private bool isOngoingTabActive = true; 

    public Image journalPageImage;        
    public Image journalPhotoImage;       
    public GameObject noPhotoWarning; 
    public TextMeshProUGUI pageNumberText; 

    public JournalEntry[] allJournalDatabase; 
    public List<int> unlockedPages = new List<int>(); 

    public Sprite[] allPhotoSpritesDatabase; 
    private Dictionary<int, Sprite> collectedPhotos = new Dictionary<int, Sprite>();

    private int currentJournalIndex = 0;
    private bool isFlashing = false;

    [HideInInspector] public string currentOngoingQuest = "Jelajahi keindahan Raja Ampat!";
    [HideInInspector] public string lastCompletedQuest = "Belum ada misi selesai.";

    public TextMeshProUGUI progressPercentageText;
    public int totalQuestsInIsland = 15;
    private int completedQuestsCount = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (cameraFlash != null) cameraFlash.color = new Color(1, 1, 1, 0);
        if (polaroidPopup != null) polaroidPopup.SetActive(false);
        if (questTabPanel != null) questTabPanel.SetActive(false);

        if (allJournalDatabase.Length > 0 && !unlockedPages.Contains(allJournalDatabase[0].pageID))
        {
            unlockedPages.Add(allJournalDatabase[0].pageID);
        }
        
        UpdateProgressUI();
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

    public void TakeSpecificPhoto(int photoID)
    {
        if (isFlashing) return; 

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

    private void UpdateJournalUI()
    {
        RefreshQuestTabText();

        if (unlockedPages.Count > 0)
        {
            int currentPageID = unlockedPages[currentJournalIndex];
            JournalEntry entry = GetPageEntryByID(currentPageID);

            if (entry != null)
            {
                if (journalPageImage != null)
                {
                    journalPageImage.sprite = entry.pageBackgroundSprite;
                    journalPageImage.gameObject.SetActive(true);
                }

                if (entry.requiresPhoto)
                {
                    if (collectedPhotos.ContainsKey(entry.photoIDToDisplay))
                    {
                        journalPhotoImage.sprite = collectedPhotos[entry.photoIDToDisplay];
                        journalPhotoImage.gameObject.SetActive(true); 
                        if (noPhotoWarning != null) noPhotoWarning.SetActive(false);
                    }
                    else
                    {
                        journalPhotoImage.gameObject.SetActive(false); 
                        if (noPhotoWarning != null) noPhotoWarning.SetActive(true);
                    }
                }
                else
                {
                    if (journalPhotoImage != null) journalPhotoImage.gameObject.SetActive(false);
                    if (noPhotoWarning != null) noPhotoWarning.SetActive(false);
                }

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

    public void UnlockNewPage(int pageID)
    {
        if (!unlockedPages.Contains(pageID))
        {
            unlockedPages.Add(pageID);
        }
    }

    public void UnlockPianemoAward()
    {
        if (pianemoAwardImage != null && pianemoActiveSprite != null)
        {
            pianemoAwardImage.gameObject.SetActive(true);
            pianemoAwardImage.sprite = pianemoActiveSprite;
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

    public void AddCompletedQuest()
    {
        completedQuestsCount++;
        UpdateProgressUI();
    }

private void UpdateProgressUI()
    {
        if (progressPercentageText != null)
        {
            float percentage = (completedQuestsCount / (float)totalQuestsInIsland) * 100f;
            
            // Membulatkan angka (misal 6.67 menjadi 6)
            int displayPercentage = Mathf.FloorToInt(percentage);
            
            progressPercentageText.text = "Progress: " + displayPercentage + "%";
        }
    }
}