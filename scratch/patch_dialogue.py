import re

with open('Assets/Scripts/Dialogue/Dialogue.cs', 'r', encoding='utf-8') as f:
    text = f.read()

part1_new = """private Canvas FindDialogueCanvas()
    {
        Transform t = transform.Find("DialogueCanvas");
        if (t != null)
        {
            Canvas c = t.GetComponent<Canvas>();
            if (c != null) return c;
        }
        if (DialogueBox != null && DialogueBox.transform.parent != null)
        {
            Canvas c = DialogueBox.transform.parent.GetComponent<Canvas>();
            if (c != null) return c;
        }
        Canvas[] all = GetComponentsInChildren<Canvas>(true);
        foreach (var c in all)
        {
            if (c.gameObject.name.IndexOf("DialogueCanvas", StringComparison.OrdinalIgnoreCase) >= 0)
                return c;
        }
        return all.Length > 0 ? all[0] : null;
    }

    private void EnsureInteractPromptHierarchy()
    {
        EnsureInteractPrompt();

        if (InteractPrompt != null)
        {
            // Pastikan InteractPrompt DI DALAM DialogueCanvas, tapi DI LUAR DialogueBox
            Canvas dialogueCanvas = FindDialogueCanvas();
            Transform canvasTransform = dialogueCanvas != null ? dialogueCanvas.transform : null;

            if (DialogueBox != null)
            {
                if (canvasTransform == null) canvasTransform = DialogueBox.transform.parent;

                // Jika InteractPrompt berada di dalam DialogueBox, pindahkan keluar menjadi sibling
                if (InteractPrompt.transform.IsChildOf(DialogueBox.transform) && InteractPrompt.transform != DialogueBox.transform)
                {
                    InteractPrompt.transform.SetParent(canvasTransform, false);
                }
            }

            if (canvasTransform != null && InteractPrompt.transform.parent != canvasTransform)
            {
                InteractPrompt.transform.SetParent(canvasTransform, false);
            }

            // Hapus Canvas komponen pada InteractPrompt jika ada (agar tidak mendistorsi posisi/layer)
            Canvas promptCanvas = InteractPrompt.GetComponent<Canvas>();
            if (promptCanvas != null)
            {
                if (Application.isPlaying) Destroy(promptCanvas);
                else DestroyImmediate(promptCanvas);
            }

            // Pastikan posisi & scale InteractPrompt sesuai standar Waisai (identik dengan TourGuide)
            RectTransform promptRt = InteractPrompt.GetComponent<RectTransform>();
            if (promptRt != null)
            {
                promptRt.anchorMin = new Vector2(0.5f, 0.5f);
                promptRt.anchorMax = new Vector2(0.5f, 0.5f);
                promptRt.pivot = new Vector2(0.5f, 0.5f);
                promptRt.sizeDelta = new Vector2(200f, 50f);
                promptRt.localScale = Vector3.one;
                promptRt.anchoredPosition = new Vector2(0f, -73.5f);
                promptRt.localPosition = new Vector3(0f, -73.5f, 0f);
            }

            TMP_Text promptText = InteractPrompt.GetComponent<TMP_Text>();
            if (promptText != null)
            {
                promptText.alignment = TextAlignmentOptions.Center;
                promptText.enableWordWrapping = false;
            }
        }
    }"""

