using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("Scroll Settings")]
    [Tooltip("Kecepatan gerak background ke kiri")]
    [SerializeField] private float scrollSpeed = 1f;

    [Header("Manual Width (Override Lebar)")]
    [Tooltip("Isi lebar gambar jika ingin override manual (0 = otomatis deteksi dari SpriteRenderer)")]
    [SerializeField] private float manualWidth = 0f;

    [Header("Camera Loop")]
    [Tooltip("Kamera yang batasnya digunakan untuk mendeteksi loop")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Pindahkan objek ke sisi kanan kamera setelah seluruhnya melewati batas kiri")]
    [SerializeField] private bool loopAtCameraBoundary = true;

    [Header("Seamless Dual-Tile Tiling")]
    [Tooltip("Aktifkan untuk background layar lebar (seperti laut & langit) agar mengulang secara seamless tanpa ada jeda kosong")]
    [SerializeField] private bool isSeamlessLoop = false;
    [Tooltip("Offset overlap antar-tile untuk mencegah garis celah subpixel")]
    [SerializeField] private float seamOverlap = 0.02f;

    [Header("Flock / Single Object Settings")]
    [Tooltip("Jarak aman di luar layar kamera sebelum objek berpindah")]
    [SerializeField] private float extraPadding = 0.5f;
    [Tooltip("Variasi ketinggian Y acak saat me-loop (khusus burung/siluet)")]
    [SerializeField] private float randomYRange = 0.3f;

    private float lengthX;
    private float localMinX;
    private float localMaxX;
    private Vector3 startPosition;
    private float currentDistance;

    // Dual-tile clone untuk seamless background
    private GameObject cloneTile;
    private bool isCloneInstance = false;
    private bool initialized = false;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (isCloneInstance) return;

        if (initialized && isSeamlessLoop && cloneTile == null)
        {
            SetupSeamlessClone();
        }
        else if (cloneTile != null)
        {
            cloneTile.SetActive(true);
        }
    }

    private void OnDisable()
    {
        if (cloneTile != null)
        {
            cloneTile.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (cloneTile != null)
        {
            Destroy(cloneTile);
        }
    }

    public void Initialize()
    {
        if (initialized || isCloneInstance) return;
        initialized = true;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // Auto-detect seamless loop jika nama objek background atau memiliki sprite sangat lebar
        DetectSeamlessMode();

        // Hitung batas visual akurat dari sprite
        CalculateBounds(out localMinX, out localMaxX, out lengthX);

        if (lengthX <= 0f)
        {
            Debug.LogWarning($"[ParallaxBackground] Lebar pada '{gameObject.name}' tidak terdeteksi. Gunakan Manual Width jika perlu.", this);
            return;
        }

        if (isSeamlessLoop)
        {
            SetupSeamlessClone();
        }
    }

    private void DetectSeamlessMode()
    {
        if (isSeamlessLoop) return;

        string objName = gameObject.name.ToLower();
        if (objName.StartsWith("bg") || objName.StartsWith("background") || objName.Contains("laut") || objName.Contains("sky"))
        {
            isSeamlessLoop = true;
        }
    }

    private void CalculateBounds(out float localMin, out float localMax, out float width)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        if (renderers != null && renderers.Length > 0)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float maxSingleWidth = 0f;
            bool foundAny = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                var sr = renderers[i];
                if (sr == null || !sr.enabled || !sr.gameObject.activeInHierarchy) continue;

                Bounds b = sr.bounds;
                float rMin = b.min.x - transform.position.x;
                float rMax = b.max.x - transform.position.x;

                if (b.size.x > maxSingleWidth)
                {
                    maxSingleWidth = b.size.x;
                }

                if (rMin < minX) minX = rMin;
                if (rMax > maxX) maxX = rMax;
                foundAny = true;
            }

            if (foundAny)
            {
                if (isSeamlessLoop)
                {
                    // Untuk seamless tiling background (langit/laut), utamakan lebar sprite background utama
                    width = maxSingleWidth;
                    if (width <= 0f && manualWidth > 0f)
                    {
                        width = manualWidth;
                    }
                    localMin = -width * 0.5f;
                    localMax = width * 0.5f;
                }
                else
                {
                    // Untuk objek kelompok/burung, gunakan rentang visual total seluruh anak
                    width = maxX - minX;
                    localMin = minX;
                    localMax = maxX;
                }
                return;
            }
        }

        // Fallback jika tidak ada SpriteRenderer
        if (manualWidth > 0f)
        {
            width = manualWidth;
        }
        else if (TryGetComponent<RectTransform>(out RectTransform rectTransform))
        {
            width = rectTransform.rect.width * transform.lossyScale.x;
        }
        else
        {
            width = 2f;
        }

        localMin = -width * 0.5f;
        localMax = width * 0.5f;
    }

    private void SetupSeamlessClone()
    {
        if (cloneTile != null || lengthX <= 0f) return;

        cloneTile = Instantiate(gameObject, transform.parent);
        cloneTile.name = "SeamlessClone_" + gameObject.name;

        // Nonaktifkan dan hapus skrip ParallaxBackground pada clone agar tidak duplikasi update loop
        ParallaxBackground cloneComp = cloneTile.GetComponent<ParallaxBackground>();
        if (cloneComp != null)
        {
            cloneComp.isCloneInstance = true;
            cloneComp.enabled = false;
            Destroy(cloneComp);
        }

        // Posisikan clone tepat di sisi kanan tile utama (dengan sedikit overlap untuk menghindari celah 1-piksel)
        cloneTile.transform.position = transform.position + new Vector3(lengthX - seamOverlap, 0f, 0f);
        cloneTile.transform.rotation = transform.rotation;
        cloneTile.transform.localScale = transform.localScale;
        cloneTile.SetActive(gameObject.activeInHierarchy);
    }

    private void LateUpdate()
    {
        if (isCloneInstance || lengthX <= 0f) return;

        float dt = Mathf.Min(Time.deltaTime, 0.1f);

        if (isSeamlessLoop && cloneTile != null)
        {
            UpdateSeamlessTiling(dt);
        }
        else if (loopAtCameraBoundary)
        {
            UpdateSingleObjectBoundaryLoop(dt);
        }
        else
        {
            UpdateLegacyRepeat(dt);
        }
    }

    private void UpdateSeamlessTiling(float dt)
    {
        float delta = scrollSpeed * dt;
        transform.position += Vector3.left * delta;
        if (cloneTile != null)
        {
            cloneTile.transform.position += Vector3.left * delta;
        }

        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || cloneTile == null) return;

        float depth = targetCamera.WorldToScreenPoint(transform.position).z;
        float cameraLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth)).x;

        // 1. Cek apakah Tile Utama sudah sepenuhnya melewati batas kiri kamera
        float myRightEdge = transform.position.x + localMaxX;
        if (myRightEdge < cameraLeft)
        {
            // Pindahkan Tile Utama ke sebelah kanan Clone Tile
            float cloneRightEdge = cloneTile.transform.position.x + localMaxX;
            float newX = (cloneRightEdge - seamOverlap) - localMinX;
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        }

        // 2. Cek apakah Clone Tile sudah sepenuhnya melewati batas kiri kamera
        float cloneRight = cloneTile.transform.position.x + localMaxX;
        if (cloneRight < cameraLeft)
        {
            // Pindahkan Clone Tile ke sebelah kanan Tile Utama
            float myNewRightEdge = transform.position.x + localMaxX;
            float newX = (myNewRightEdge - seamOverlap) - localMinX;
            cloneTile.transform.position = new Vector3(newX, cloneTile.transform.position.y, cloneTile.transform.position.z);
        }
    }

    private void UpdateSingleObjectBoundaryLoop(float dt)
    {
        float delta = scrollSpeed * dt;
        transform.position += Vector3.left * delta;

        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        float depth = targetCamera.WorldToScreenPoint(transform.position).z;
        float cameraLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth)).x;
        float cameraRight = targetCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth)).x;

        // Objek HANYA di-loop jika bagian terkanannya (localMaxX) sudah sepenuhnya melewati batas kiri kamera
        float currentRightEdge = transform.position.x + localMaxX;
        if (currentRightEdge < cameraLeft - extraPadding)
        {
            // Posisikan bagian terkiri objek (localMinX) berada di sisi kanan kamera
            float newX = (cameraRight + extraPadding) - localMinX;

            // Berikan variasi ketinggian Y agar tidak monoton
            float newY = startPosition.y;
            if (randomYRange > 0f)
            {
                newY += Random.Range(-randomYRange, randomYRange);
            }

            transform.position = new Vector3(newX, newY, transform.position.z);
        }
    }

    private void UpdateLegacyRepeat(float dt)
    {
        currentDistance += scrollSpeed * dt;
        float offset = Mathf.Repeat(currentDistance, lengthX);
        transform.position = startPosition + (Vector3.left * offset);
    }
}
