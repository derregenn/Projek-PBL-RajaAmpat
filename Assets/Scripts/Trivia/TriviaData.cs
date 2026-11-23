using UnityEngine;

[System.Serializable]
public class TriviaQuestion
{
    [TextArea(2, 5)] public string questionText; // Teks pertanyaan
    public string[] options = new string[4];     // 4 Pilihan jawaban
    public int correctAnswerIndex;             // Indeks jawaban benar (0, 1, 2, atau 3)

    [Header("Feedback Per Pertanyaan (Opsional - Inspector)")]
    [TextArea(1, 3)] public string correctFeedback; // Pesan khusus jika jawaban benar
    [TextArea(1, 3)] public string wrongFeedback;   // Pesan khusus jika jawaban salah
}