part2_new = """private void EnsureWaisaiDialogueStandard()
    {
        // 1. Standar Canvas (Normalisasi World Scale & Offset agar identik di SEMUA NPC di SEMUA SCENE)
        // Standar golden Waisai: NPC scale = 0.16f, Canvas localScale = 0.051125f -> worldScale = 0.00818f
        // Offset Y golden Waisai: 11.06f * 0.16f = 1.77f world units di atas posisi pivot NPC
        Canvas dialogueCanvas = FindDialogueCanvas();
        if (dialogueCanvas != null)
        {
            RectTransform cRt = dialogueCanvas.GetComponent<RectTransform>();
            if (cRt != null)
            {
                cRt.sizeDelta = new Vector2(1000f, 600f);

                float pScaleX = Mathf.Abs(transform.lossyScale.x);
                float pScaleY = Mathf.Abs(transform.lossyScale.y);

                const float targetWorldScale = 0.00818f;
                const float targetWorldOffsetY = 1.77f;

                if (pScaleX > 0.0001f && pScaleY > 0.0001f)
                {
                    float sX = targetWorldScale / pScaleX;
                    float sY = targetWorldScale / pScaleY;
                    cRt.localScale = new Vector3(sX, sY, 1f);
                    cRt.anchoredPosition = new Vector2(0f, targetWorldOffsetY / pScaleY);
                }
                else
                {
                    cRt.localScale = new Vector3(0.051125f, 0.051125f, 1f);
                    cRt.anchoredPosition = new Vector2(0f, 11.06f);
                }
            }
        }

        // 2. Standar DialogueBox (300 x 102.69 di SEMUA NPC di SEMUA SCENE, identik dengan Pulau Waisai)
        if (DialogueBox != null)
        {
            RectTransform boxRt = DialogueBox.GetComponent<RectTransform>();
            if (boxRt != null)
            {
                boxRt.anchorMin = new Vector2(0.5f, 0.5f);
                boxRt.anchorMax = new Vector2(0.5f, 0.5f);
                boxRt.pivot = new Vector2(0.5f, 0.5f);
                boxRt.sizeDelta = new Vector2(300f, 102.69f);
                boxRt.anchoredPosition = new Vector2(0f, -47.4f);
                boxRt.localScale = Vector3.one;
            }

            if (DialogueText != null)
            {
                RectTransform textRt = DialogueText.GetComponent<RectTransform>();
                if (textRt != null)
                {
                    textRt.sizeDelta = new Vector2(280f, textRt.sizeDelta.y);
                }
                DialogueText.fontSize = 18f;
                DialogueText.fontSizeBase = 18f;
                DialogueText.enableWordWrapping = true;
                DialogueText.alignment = TextAlignmentOptions.Center;
                DialogueText.fontStyle = FontStyles.Normal;
            }

            // Pastikan NextPrompt terhubung jika ada child button / NextButton
            if (NextPrompt == null)
            {
                Transform btnNext = DialogueBox.transform.Find("NextButton");
                if (btnNext == null) btnNext = DialogueBox.transform.Find("Btn_Next");
                if (btnNext == null) btnNext = DialogueBox.transform.Find("NextPrompt");
                if (btnNext != null)
                {
                    NextPrompt = btnNext.gameObject;
                }
            }
        }

        // 3. Standar Audio Typing
        if (typingAudioSource == null)
        {
            typingAudioSource = GetComponent<AudioSource>();
            if (typingAudioSource == null)
            {
                typingAudioSource = gameObject.AddComponent<AudioSource>();
                typingAudioSource.playOnAwake = false;
                typingAudioSource.spatialBlend = 0f;
            }
        }

        if (typingAudioClip == null)
        {
            if (typingAudioSource != null && typingAudioSource.clip != null)
            {
                typingAudioClip = typingAudioSource.clip;
            }
            else
            {
                typingAudioClip = Resources.Load<AudioClip>("Dialog_TypingSlow");
            }
        }

        TypeSpeed = 0.04f;
    }

    private void EnsureDialogueCanvasSorting()
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.WorldSpace)
            {
                c.overrideSorting = true;
                c.sortingLayerName = "Foreground";
                c.sortingOrder = 100;
                if (c.worldCamera == null)
                {
                    c.worldCamera = Camera.main;
                }
            }
        }

        if (InteractPrompt != null)
        {
            Canvas promptCanvas = InteractPrompt.GetComponent<Canvas>();
            if (promptCanvas != null)
            {
                if (Application.isPlaying) Destroy(promptCanvas);
                else DestroyImmediate(promptCanvas);
            }
        }
    }"""

text = text[:3362] + part1_new + text[5165:5714] + part2_new + text[10847:]

# Cutscene check on triggers
text = text.replace("""    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))""", """    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (CheckIsCutscene()) return;
        if (collision.CompareTag("Player"))""")

text = text.replace("""    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))""", """    private void OnTriggerStay2D(Collider2D collision)
    {
        if (CheckIsCutscene()) return;
        if (collision.CompareTag("Player"))""")

with open('Assets/Scripts/Dialogue/Dialogue.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print('Updated Dialogue.cs successfully')

