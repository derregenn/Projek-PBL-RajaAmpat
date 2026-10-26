using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class FaderTrigger : MonoBehaviour
{
    [Header("Referensi Objek Scene")]
    public GameObject perahuLepas;
    public GameObject perahuKeiket;
    public Transform startingPoint;
    public GameObject qteUI;
    public GameObject triggerQTEPerahu;
    public GameObject npcWarlok;

    // --- 1. ALUR GAGAL (Double Fade) ---
    public void PlayFailSequence()
    {
        StartCoroutine(FailSequenceRoutine());
    }

    private IEnumerator FailSequenceRoutine()
    {
        // Fade 1: Ke layar hitam untuk memunculkan perahu lepas
        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeOutRoutine());

        if (perahuKeiket != null) perahuKeiket.SetActive(false);
        if (perahuLepas != null) perahuLepas.SetActive(true);
        if (qteUI != null) qteUI.SetActive(false);

        // Fade 1 Terang: Pemain nonton perahu lepas selama 2 detik
        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeInRoutine());

        yield return new WaitForSeconds(2.0f);

        // Fade 2: Ke layar hitam untuk reset posisi player DAN reset perahu keikat
        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeOutRoutine());

        // Reset player & reset perahu kembali terikat
        TeleportPlayerToPoint(startingPoint);
        if (perahuLepas != null) perahuLepas.SetActive(false);
        if (perahuKeiket != null) perahuKeiket.SetActive(true);

        // Fade 2 Terang: Layar terbuka kembali di titik awal
        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeInRoutine());
    }

    // --- 2. ALUR BERHASIL (Single Fade) ---
    public void PlaySuccessSequence()
    {
        StartCoroutine(SuccessSequenceRoutine());
    }

    private IEnumerator SuccessSequenceRoutine()
    {
        // 1. Fade ke layar hitam
        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeOutRoutine());

        // 2. Ubah visual perahu & aktifkan NPC di balik layar hitam
        if (perahuLepas != null) perahuLepas.SetActive(false);
        if (perahuKeiket != null) perahuKeiket.SetActive(true);
        if (qteUI != null) qteUI.SetActive(false);
        if (npcWarlok != null) npcWarlok.SetActive(true);

        // Matikan collider trigger dulu agar tidak bisa memicu QTE lagi
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 3. Fade terang kembali (Layar kembali jelas!)
        if (ScreenFader.Instance != null)
            yield return StartCoroutine(ScreenFader.Instance.FadeInRoutine());

        // 4. Jalankan dialog NPC setelah layar sudah terang
        if (npcWarlok != null)
        {
            var dialogue = npcWarlok.GetComponentInChildren<Dialogue>();
            if (dialogue != null)
            {
                dialogue.StartDialogue();
            }
        }

        // 5. Matikan GameObject trigger di paling akhir agar Coroutine tidak mati prematur
        if (triggerQTEPerahu != null && triggerQTEPerahu != gameObject)
        {
            triggerQTEPerahu.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void TeleportPlayerToPoint(Transform targetPoint)
    {
        if (targetPoint == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");

        if (player != null)
        {
            player.transform.position = targetPoint.position;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }
}