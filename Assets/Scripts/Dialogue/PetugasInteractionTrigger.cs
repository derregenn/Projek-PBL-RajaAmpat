using UnityEngine;

public class PetugasInteractionTrigger : MonoBehaviour
{
    private bool playerIsClose = false;
    private PetugasMisool petugasScript;

    private void Start()
    {
        petugasScript = GetComponent<PetugasMisool>();
    }

    private void Update()
    {
        // Jika pemain dekat dan menekan tombol E
        if (playerIsClose && Input.GetKeyDown(KeyCode.E))
        {
            if (petugasScript != null)
            {
                petugasScript.InteractWithOfficer();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerIsClose = true;
            Debug.Log("Tekan 'E' untuk berbicara dengan Petugas Konservasi.");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerIsClose = false;
        }
    }
}