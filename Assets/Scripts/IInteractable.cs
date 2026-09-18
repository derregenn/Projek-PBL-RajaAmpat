using UnityEngine;

public interface IInteractable
{
    void Interact();
    string GetPromptText(); // Pesan bantuan (misal: "Baca Papan", "Ambil Sampah")
}
