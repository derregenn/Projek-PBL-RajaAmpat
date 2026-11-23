using UnityEngine;

/// <summary>
/// Pasang ke satu GameObject kosong di setiap scene yang memiliki sampah.
/// Tugasnya: memberi tahu GlobalTrashManager berapa total sampah di scene ini.
///
/// CARA SETUP DI UNITY INSPECTOR:
///   1. Buat GameObject kosong, beri nama "GlobalTrashSceneConfig".
///   2. Pasang script ini.
///   3. Isi "Scene Key" (nama unik untuk scene ini, misal: "04_Misool").
///   4. Isi "Total Trash In This Scene" (jumlah sampah di scene ini).
///
/// CATATAN: Pastikan Scene Key sama persis di setiap scene yang berbeda.
///          Total hanya didaftarkan sekali ke PlayerPrefs per Scene Key.
/// </summary>
public class GlobalTrashSceneConfig : MonoBehaviour
{
    [Header("Identitas Scene")]
    [Tooltip("Nama unik scene ini. Gunakan nama scene Unity-nya, misal '04_Misool'.")]
    [SerializeField] private string sceneKey = "";

    [Header("Jumlah Sampah di Scene Ini")]
    [Tooltip("Jumlah total objek TrashPickup yang ada di scene ini.")]
    [SerializeField] private int totalTrashInScene = 0;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            sceneKey = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        if (totalTrashInScene > 0)
        {
            GlobalTrashManager.Instance.RegisterTotalForScene(sceneKey, totalTrashInScene);
        }
    }
}
