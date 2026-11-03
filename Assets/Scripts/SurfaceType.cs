using UnityEngine;

// Opsi jenis tanah yang ada di dalam game
public enum SurfaceKind
{
    Wood,
    Sand,
    Stone,
    Dirt
}

public class SurfaceType : MonoBehaviour
{
    [Tooltip("Pilih jenis permukaan untuk objek collider ini")]
    public SurfaceKind surfaceKind = SurfaceKind.Wood;
}