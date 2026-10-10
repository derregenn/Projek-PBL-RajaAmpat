using UnityEngine;

public class NPCMapTrigger : MonoBehaviour
{
    public MapController mapController;
    private bool isMapOpen = false;

    private void Update()
    {
        if (isMapOpen && Input.GetKeyDown(KeyCode.E))
        {
            CloseWorldMap();
        }
    }

    // Hanya dipanggil melalui UnityEvent atau script event lain.
    public void OpenWorldMap()
    {
        if (mapController == null) return;

        mapController.OpenWorldMap();
        isMapOpen = true;
    }

    public void CloseWorldMap()
    {
        if (mapController == null) return;

        mapController.CloseWorldMap();
        isMapOpen = false;
    }
}