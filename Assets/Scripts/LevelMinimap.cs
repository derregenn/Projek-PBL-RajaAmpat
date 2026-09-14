using UnityEngine;
using System.Collections.Generic;
using TMPro; // Gunakan UnityEngine.UI jika memakai Text standar

public class LevelMinimap : MonoBehaviour
{
    [System.Serializable]
    public class QuestMarker
    {
        public Transform target;
        public RectTransform iconUI;
        public bool isPassed = false;
    }

    [Header("World Boundaries")]
    public Transform startPoint;
    public Transform endPoint;
    public Transform player;

    [Header("UI Track Elements")]
    public RectTransform trackBar;
    public RectTransform playerIcon;
    public GameObject questIconPrefab;
    public Transform questContainer;

    [Header("Quest Targets")]
    public List<Transform> questTargets = new List<Transform>();

    private float trackWidth;
    private List<QuestMarker> questMarkers = new List<QuestMarker>();

    void Start()
    {
        trackWidth = trackBar.rect.width > 0 ? trackBar.rect.width : trackBar.sizeDelta.x;
        SpawnQuestIcons();
    }

    void Update()
    {
        if (player == null || startPoint == null || endPoint == null) return;

        float totalDistance = endPoint.position.x - startPoint.position.x;
        if (Mathf.Abs(totalDistance) <= 0.01f) return;

        float playerProgress = (player.position.x - startPoint.position.x) / totalDistance;
        playerProgress = Mathf.Clamp01(playerProgress);
        playerIcon.anchoredPosition = new Vector2(playerProgress * trackWidth, playerIcon.anchoredPosition.y);

        CheckPassedQuests();
    }

    void SpawnQuestIcons()
    {
        float totalDistance = endPoint.position.x - startPoint.position.x;
        if (Mathf.Abs(totalDistance) <= 0.01f) return;

        for (int i = 0; i < questTargets.Count; i++)
        {
            if (questTargets[i] == null) continue;

            float questProgress = (questTargets[i].position.x - startPoint.position.x) / totalDistance;
            questProgress = Mathf.Clamp01(questProgress);

            GameObject iconObj = Instantiate(questIconPrefab, questContainer);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();

            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);

            float offsetY = (i % 2 == 0) ? 25f : -25f;
            iconRect.anchoredPosition = new Vector2(questProgress * trackWidth, offsetY);

            // Set teks abjad (0 = 'A', 1 = 'B', 2 = 'C', dst.)
            TMP_Text label = iconObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                char letter = (char)('A' + i);
                label.text = letter.ToString();
            }

            questMarkers.Add(new QuestMarker
            {
                target = questTargets[i],
                iconUI = iconRect,
                isPassed = false
            });
        }
    }

    void CheckPassedQuests()
    {
        foreach (var marker in questMarkers)
        {
            if (marker.isPassed || marker.iconUI == null) continue;

            if (marker.target == null || player.position.x >= marker.target.position.x)
            {
                marker.isPassed = true;
                Destroy(marker.iconUI.gameObject);
            }
        }
    }
}