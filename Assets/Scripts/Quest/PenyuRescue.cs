using UnityEngine;
public class PenyuRescue : MonoBehaviour
{
    public GameObject penyuSprite;
    public Dialogue tourGuideDialogue;

    public void OnQTESuccess()
    {
        if (UniversalFader.Instance != null)
        {
            UniversalFader.Instance.FadeTransition(() =>
            {
                penyuSprite.SetActive(false);
                PlayerPrefs.SetInt("Misool_QuestStage", 1); PlayerPrefs.Save();
                MisoolLandSceneManager.Instance?.RefreshLandState();
                tourGuideDialogue.StartDialogue(); // Tour Guide bicara
            });
        }
    }
}