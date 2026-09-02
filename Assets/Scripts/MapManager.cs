using UnityEngine;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour
{
    // Fungsi ini dipanggil saat tombol pulau di UI Peta diklik
    public void SelectIsland(string islandSceneName)
    {
        Time.timeScale = 1; // Kembalikan waktu normal
        SceneManager.LoadScene(islandSceneName);
    }

    // Fungsi untuk menutup kembali peta jika pemain batal memilih
    public void CloseMap(GameObject mapUI)
    {
        mapUI.SetActive(false);
        Time.timeScale = 1;
    }
}