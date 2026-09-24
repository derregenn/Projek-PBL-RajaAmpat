using UnityEngine;
using UnityEngine.SceneManagement;

public class MapTeleport : MonoBehaviour
{
    // Fungsi ini akan dieksekusi saat tombol pulau diklik
    public void PindahKePulau(string namaSceneTujuan)
    {
        // Kembalikan waktu normal (jika saat buka map gamenya dipause/Time.timeScale = 0)
        Time.timeScale = 1f; 
        
        // Perintah untuk memuat scene baru
        SceneManager.LoadScene(namaSceneTujuan);
    }
}