using UnityEngine;
using TMPro; // Wajib ditambahkan untuk memanggil UI Text milikmu

public class TrashInteractable : MonoBehaviour, IInteractable
{
    [Header("Quest Integration (Dari Regen)")]
    [SerializeField] private string objectiveID = "collect_trash";
    [SerializeField] private int amountToAdd = 1;

    [Header("Audio & FX (Opsional)")]
    [SerializeField] private AudioClip collectSound;

    [Header("UI Collectibles (Dari Kamu)")]
    [Tooltip("Tarik teks 'Progress' dari papan cokelat ke kolom ini")]
    public TextMeshProUGUI collectibleUIText; 
    
    [Tooltip("Isi ID Jurnal jika sampah ini ngebuka cerita baru. Biarkan -1 jika tidak.")]
    public int unlockJournalID = -1; 
    
    // Total sampah global yang sudah diambil pemain
    public static int totalCollected = 0;
    
    // Sesuaikan dengan total sampah mentok yang ada di game kalian
    private int maxTrash = 20; 

    public void Interact()
    {
        // 1. Jalankan Logika Quest Regen
        if (QuestController.Instance != null && !string.IsNullOrEmpty(objectiveID))
        {
            QuestController.Instance.ProgressObjective(objectiveID, amountToAdd);
        }

        // 2. Putar Suara
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        // 3. Update UI Progress Bar Kamu
        totalCollected += amountToAdd;
        if (collectibleUIText != null)
        {
            float persentase = ((float)totalCollected / maxTrash) * 100f;
            collectibleUIText.text = "Progress: " + persentase.ToString("F0") + "%"; // "F0" agar tidak ada koma desimal
        }

        // 4. Buka halaman jurnal baru jika sampah ini punya cerita (Integrasi UI Kanan)
        if (unlockJournalID != -1 && QuestJournalManager.Instance != null)
        {
            QuestJournalManager.Instance.TakeSpecificPhoto(unlockJournalID);
        }

        // 5. Hapus sampah dari layar
        Destroy(gameObject);
    }

    public string GetPromptText() => "Ambil Sampah";
}