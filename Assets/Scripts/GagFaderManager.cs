using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GagFaderManager : MonoBehaviour
{
    private bool isTransitioning = false;
    public float durasiFade = 1.0f;

    [Header("Transisi 1: Peneliti Pamit Pulang (Quest 2 Minta Air)")]
    [Tooltip("Drag NPC_Peneliti_Awal (Peneliti Cewek) ke sini")]
    public GameObject npcPeneliti;

    [Header("Transisi 2: Ending Pulau Gag (Pindah Scene)")]
    public string sceneTujuanEnding = "02_Waisai";

    // =========================================================
    // PUBLIC METHODS FOR INSPECTOR EVENTS
    // =========================================================

    // 1. Panggil ini di On Dialogue End NPC_Peneliti_Awal saat MC minta air bersih
    public void PenelitiPulang()
    {
        if (!isTransitioning) StartCoroutine(RoutinePenelitiPulang());
    }

    // 2. Panggil ini di On Dialogue End TourGuide di Dermaga
    public void PindahKeWaisai()
    {
        if (!isTransitioning) StartCoroutine(RoutinePindahScene(sceneTujuanEnding));
    }

    // Alternatif jika ingin pindah ke scene lain
    public void PindahKeScene(string sceneName)
    {
        if (!isTransitioning) StartCoroutine(RoutinePindahScene(sceneName));
    }

    // =========================================================
    // COROUTINES
    // =========================================================

    private IEnumerator RoutinePenelitiPulang()
    {
        isTransitioning = true;
        ScreenFader fader = ScreenFader.Instance != null ? ScreenFader.Instance : FindFirstObjectByType<ScreenFader>();

        // Fade out ke hitam
        if (fader != null) yield return StartCoroutine(fader.FadeOutRoutine());
        else yield return new WaitForSeconds(durasiFade);

        CloseDialogueUI();

        // Nonaktifkan Peneliti Cewek (pamit pulang ke lab pusat)
        if (npcPeneliti != null)
        {
            ResetKamera(npcPeneliti);
            npcPeneliti.SetActive(false);
        }

        yield return new WaitForSeconds(0.3f);

        // Fade in kembali ke game
        if (fader != null) yield return StartCoroutine(fader.FadeInRoutine());
        isTransitioning = false;
    }

    private IEnumerator RoutinePindahScene(string sceneName)
    {
        isTransitioning = true;
        ScreenFader fader = ScreenFader.Instance != null ? ScreenFader.Instance : FindFirstObjectByType<ScreenFader>();

        // Fade ke layar hitam
        if (fader != null) yield return StartCoroutine(fader.FadeOutRoutine());
        else yield return new WaitForSeconds(durasiFade);

        CloseDialogueUI();

        // Pindah Scene setelah layar hitam pekat
        SceneManager.LoadScene(sceneName);
    }

    private void ResetKamera(GameObject npc)
    {
        if (npc == null) return;
        Dialogue dialogue = npc.GetComponentInChildren<Dialogue>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");

        if (dialogue != null && dialogue.dialogueCamera != null)
        {
            dialogue.dialogueCamera.Priority.Value = 0;
            if (playerObj != null) dialogue.dialogueCamera.Target.TrackingTarget = playerObj.transform;
        }
    }

    private void CloseDialogueUI()
    {
        GameObject dialogueBox = GameObject.Find("DialogueBox");
        if (dialogueBox != null) dialogueBox.SetActive(false);

        GameObject playerDialogueBox = GameObject.Find("PlayerDialogueBox");
        if (playerDialogueBox != null) playerDialogueBox.SetActive(false);
    }
}