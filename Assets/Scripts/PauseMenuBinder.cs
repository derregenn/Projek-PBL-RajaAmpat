using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuBinder : MonoBehaviour
{
    private void Awake()
    {
        BindButtons();
    }

    private void Start()
    {
        BindButtons();
    }

    private void OnEnable()
    {
        BindButtons();
    }

    public void BindButtons()
    {
        // Cari semua Button di dalam hierarchy menu pause ini
        Button[] buttons = GetComponentsInChildren<Button>(true);
        if (buttons == null || buttons.Length == 0)
        {
            // Jika script terpasang di child, coba cari dari root canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                buttons = canvas.GetComponentsInChildren<Button>(true);
            }
        }

        if (buttons == null) return;

        foreach (Button btn in buttons)
        {
            string btnName = btn.gameObject.name.ToLower();

            if (btnName.Contains("resume"))
            {
                btn.onClick.RemoveListener(OnResumeClicked);
                btn.onClick.AddListener(OnResumeClicked);
            }
            else if (btnName.Contains("mainmenu") || btnName.Contains("menu"))
            {
                btn.onClick.RemoveListener(OnMainMenuClicked);
                btn.onClick.AddListener(OnMainMenuClicked);
            }
            else if (btnName.Contains("save"))
            {
                btn.onClick.RemoveListener(OnSaveClicked);
                btn.onClick.AddListener(OnSaveClicked);
            }
            else if (btnName.Contains("audio") || btnName.Contains("sound"))
            {
                btn.onClick.RemoveListener(OnToggleAudioClicked);
                btn.onClick.AddListener(OnToggleAudioClicked);
            }
        }
    }

    public void OnResumeClicked()
    {
        Debug.Log("[PauseMenuBinder] Resume diklik - menutup pause menu.");
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ResumeGame();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }

        Time.timeScale = 1f;

        // Tutup langsung GameObject ini atau parent Canvas_Pause jika masih aktif
        gameObject.SetActive(false);
        if (transform.parent != null && transform.parent.name.Contains("Canvas_Pause"))
        {
            transform.parent.gameObject.SetActive(false);
        }
    }

    public void OnMainMenuClicked()
    {
        Debug.Log("[PauseMenuBinder] Main Menu diklik.");
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BackToMainMenu();
        }
        else
        {
            SceneManager.LoadScene("01_MainMenu");
        }
    }

    public void OnSaveClicked()
    {
        Debug.Log("[PauseMenuBinder] Save Game diklik.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
        }
    }

    public void OnToggleAudioClicked()
    {
        Debug.Log("[PauseMenuBinder] Toggle Audio diklik.");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleAudio();
        }
    }
}
