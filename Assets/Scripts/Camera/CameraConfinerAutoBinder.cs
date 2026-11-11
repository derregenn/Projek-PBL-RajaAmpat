using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class CameraConfinerAutoBinder : MonoBehaviour
{
    private static CameraConfinerAutoBinder instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("CameraConfinerAutoBinder");
            instance = go.AddComponent<CameraConfinerAutoBinder>();
            DontDestroyOnLoad(go);
        }
        instance.BindAllCamerasInCurrentScene();
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(BindCamerasNextFrame());
    }

    private IEnumerator BindCamerasNextFrame()
    {
        yield return null; // Tunggu satu frame agar seluruh objek scene selesai terinisialisasi
        BindAllCamerasInCurrentScene();
    }

    public void BindAllCamerasInCurrentScene()
    {
        // 1. Cari Player transform secara robust
        GameObject player = FindPlayerGameObject();

        // 2. Snap Main Camera ke posisi Player secara instan agar tidak melayang di langit / (0,0)
        if (Camera.main != null && player != null)
        {
            Vector3 playerTargetPos = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
            Camera.main.transform.position = playerTargetPos;

            CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                // Nonaktifkan blend di frame awal agar langsung snap ke player
                var origBlend = brain.DefaultBlend;
                if (origBlend.Time <= 0f)
                {
                    origBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.0f);
                }
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
                StartCoroutine(RestoreDefaultBlend(brain, origBlend, 0.25f));
            }
        }

        Collider2D boundsShape = FindOrCreateBoundsCollider();
        if (boundsShape == null) return;

        CinemachineCamera[] allCameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            if (cam == null) continue;

            // Jika kamera gameplay utama belum punya tracking target atau prioritas 0, bind ke player
            bool isGameplayCam = cam.name.Equals("CinemachineCamera", System.StringComparison.OrdinalIgnoreCase) ||
                                 (!cam.name.Contains("Dialogue") && !cam.name.Contains("Objective"));

            if (isGameplayCam && player != null)
            {
                if (cam.Target.TrackingTarget == null)
                {
                    cam.Target.TrackingTarget = player.transform;
                }
                if (cam.Priority.Value <= 0)
                {
                    cam.Priority.Value = 10;
                }
                cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
                cam.PreviousStateIsValid = false; // Memaksa Cinemachine 3 untuk langsung snap tanpa damping di frame awal
            }
            else if (cam.name.IndexOf("Dialogue", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Kamera dialog selalu mulai standby dengan prioritas 0
                cam.Priority.Value = 0;
            }

            CinemachineConfiner2D confiner = cam.GetComponent<CinemachineConfiner2D>();
            if (confiner == null)
            {
                confiner = cam.gameObject.AddComponent<CinemachineConfiner2D>();
            }

            if (confiner != null)
            {
                confiner.BoundingShape2D = boundsShape;
                confiner.Damping = 0.5f;
                confiner.SlowingDistance = 0f; // 0 agar tidak mendorong kamera menjauh sebelum batas
                confiner.enabled = true;
                confiner.InvalidateBoundingShapeCache();
            }
        }
    }

    private GameObject FindPlayerGameObject()
    {
        try
        {
            GameObject pTag = GameObject.FindGameObjectWithTag("Player");
            if (pTag != null) return pTag;
        }
        catch { }

        Player playerScript = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (playerScript != null) return playerScript.gameObject;

        GameObject pName = GameObject.Find("Player");
        if (pName != null) return pName;

        GameObject[] allObjs = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allObjs)
        {
            if (go != null && go.name.IndexOf("Player", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                !go.name.Contains("Dialogue") && !go.name.Contains("Canvas") && !go.name.Contains("Camera"))
            {
                return go;
            }
        }

        return null;
    }

    private IEnumerator RestoreDefaultBlend(CinemachineBrain brain, CinemachineBlendDefinition blend, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (brain != null)
        {
            brain.DefaultBlend = blend;
        }
    }

    private Collider2D FindOrCreateBoundsCollider()
    {
        // 1. Cari CameraBounds yang sudah ada
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allObjects)
        {
            if (go == null) continue;
            if (go.name.Equals("CameraBounds", System.StringComparison.OrdinalIgnoreCase))
            {
                Collider2D col = go.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.isTrigger = true;
                    return col;
                }
            }
        }

        // 2. Cari objek dengan tag CameraBounds jika ada
        try
        {
            GameObject taggedObj = GameObject.FindGameObjectWithTag("CameraBounds");
            if (taggedObj != null)
            {
                Collider2D col = taggedObj.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.isTrigger = true;
                    return col;
                }
            }
        }
        catch { }

        // 3. Jika belum ada CameraBounds di scene, buat otomatis dari batas Environment / Background
        Bounds sceneBounds = CalculateSceneEnvironmentBounds();
        if (sceneBounds.size == Vector3.zero)
        {
            sceneBounds = new Bounds(Vector3.zero, new Vector3(120f, 60f, 0f));
        }

        GameObject boundsGo = new GameObject("CameraBounds");
        PolygonCollider2D poly = boundsGo.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;

        Vector2 min = sceneBounds.min;
        Vector2 max = sceneBounds.max;

        float marginX = 8f;
        float marginTop = 8f;
        float marginBottom = 12f;
        Vector2[] points = new Vector2[]
        {
            new Vector2(min.x - marginX, min.y - marginBottom),
            new Vector2(max.x + marginX, min.y - marginBottom),
            new Vector2(max.x + marginX, max.y + marginTop),
            new Vector2(min.x - marginX, max.y + marginTop)
        };

        poly.SetPath(0, points);
        Debug.Log($"[CameraConfinerAutoBinder] Dibuat CameraBounds otomatis untuk scene '{SceneManager.GetActiveScene().name}' dengan ukuran: {sceneBounds.size}");
        return poly;
    }

    private Bounds CalculateSceneEnvironmentBounds()
    {
        Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        GameObject env = GameObject.Find("Environment");
        if (env != null)
        {
            SpriteRenderer[] renderers = env.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                if (sr == null || !sr.enabled) continue;
                if (sr.gameObject.name.Contains("Preview")) continue;

                if (!hasBounds)
                {
                    combinedBounds = sr.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(sr.bounds);
                }
            }

            Collider2D[] colliders = env.GetComponentsInChildren<Collider2D>(true);
            foreach (var col in colliders)
            {
                if (col == null || !col.enabled || col.isTrigger) continue;
                if (!hasBounds)
                {
                    combinedBounds = col.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(col.bounds);
                }
            }
        }

        if (!hasBounds)
        {
            SpriteRenderer[] allRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var sr in allRenderers)
            {
                if (sr == null || sr.CompareTag("Player") || sr.gameObject.name.Contains("Preview")) continue;
                if (!hasBounds)
                {
                    combinedBounds = sr.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(sr.bounds);
                }
            }
        }

        return combinedBounds;
    }
}
