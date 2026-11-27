using UnityEngine;

public interface IInteractable
{
    void Interact();
    string GetPromptText(); // Pesan bantuan (misal: "Baca Papan", "Ambil Sampah")
}

public static class InteractionInputLock
{
    private static int lastInteractKeyConsumedFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        lastInteractKeyConsumedFrame = -1;
    }

    public static bool IsDialogueActive => Dialogue.IsAnyDialogueActive || Dialogue2.IsAnyDialogueActive;
    public static bool IsSignboardOpen => SignboardInteractable.IsAnySignboardOpen;
    public static bool IsInfographicOpen => InfographicUI.IsAnyInfographicOpen;
    public static bool IsTriviaActive => TriviaQuizUI.IsAnyQuizActive;
    public static bool IsWorldMapOpen => NPCMapTrigger.IsAnyMapOpen;

    public static bool IsAnyModalBlockingWorldInteract =>
        IsDialogueActive || IsTriviaActive || IsInfographicOpen || IsWorldMapOpen;

    public static bool IsAnyOverlayBlockingDialogue =>
        IsSignboardOpen || IsInfographicOpen || IsTriviaActive || IsWorldMapOpen;

    public static void ConsumeInteractKeyThisFrame()
    {
        lastInteractKeyConsumedFrame = Time.frameCount;
    }

    public static bool WasInteractKeyConsumedThisFrame =>
        lastInteractKeyConsumedFrame == Time.frameCount;
}
