#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ResetProgressTool
{
    static ResetProgressTool()
    {
        // Otomatis reset PlayerPrefs satu kali saat Unity compile agar test play langsung mulai dari 0
        if (!EditorPrefs.GetBool("FreshResetDone_v2", false))
        {
            ResetAllProgress();
            EditorPrefs.SetBool("FreshResetDone_v2", true);
        }
    }

    [MenuItem("Tools/Reset Game Progress To 0 (Fresh Test Play) %#r")]
    public static void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("<color=green>[ResetProgressTool] PlayerPrefs berhasil dibersihkan! Seluruh progress jurnal, award, dan trivia kembali ke 0.</color>");
    }
}
#endif

