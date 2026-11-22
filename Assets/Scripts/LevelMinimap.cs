using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

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
    public List<Sprite> questTargetIcons = new List<Sprite>();

    private float trackWidth;
    private List<QuestMarker> questMarkers = new List<QuestMarker>();
    private bool hasSpawnedIcons = false;

    void Awake()
    {
        EnsureRequiredReferences();
    }

    void Start()
    {
        EnsureRequiredReferences();

        if (trackBar != null)
        {
            trackWidth = trackBar.rect.width > 0 ? trackBar.rect.width : trackBar.sizeDelta.x;
        }

        if (playerIcon != null)
        {
            playerIcon.anchorMin = new Vector2(0f, 0.5f);
            playerIcon.anchorMax = new Vector2(0f, 0.5f);
        }

        SpawnQuestIcons();
    }

    void Update()
    {
        // 1. Auto-healing jika referensi player, startPoint, atau endPoint hilang / belum terpasang di Inspector
        if (player == null || startPoint == null || endPoint == null)
        {
            EnsureRequiredReferences();
            if (player == null || startPoint == null || endPoint == null) return;
        }

        if (trackBar == null || playerIcon == null) return;

        // 2. Pastikan lebar track selalu responsif terhadap layout Canvas / resolusi
        float currentTrackWidth = trackBar.rect.width > 0 ? trackBar.rect.width : trackBar.sizeDelta.x;
        if (currentTrackWidth <= 0.01f) currentTrackWidth = trackWidth;
        if (currentTrackWidth <= 0.01f) return;
        trackWidth = currentTrackWidth;

        // 3. Jika quest icon belum ter-spawn karena startPoint/endPoint baru ditemukan di runtime
        if (!hasSpawnedIcons)
        {
            SpawnQuestIcons();
        }

        float totalDistance = endPoint.position.x - startPoint.position.x;
        if (Mathf.Abs(totalDistance) <= 0.01f) return;

        // 4. Hitung progresi pergerakan player (0 = startPoint, 1 = endPoint)
        float playerProgress = (player.position.x - startPoint.position.x) / totalDistance;
        playerProgress = Mathf.Clamp01(playerProgress);

        float iconHalfWidth = playerIcon.rect.width * Mathf.Abs(playerIcon.localScale.x) * 0.5f;
        float iconOffset = Mathf.Min(iconHalfWidth, trackWidth * 0.5f);
        float playerX = Mathf.Lerp(iconOffset, trackWidth - iconOffset, playerProgress);

        if (Mathf.Abs(playerIcon.anchoredPosition.x - playerX) > 0.01f)
        {
            playerIcon.anchoredPosition = new Vector2(playerX, playerIcon.anchoredPosition.y);
        }

        CheckPassedQuests();
    }

    private void EnsureRequiredReferences()
    {
        // Cari Player
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) player = p.transform;
        }

        // Cari StartPoint
        if (startPoint == null)
        {
            GameObject sp = GameObject.Find("StartPoint");
            if (sp == null) sp = GameObject.Find("Starting point");
            if (sp == null) sp = GameObject.Find("Start_Point");
            if (sp == null) sp = GameObject.Find("Start Point");
            if (sp != null) startPoint = sp.transform;
        }

        // Cari EndPoint
        if (endPoint == null)
        {
            GameObject ep = GameObject.Find("EndPoint");
            if (ep == null) ep = GameObject.Find("Ending point");
            if (ep == null) ep = GameObject.Find("End_Point");
            if (ep == null) ep = GameObject.Find("End Point");
            if (ep != null) endPoint = ep.transform;
        }

        // Cari elemen UI track jika belum terhubung
        if (trackBar == null)
        {
            Transform tb = transform.Find("TrackBar");
            if (tb == null) tb = transform.Find("Track");
            if (tb != null) trackBar = tb.GetComponent<RectTransform>();
            if (trackBar == null) trackBar = GetComponent<RectTransform>();
        }

        if (playerIcon == null && trackBar != null)
        {
            Transform pi = trackBar.Find("PlayerIcon");
            if (pi == null) pi = transform.Find("PlayerIcon");
            if (pi != null) playerIcon = pi.GetComponent<RectTransform>();
        }

        if (questContainer == null && trackBar != null)
        {
            Transform qc = trackBar.Find("QuestContainer");
            if (qc == null) qc = trackBar.Find("QuestTrack");
            if (qc == null) qc = transform.Find("QuestContainer");
            if (qc != null) questContainer = qc;
        }
    }

    void SpawnQuestIcons()
    {
        if (startPoint == null || endPoint == null) return;
        if (questIconPrefab == null || questContainer == null) return;
        if (hasSpawnedIcons) return;

        float totalDistance = endPoint.position.x - startPoint.position.x;
        if (Mathf.Abs(totalDistance) <= 0.01f) return;

        float currentTrackWidth = trackBar != null && trackBar.rect.width > 0 ? trackBar.rect.width : trackWidth;
        if (currentTrackWidth <= 0.01f && trackBar != null) currentTrackWidth = trackBar.sizeDelta.x;
        if (currentTrackWidth <= 0.01f) return;

        hasSpawnedIcons = true;

        for (int i = 0; i < questTargets.Count; i++)
        {
            if (questTargets[i] == null) continue;

            float questProgress = (questTargets[i].position.x - startPoint.position.x) / totalDistance;
            questProgress = Mathf.Clamp01(questProgress);

            GameObject iconObj = Instantiate(questIconPrefab, questContainer);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();

            if (i < questTargetIcons.Count && questTargetIcons[i] != null)
            {
                Image iconImage = iconObj.GetComponentInChildren<Image>();
                if (iconImage != null)
                    iconImage.sprite = questTargetIcons[i];
            }

            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);

            float offsetY = (i % 2 == 0) ? 25f : -25f;
            iconRect.anchoredPosition = new Vector2(questProgress * currentTrackWidth, offsetY);

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
        if (player == null || startPoint == null || endPoint == null) return;

        bool isMovingRight = endPoint.position.x >= startPoint.position.x;

        foreach (var marker in questMarkers)
        {
            if (marker.isPassed || marker.iconUI == null) continue;

            bool passed = false;
            if (marker.target == null)
            {
                passed = true;
            }
            else if (isMovingRight)
            {
                passed = player.position.x >= marker.target.position.x;
            }
            else
            {
                passed = player.position.x <= marker.target.position.x;
            }

            if (passed)
            {
                marker.isPassed = true;
                if (marker.iconUI != null)
                {
                    Destroy(marker.iconUI.gameObject);
                }
            }
        }
    }
}
