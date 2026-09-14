using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SimpleCamera : MonoBehaviour
{
    [Header("UI References")]
    public Image cameraFlash;
    public GameObject polaroidPopup;
    public Image popupPhotoDisplay; 
    public GameObject galleryUI; 
    public Image galleryDisplayImage; 

    public static List<Sprite> capturedPhotos = new List<Sprite>();
    private int currentIndex = 0;
    private bool isCapturing = false;

    void Start()
    {
        if (cameraFlash != null)
            cameraFlash.color = new Color(1, 1, 1, 0); 
        
        if (polaroidPopup != null)
            polaroidPopup.SetActive(false);
            
        if (galleryUI != null)
            galleryUI.SetActive(false);
    }

    void Update()
    {
        // Tekan F untuk Memotret Layar Gameplay
        if (Input.GetKeyDown(KeyCode.F) && !isCapturing)
        {
            StartCoroutine(CaptureGameplayScreen());
        }

        // Tekan Tab untuk Buka/Tutup Galeri
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isOpen = galleryUI.activeSelf;
            galleryUI.SetActive(!isOpen);
            Time.timeScale = isOpen ? 1 : 0; 

            if (!isOpen)
            {
                UpdateGalleryView();
            }
        }
    }

    private IEnumerator CaptureGameplayScreen()
    {
        isCapturing = true;

        // 1. Sembunyikan UI sementara agar hasil screenshot bersih dari tombol/flash
        if (polaroidPopup != null) polaroidPopup.SetActive(false);
        if (galleryUI != null) galleryUI.SetActive(false);

        // 2. Tunggu sampai frame selesai dirender sepenuhnya
        yield return new WaitForEndOfFrame();

        // 3. Ambil piksel layar game
        Texture2D screenTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTex.Apply();

        // 4. Ubah Texture2D menjadi Sprite agar bisa dibaca oleh UI Image
        Sprite gameSnapshot = Sprite.Create(
            screenTex, 
            new Rect(0, 0, screenTex.width, screenTex.height), 
            new Vector2(0.5f, 0.5f)
        );

        // 5. Nyalakan efek Flash Putih Full Screen
        if (cameraFlash != null)
        {
            cameraFlash.gameObject.SetActive(true);
            cameraFlash.color = new Color(1, 1, 1, 1);
        }

        // 6. Masukkan hasil tangkapan layar ke pop-up dan list galeri
        if (popupPhotoDisplay != null)
        {
            popupPhotoDisplay.gameObject.SetActive(true);
            popupPhotoDisplay.sprite = gameSnapshot;
        }

        capturedPhotos.Add(gameSnapshot);

        // 7. Munculkan pop-up polaroid
        if (polaroidPopup != null)
            polaroidPopup.SetActive(true);

        // 8. Pudarkan flash putih pelan-pelan
        float alpha = 1f;
        while (alpha > 0)
        {
            alpha -= Time.deltaTime * 2.5f;
            if (cameraFlash != null)
                cameraFlash.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);

        // 9. Matikan pop-up polaroid
        if (polaroidPopup != null)
            polaroidPopup.SetActive(false);

        isCapturing = false;
    }

    // Navigasi Galeri
    public void NextPhoto()
    {
        if (capturedPhotos.Count > 0)
        {
            currentIndex = (currentIndex + 1) % capturedPhotos.Count;
            UpdateGalleryView();
        }
    }

    public void PrevPhoto()
    {
        if (capturedPhotos.Count > 0)
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = capturedPhotos.Count - 1;
            UpdateGalleryView();
        }
    }

    private void UpdateGalleryView()
    {
        if (galleryDisplayImage != null && capturedPhotos.Count > 0)
        {
            galleryDisplayImage.gameObject.SetActive(true);
            galleryDisplayImage.sprite = capturedPhotos[currentIndex];
        }
        else if (galleryDisplayImage != null)
        {
            galleryDisplayImage.gameObject.SetActive(false); 
        }
    }
}