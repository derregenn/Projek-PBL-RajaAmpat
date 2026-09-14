using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class DialogueCameraZoom : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera gameplayCamera;
    [SerializeField] private CinemachineCamera dialogueCamera;

    [Header("Priority")]
    [SerializeField] private int gameplayPriority = 10;
    [SerializeField] private int dialoguePriority = 20;

    [Header("Zoom")]
    [SerializeField] private float normalSize = 8.28f;
    [SerializeField] private float zoomSize = 4.7f;
    [SerializeField] private float zoomDuration = 0.5f;
    private Coroutine zoomCoroutine;

    private void Start()
    {
        // Kamera gameplay aktif saat game dimulai
        gameplayCamera.Priority.Value = gameplayPriority;
        dialogueCamera.Priority.Value = 0;

        // Set ukuran awal kamera dialogue
        var lens = dialogueCamera.Lens;
        lens.OrthographicSize = zoomSize;
        dialogueCamera.Lens = lens;
    }

    public void ZoomIn()
    {
        if (dialogueCamera == null || gameplayCamera == null)
            return;

        // Aktifkan kamera dialogue
        dialogueCamera.Priority.Value = dialoguePriority;

        if (zoomCoroutine != null)
            StopCoroutine(zoomCoroutine);

        zoomCoroutine = StartCoroutine(ChangeZoom(zoomSize));
    }

    public void ZoomOut()
    {
        if (dialogueCamera == null || gameplayCamera == null)
            return;

        if (zoomCoroutine != null)
            StopCoroutine(zoomCoroutine);

        zoomCoroutine = StartCoroutine(ZoomOutCoroutine());
    }

    private IEnumerator ChangeZoom(float targetSize)
    {
        float startSize = dialogueCamera.Lens.OrthographicSize;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;

            float newSize = Mathf.Lerp(
                startSize,
                targetSize,
                elapsed / zoomDuration
            );

            var lens = dialogueCamera.Lens;
            lens.OrthographicSize = newSize;
            dialogueCamera.Lens = lens;

            yield return null;
        }

        var finalLens = dialogueCamera.Lens;
        finalLens.OrthographicSize = targetSize;
        dialogueCamera.Lens = finalLens;
    }

    private IEnumerator ZoomOutCoroutine()
    {
        float startSize = dialogueCamera.Lens.OrthographicSize;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;

            float newSize = Mathf.Lerp(
                startSize,
                normalSize,
                elapsed / zoomDuration
            );

            var lens = dialogueCamera.Lens;
            lens.OrthographicSize = newSize;
            dialogueCamera.Lens = lens;

            yield return null;
        }

        var finalLens = dialogueCamera.Lens;
        finalLens.OrthographicSize = normalSize;
        dialogueCamera.Lens = finalLens;

        // Kembalikan kamera gameplay
        dialogueCamera.Priority.Value = 0;
        gameplayCamera.Priority.Value = gameplayPriority;
    }
}