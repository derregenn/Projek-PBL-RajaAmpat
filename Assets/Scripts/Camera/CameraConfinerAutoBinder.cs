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
        Collider2D boundsShape = FindOrCreateBoundsCollider();
        if (boundsShape == null) return;

        CinemachineCamera[] allCameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            if (cam == null) continue;

            CinemachineConfiner2D confiner = cam.GetComponent<CinemachineConfiner2D>();
            if (confiner == null)
            {
                confiner = cam.gameObject.AddComponent<CinemachineConfiner2D>();
            }

            if (confiner != null)
            {
                confiner.BoundingShape2D = boundsShape;
                confiner.Damping = 0.5f;
                confiner.enabled = true;
                confiner.InvalidateBoundingShapeCache();
            }
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
            // Fallback default jika scene sangat kecil / belum ada environment
            sceneBounds = new Bounds(Vector3.zero, new Vector3(120f, 60f, 0f));
        }

        GameObject boundsGo = new GameObject("CameraBounds");
        PolygonCollider2D poly = boundsGo.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;

        Vector2 min = sceneBounds.min;
        Vector2 max = sceneBounds.max;

        // Beri sedikit margin agar kamera tidak terjepit di tepi absolut
        float margin = 0.5f;
        Vector2[] points = new Vector2[]
        {
            new Vector2(min.x - margin, min.y - margin),
            new Vector2(max.x + margin, min.y - margin),
            new Vector2(max.x + margin, max.y + margin),
            new Vector2(min.x - margin, max.y + margin)
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
                // Lewatkan preview setup yang tersembunyi
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

        // Jika Environment tidak memiliki sprite, hitung dari seluruh SpriteRenderer di scene
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

