using System.Collections.Generic;
using UnityEngine;

public class BoatBobbing : MonoBehaviour
{
    [Header("Bobbing Settings")]
    [SerializeField] private float floatSpeed = 2f;    // Kecepatan membal
    [SerializeField] private float floatAmount = 0.1f;  // Ketinggian membal naik-turun
    [SerializeField] private float tiltAmount = 1.5f;   // Kemiringan rotasi kapal

    [Header("Passenger Anchoring")]
    [Tooltip("Otomatis kunci posisi dan nonaktifkan kontrol Player agar tetap stay di kapal")]
    [SerializeField] private bool autoAnchorPlayer = true;
    [Tooltip("Otomatis kunci posisi NPC Tour Guide jika ada di kapal")]
    [SerializeField] private bool autoAnchorNPC = true;

    private Vector3 startPos;

    private class AnchoredPassenger
    {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Rigidbody2D rb;
        public RigidbodyType2D originalBodyType;
        public Player playerComponent;
    }

    private readonly List<AnchoredPassenger> anchoredPassengers = new List<AnchoredPassenger>();
    private bool initialized = false;

    private void Awake()
    {
        startPos = transform.position;
    }

    private void Start()
    {
        InitializeAnchoring();
    }

    public void InitializeAnchoring()
    {
        if (initialized) return;
        initialized = true;

        if (autoAnchorPlayer)
        {
            AnchorPlayer();
        }

        if (autoAnchorNPC)
        {
            AnchorNPC();
        }
    }

    private void AnchorPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            Player p = FindFirstObjectByType<Player>();
            if (p != null) playerObj = p.gameObject;
        }

        if (playerObj != null)
        {
            // 1. Kunci input dan status movement di script Player
            Player playerComp = playerObj.GetComponent<Player>();
            if (playerComp != null)
            {
                playerComp.isMovementLocked = true;
                playerComp.enabled = false;
            }

            // 2. Hentikan suara jalan jika sedang menyala
            AudioSource audioSrc = playerObj.GetComponent<AudioSource>();
            if (audioSrc != null && audioSrc.isPlaying)
            {
                audioSrc.Stop();
            }

            // 3. Pastikan animasi diset ke Player_Idle
            Animator anim = playerObj.GetComponent<Animator>();
            if (anim != null)
            {
                anim.Play("Player_Idle");
            }

            // 4. Kunci rigidbody agar tidak kena gravitasi atau sliding
            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            RigidbodyType2D origType = RigidbodyType2D.Dynamic;
            if (rb != null)
            {
                origType = rb.bodyType;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            // 5. Parent ke kapal (pertahankan world position saat ini)
            playerObj.transform.SetParent(this.transform, true);

            anchoredPassengers.Add(new AnchoredPassenger
            {
                transform = playerObj.transform,
                localPosition = playerObj.transform.localPosition,
                localRotation = playerObj.transform.localRotation,
                rb = rb,
                originalBodyType = origType,
                playerComponent = playerComp
            });

            Debug.Log("[BoatBobbing] Player berhasil di-anchor ke perahu.");
        }
    }

    private void AnchorNPC()
    {
        NPCTourGuide npcGuide = FindFirstObjectByType<NPCTourGuide>();
        GameObject npcObj = npcGuide != null ? npcGuide.gameObject : GameObject.Find("NPCTourGuide");

        if (npcObj != null && npcObj.transform.parent != this.transform)
        {
            Rigidbody2D rb = npcObj.GetComponent<Rigidbody2D>();
            RigidbodyType2D origType = RigidbodyType2D.Dynamic;
            if (rb != null)
            {
                origType = rb.bodyType;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            npcObj.transform.SetParent(this.transform, true);

            anchoredPassengers.Add(new AnchoredPassenger
            {
                transform = npcObj.transform,
                localPosition = npcObj.transform.localPosition,
                localRotation = npcObj.transform.localRotation,
                rb = rb,
                originalBodyType = origType,
                playerComponent = null
            });

            Debug.Log("[BoatBobbing] NPCTourGuide berhasil di-anchor ke perahu.");
        }
    }

    private void Update()
    {
        // Gerakan naik-turun menggunakan Gelombang Sinus
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Sedikit rotasi membal
        float tilt = Mathf.Sin(Time.time * floatSpeed * 0.8f) * tiltAmount;
        transform.rotation = Quaternion.Euler(0, 0, tilt);
    }

    private void LateUpdate()
    {
        // Pastikan setiap penumpang tetap terkunci presisi di koordinat dek lokalnya
        for (int i = 0; i < anchoredPassengers.Count; i++)
        {
            var p = anchoredPassengers[i];
            if (p != null && p.transform != null)
            {
                p.transform.localPosition = p.localPosition;
                p.transform.localRotation = p.localRotation;

                if (p.playerComponent != null)
                {
                    p.playerComponent.isMovementLocked = true;
                    if (p.playerComponent.enabled)
                    {
                        p.playerComponent.enabled = false;
                    }
                }
            }
        }
    }

    private void OnDisable()
    {
        if (gameObject.activeInHierarchy)
        {
            ReleasePassengers();
        }
    }

    private void OnDestroy()
    {
        if (gameObject.activeInHierarchy)
        {
            ReleasePassengers();
        }
    }

    public void ReleasePassengers()
    {
        foreach (var p in anchoredPassengers)
        {
            if (p != null && p.transform != null && p.transform.parent == this.transform)
            {
                p.transform.SetParent(null, true);
                if (p.rb != null)
                {
                    p.rb.bodyType = p.originalBodyType;
                }
                if (p.playerComponent != null)
                {
                    p.playerComponent.isMovementLocked = false;
                    p.playerComponent.enabled = true;
                }
            }
        }
        anchoredPassengers.Clear();
    }
}
