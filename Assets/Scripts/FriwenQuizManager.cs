using UnityEngine;
using System.Collections;

public class FriwenQuizManager : MonoBehaviour
{
    [Header("Referensi NPC & Panel")]
    public GameObject quizPanelUtama;    // GameObject kotak hitam Kuis
    public Dialogue scriptKuis;          // Skrip Dialogue NPC_Warlok2

    [Header("Referensi Post-Trivia & Quest")]
    public Dialogue dialoguePostTrivia;  // Dialog "Nah kalau jalan terus..."
    public QuestGiver pemberiQuestFoto;  // Komponen yang ngasih Quest03

    private bool quizSudahSelesai = false;

    private void Start()
    {
        // Matikan panel kuis di awal game
        if (quizPanelUtama != null) quizPanelUtama.SetActive(false);
    }

    // ==========================================
    // DIPANGGIL DARI SIGNBOARD PAS PAPAN DITUTUP
    // ==========================================
    public void MulaiKuisFriwen()
    {
        if (quizSudahSelesai) return; // Cegah kuis muncul lagi kalau sudah beres
        StartCoroutine(RoutineMulaiKuis());
    }

    private IEnumerator RoutineMulaiKuis()
    {
        yield return new WaitForSeconds(0.1f);

        // 1. Nyalakan Quiz Panel
        if (quizPanelUtama != null)
        {
            quizPanelUtama.SetActive(true);
            
            // 2. Munculkan teks pertanyaan trivia
            if (scriptKuis != null)
            {
                scriptKuis.ShowTriviaQuestion("Mengapa kita dilarang keras menyenggol atau menginjak dinding karang saat berada di sekitar perairan Friwen Wall ?");
            }
        }
    }

    // ==========================================
    // DIPANGGIL SAAT JAWABAN KUIS BENAR
    // ==========================================
    public void KuisDijawabBenar()
    {
        quizSudahSelesai = true; // Kunci permanen biar nggak ke-trigger lagi

        // 1. Matikan Panel Kuis
        if (quizPanelUtama != null) quizPanelUtama.SetActive(false);

        // 2. Munculkan Dialog Post-Trivia
        if (dialoguePostTrivia != null)
        {
            dialoguePostTrivia.StartCustomDialogue(null, SelesaiPostTriviaLaluKasihQuest);
        }
    }

    // ==========================================
    // DIPANGGIL SAAT POST-TRIVIA KELAR
    // ==========================================
    private void SelesaiPostTriviaLaluKasihQuest()
    {
        // 3. Paksa Quest Foto Muncul!
        if (pemberiQuestFoto != null)
        {
            pemberiQuestFoto.GiveQuest();
        }
    }
}