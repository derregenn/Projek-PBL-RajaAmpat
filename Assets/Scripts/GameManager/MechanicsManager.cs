using UnityEngine;

public class MechanicsManager : MonoBehaviour
{
    [Header("Gallery Settings")]
    public GameObject galleryContainer; // UI Panel untuk Galeri

    [Header("Photography Settings")]
    public AudioClip cameraShutterClip;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) 
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        // Mekanik Galeri (Tab)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isGalleryOpen = galleryContainer.activeSelf;
            galleryContainer.SetActive(!isGalleryOpen);
            Time.timeScale = isGalleryOpen ? 1 : 0; // Pause game saat galeri terbuka
        }

        // Mekanik Fotografi (F)
        if (Input.GetKeyDown(KeyCode.F))
        {
            CapturePhoto();
        }
    }

    void CapturePhoto()
    {
        if (cameraShutterClip != null)
        {
            audioSource.PlayOneShot(cameraShutterClip);
        }
        Debug.Log("Cekrek! Foto berhasil diambil.");
        // Logika untuk menyimpan foto atau menampilkan notifikasi bisa ditambahkan di sini
    }
}