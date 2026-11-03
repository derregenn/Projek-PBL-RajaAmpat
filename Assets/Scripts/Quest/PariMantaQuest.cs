using UnityEngine;

public class PariMantaQuest : MonoBehaviour
{
    [SerializeField] private GameObject jaringObject;
    private bool isFreed = false;
    private bool canTakePhoto = false;

    // 1. Panggil saat QTE Lepas Jaring Berhasil
    public void OnQTEFreedSuccess()
    {
        if (jaringObject != null) jaringObject.SetActive(false);
        isFreed = true;
        canTakePhoto = true;
        Debug.Log("Pari Manta Bebas! Tekan 'F' untuk mengambil foto.");
    }

    private void Update()
    {
        // 2. Mekanik Foto dengan tombol F
        if (canTakePhoto && Input.GetKeyDown(KeyCode.F))
        {
            TakeMantaPhoto();
        }
    }

    private void TakeMantaPhoto()
    {
        canTakePhoto = false;

        // Simpan ke Jurnal / Trigger UI Kamera Flash jika ada
        Debug.Log("Foto Pari Manta Berhasil Dimasukkan ke Jurnal!");

        // Set stage ke 8 (Siap lapor ke Ibu-Ibu)
        PlayerPrefs.SetInt("Misool_QuestStage", 8);
        PlayerPrefs.Save();
    }
}