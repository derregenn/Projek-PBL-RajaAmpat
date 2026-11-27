using UnityEngine;

public class MapTeleport : MonoBehaviour
{
    // Fungsi ini yang dipanggil oleh On Click () tombol di Inspector
    public void PindahKePulau(string sceneName)
    {
        BoatTransitionController.NavigateToIsland(sceneName);
    }
}
