using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FishFeedSpot : MonoBehaviour
{
    [Header("Target Ikan untuk Titik Ini")]
    [Tooltip("Tarik objek ikan (BiotaMovement) yang akan datang ke titik ini")]
    public BiotaMovement targetFish;

    [Tooltip("Opsional: titik spesifik spawn umpan (jika kosong, memakai posisi objek ini)")]
    public Transform baitSpawnPoint;

    public bool IsFed { get; private set; } = false;
    private FishFeedZone parentZone;

    public void Initialize(FishFeedZone zone)
    {
        parentZone = zone;
    }

    public Vector3 GetBaitPosition()
    {
        return baitSpawnPoint != null ? baitSpawnPoint.position : transform.position;
    }

    public void MarkAsFed()
    {
        IsFed = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !IsFed && parentZone != null)
        {
            parentZone.NotifyPlayerEnteredSpot(this);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !IsFed && parentZone != null)
        {
            parentZone.NotifyPlayerEnteredSpot(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && parentZone != null)
        {
            parentZone.NotifyPlayerExitedSpot(this);
        }
    }

    private void OnDisable()
    {
        if (parentZone != null)
        {
            parentZone.NotifyPlayerExitedSpot(this);
        }
    }
}