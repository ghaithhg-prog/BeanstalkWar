using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

public class CharacterSelectionUI : MonoBehaviour
{
    public static bool IsSelectionOpen { get; private set; } = false;

    [Header("References")]
    public CharacterDatabase database;
    public CharacterCardUI cardPrefab;
    public Transform cardContainer;
    public TextMeshProUGUI selectedNameText;
    public TextMeshProUGUI selectedDescText;
    public TextMeshProUGUI selectedStatsText;
    public Image selectedIcon;
    public Button confirmButton;

    [Header("State")]
    private int selectedCharacterID = 0; // Default to Archer (Erika Archer, ID: 0)
    private bool isShowing = false;
    private int currentSkinIndex = 0;
    private bool skinEquipped = false;
    private CharacterSkin[] availableSkins;
    private CharacterSkin currentSkin;

    // Grid & UI state
    private Vector2 gridScrollPosition;
    private Vector2 detailsScrollPosition;
    private int lastDetailsCharID = -999;
    private string skinNotification = "";
    private float skinNotificationTimer = 0f;
    private int hoveredCharacterID = -1;
    private float hoverAnimTime = 0f;
    private float selectionAnimTime = 0f;
    private float headerPulse = 0f;
    private float[] cardAnimTimers;

    // Cached textures
    private Texture2D gradientTex;
    private Texture2D glowTex;
    private Texture2D cardBgTex;

    // 3D Preview Studio
    private GameObject previewStudioRoot;
    private Camera previewCamera;
    private RenderTexture previewRenderTexture;
    private Transform turntable;
    private GameObject currentPreviewModel;
    private int currentPreviewID = -999;
    private const float STUDIO_X = 800f;
    private const float STUDIO_Y = -400f;
    private const float STUDIO_Z = 800f;

    // Layout constants
    private const int GRID_COLUMNS = 5;
    private const float CARD_ASPECT = 1.35f;
    private const float CARD_SPACING = 6f;
    private const float GRID_PADDING = 12f;

    // Role colors
    private static readonly Dictionary<CharacterData.CharacterRole, Color> RoleColors = new Dictionary<CharacterData.CharacterRole, Color>
    {
        { CharacterData.CharacterRole.Tank, new Color(0.25f, 0.5f, 0.85f) },
        { CharacterData.CharacterRole.Damage, new Color(0.9f, 0.25f, 0.2f) },
        { CharacterData.CharacterRole.Support, new Color(0.2f, 0.8f, 0.4f) },
        { CharacterData.CharacterRole.Control, new Color(0.65f, 0.3f, 0.85f) },
        { CharacterData.CharacterRole.Specialist, new Color(0.9f, 0.6f, 0.15f) }
    };

    private static readonly Dictionary<CharacterData.CharacterRole, string> RoleNames = new Dictionary<CharacterData.CharacterRole, string>
    {
        { CharacterData.CharacterRole.Tank, "TANK" },
        { CharacterData.CharacterRole.Damage, "DAMAGE" },
        { CharacterData.CharacterRole.Support, "SUPPORT" },
        { CharacterData.CharacterRole.Control, "CONTROL" },
        { CharacterData.CharacterRole.Specialist, "SPECIALIST" }
    };

    public static string GetRoleName(CharacterData.CharacterRole role)
    {
        switch (role)
        {
            case CharacterData.CharacterRole.Tank:
                return LanguageSelectionUI.Localize("TANK", "مدرع", "ТАНК", "TANQUE", "TANK");
            case CharacterData.CharacterRole.Damage:
                return LanguageSelectionUI.Localize("DAMAGE", "هجوم", "УРОН", "DAÑO", "DÉGÂTS");
            case CharacterData.CharacterRole.Support:
                return LanguageSelectionUI.Localize("SUPPORT", "دعم", "ПОДДЕРЖКА", "APOYO", "SOUTIEN");
            case CharacterData.CharacterRole.Control:
                return LanguageSelectionUI.Localize("CONTROL", "تحكم", "КОНТРОЛЬ", "CONTROL", "CONTRÔLE");
            case CharacterData.CharacterRole.Specialist:
                return LanguageSelectionUI.Localize("SPECIALIST", "مختص", "СПЕЦИАЛИСТ", "ESPECIALISTA", "SPÉCIALISTE");
            default:
                return LanguageSelectionUI.Localize("HERO", "بطل", "ГЕРОЙ", "HÉROE", "HÉROS");
        }
    }

    void Start()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSelection);

        CreateCachedTextures();
        SetupPreviewStudio();
    }

    void OnDestroy()
    {
        if (previewRenderTexture != null)
        {
            previewRenderTexture.Release();
            Destroy(previewRenderTexture);
        }
        if (previewStudioRoot != null)
        {
            Destroy(previewStudioRoot);
        }
        if (gradientTex != null) Destroy(gradientTex);
        if (glowTex != null) Destroy(glowTex);
        if (cardBgTex != null) Destroy(cardBgTex);
        IsSelectionOpen = false;
    }

    void CreateCachedTextures()
    {
        // Vertical gradient texture
        gradientTex = new Texture2D(1, 64);
        for (int y = 0; y < 64; y++)
        {
            float t = y / 63f;
            gradientTex.SetPixel(0, y, new Color(0, 0, 0, Mathf.Lerp(0.95f, 0.4f, t)));
        }
        gradientTex.Apply();
        gradientTex.wrapMode = TextureWrapMode.Clamp;

        // Glow texture
        glowTex = new Texture2D(32, 32);
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
            {
                float dx = (x - 15.5f) / 16f;
                float dy = (y - 15.5f) / 16f;
                float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                glowTex.SetPixel(x, y, new Color(1, 1, 1, d * d));
            }
        glowTex.Apply();

        // Card background
        cardBgTex = new Texture2D(1, 32);
        for (int y = 0; y < 32; y++)
        {
            float t = y / 31f;
            float v = Mathf.Lerp(0.06f, 0.12f, t);
            cardBgTex.SetPixel(0, y, new Color(v * 0.6f, v, v * 0.7f, 1f));
        }
        cardBgTex.Apply();
        cardBgTex.wrapMode = TextureWrapMode.Clamp;
    }

    // =========================================================================
    // 3D PREVIEW STUDIO (TURNTABLE + CAMERA)
    // =========================================================================
    void SetupPreviewStudio()
    {
        if (previewStudioRoot != null) return;

        previewStudioRoot = new GameObject("ChampionPreviewStudio");
        previewStudioRoot.transform.position = new Vector3(STUDIO_X, STUDIO_Y, STUDIO_Z);

        // High-Resolution Render Texture
        previewRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
        previewRenderTexture.Create();

        // Camera
        GameObject camObj = new GameObject("PreviewCamera");
        camObj.transform.SetParent(previewStudioRoot.transform, false);
        camObj.transform.localPosition = new Vector3(0f, 1.08f, -2.55f);
        camObj.transform.localRotation = Quaternion.Euler(7f, 0f, 0f);

        previewCamera = camObj.AddComponent<Camera>();
        previewCamera.targetTexture = previewRenderTexture;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.03f, 0.06f, 0.04f, 1f);
        previewCamera.fieldOfView = 35f;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 20f;
        previewCamera.enabled = true;

        // Key Studio Light
        GameObject lightObj = new GameObject("StudioKeyLight");
        lightObj.transform.SetParent(previewStudioRoot.transform, false);
        lightObj.transform.localPosition = new Vector3(1.5f, 3.5f, -2f);
        lightObj.transform.localRotation = Quaternion.Euler(45f, -30f, 0f);
        Light keyLight = lightObj.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(1f, 0.96f, 0.88f);
        keyLight.intensity = 1.35f;

        // Rim Light (Emerald forest glow)
        GameObject rimObj = new GameObject("StudioRimLight");
        rimObj.transform.SetParent(previewStudioRoot.transform, false);
        rimObj.transform.localPosition = new Vector3(-2f, 2f, 2f);
        rimObj.transform.localRotation = Quaternion.Euler(30f, 140f, 0f);
        Light rimLight = rimObj.AddComponent<Light>();
        rimLight.type = LightType.Directional;
        rimLight.color = new Color(0.2f, 0.85f, 0.4f);
        rimLight.intensity = 0.8f;

        // Turntable Root
        GameObject ttObj = new GameObject("Turntable");
        ttObj.transform.SetParent(previewStudioRoot.transform, false);
        ttObj.transform.localPosition = Vector3.zero;
        turntable = ttObj.transform;

        // Pedestal dais
        GameObject dais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dais.name = "Dais";
        dais.transform.SetParent(turntable, false);
        dais.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        dais.transform.localScale = new Vector3(1.6f, 0.05f, 1.6f);
        Collider dCol = dais.GetComponent<Collider>();
        if (dCol != null) Destroy(dCol);
        Renderer dRend = dais.GetComponent<Renderer>();
        if (dRend != null)
        {
            dRend.material.color = new Color(0.12f, 0.18f, 0.14f);
        }

        UpdatePreviewStudioModel(selectedCharacterID >= 0 ? selectedCharacterID : 0);
    }

    void UpdatePreviewStudioModel(int characterID)
    {
        if (turntable == null) return;
        if (characterID == currentPreviewID && currentPreviewModel != null) return;

        currentPreviewID = characterID;

        if (currentPreviewModel != null)
        {
            Destroy(currentPreviewModel);
        }

        CharacterData ch = (database != null) ? database.GetCharacter(characterID) : null;
        string lowName = (ch != null && !string.IsNullOrEmpty(ch.characterName)) ? ch.characterName.ToLower() : "";
        bool isArcherModel = (characterID == 0) || lowName.Contains("archer") || lowName.Contains("ranger") || lowName.Contains("windwalker");

        GameObject prefab = null;
        if (isArcherModel)
        {
            prefab = Resources.Load<GameObject>("Models/Erika_Archer");
            if (prefab == null) prefab = Resources.Load<GameObject>("Models/Akai");
        }
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Models/Warrior_Medieval");
        }

        if (prefab != null)
        {
            currentPreviewModel = Instantiate(prefab, turntable);
            currentPreviewModel.name = "Preview_Champion_" + characterID;
            currentPreviewModel.transform.localPosition = new Vector3(0f, 0f, 0f);
            currentPreviewModel.transform.localRotation = Quaternion.identity;
            currentPreviewModel.transform.localScale = Vector3.one * 1.05f;

            foreach (Collider c in currentPreviewModel.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (Rigidbody r in currentPreviewModel.GetComponentsInChildren<Rigidbody>()) Destroy(r);
            foreach (Animator a in currentPreviewModel.GetComponentsInChildren<Animator>())
            {
                a.applyRootMotion = false;
                if (isArcherModel)
                {
                    a.enabled = true;
                    a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
                else
                {
                    a.enabled = false;
                }
            }

            if (isArcherModel)
            {
                PlayerMovement.ApplyErikaArcherMaterials(currentPreviewModel, PlayerMovement.TeamType.Protector);

                // Attach 3D Bow to LeftHand if not already present on the mesh
                Transform bowTr = null;
                foreach (Transform t in currentPreviewModel.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.ToLower().Contains("bow")) { bowTr = t; break; }
                }
                if (bowTr == null)
                {
                    Transform leftHand = null;
                    foreach (Transform t in currentPreviewModel.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.ToLower().Contains("lefthand")) { leftHand = t; break; }
                    }
                    if (leftHand != null)
                    {
                        GameObject bowPrefab = Resources.Load<GameObject>("Models/Bow_3D");
                        if (bowPrefab != null)
                        {
                            GameObject bowInstance = Instantiate(bowPrefab, leftHand);
                            bowInstance.name = "Bow_3D";
                            bowInstance.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                            bowInstance.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                            bowInstance.transform.localScale = Vector3.one * (leftHand.lossyScale.x < 0.1f ? 100f : 1f);
                            PlayerMovement.ApplyErikaArcherMaterials(bowInstance, PlayerMovement.TeamType.Protector);
                        }
                    }
                }
            }
            else if (ch != null)
            {
                PlayerMovement.StyleModelForCharacter(currentPreviewModel, ch, currentSkin);
            }

            if (currentPreviewModel.GetComponent<CharacterVisualAnimator>() == null)
            {
                currentPreviewModel.AddComponent<CharacterVisualAnimator>();
            }
        }

        // Also update the player character standing outside in the 3D scene immediately!
        PlayerMovement localPlayer = GetLocalPlayer();
        if (localPlayer != null)
        {
            localPlayer.ApplyCharacter(characterID);
        }
    }

    public void Show()
    {
        if (MatchEndStatsUI.isAutoRestartingMatch) return;
        if (GameStateManager.Instance != null && GameStateManager.Instance.currentState != GameStateManager.GameState.WaitingForPlayers) return;

        isShowing = true;
        IsSelectionOpen = true;

        if (selectedCharacterID < 0)
            selectedCharacterID = 0; // Default to Archer (Erika Archer) so she is immediately visible!

        hoveredCharacterID = -1;
        currentSkinIndex = 0;
        skinEquipped = false;
        hoverAnimTime = 0f;
        selectionAnimTime = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (database != null && database.characters != null)
        {
            cardAnimTimers = new float[database.characters.Length];
            for (int i = 0; i < cardAnimTimers.Length; i++)
                cardAnimTimers[i] = 0f;
        }

        if (previewStudioRoot == null)
        {
            SetupPreviewStudio();
        }
        else
        {
            previewStudioRoot.SetActive(true);
            if (previewCamera != null) previewCamera.enabled = true;
        }

        currentPreviewID = -999;
        UpdatePreviewStudioModel(selectedCharacterID);
    }

    public void Hide()
    {
        isShowing = false;
        IsSelectionOpen = false;
        if (previewCamera != null) previewCamera.enabled = false;
        if (previewStudioRoot != null) previewStudioRoot.SetActive(false);
    }

    void Update()
    {
        if (!isShowing) return;

        headerPulse += Time.deltaTime * 1.5f;
        hoverAnimTime += Time.deltaTime * 4f;
        selectionAnimTime += Time.deltaTime * 3f;
        if (skinNotificationTimer > 0f) skinNotificationTimer -= Time.deltaTime;

        // Turntable smooth rotation
        if (turntable != null)
        {
            turntable.Rotate(Vector3.up * 38f * Time.deltaTime, Space.World);
        }

        // Keep 3D preview model synced with hovered or selected character
        int activePreviewID = (hoveredCharacterID >= 0) ? hoveredCharacterID : (selectedCharacterID >= 0 ? selectedCharacterID : 0);
        if (activePreviewID != currentPreviewID)
        {
            UpdatePreviewStudioModel(activePreviewID);
        }

        // Animate card timers
        if (cardAnimTimers != null && database != null && database.characters != null)
        {
            for (int i = 0; i < cardAnimTimers.Length && i < database.characters.Length; i++)
            {
                if (database.characters[i] == null) continue;
                bool isHovered = database.characters[i].characterID == hoveredCharacterID;
                bool isSelected = database.characters[i].characterID == selectedCharacterID;

                float target = (isHovered || isSelected) ? 1f : 0f;
                cardAnimTimers[i] = Mathf.MoveTowards(cardAnimTimers[i], target, Time.deltaTime * 6f);
            }
        }
    }

    void OnGUI()
    {
        if (!isShowing) return;
        if (GameIntroBriefingUI.IsBriefingOpen) return;
        if (PlayerProgressionAndShopUI.IsShopOpen) return;
        if (gradientTex == null) CreateCachedTextures();

        float sw = Screen.width;
        float sh = Screen.height;

        // Full Screen Background (translucent so outdoor 3D world & character remain visible)
        DrawBackground(sw, sh);

        // Header
        DrawHeader(sw, sh);

        // Main Layout: Grid on left (55%), Details & Large 3D Hero Stage on right (41%)
        float contentY = sh * 0.11f;
        float contentH = sh * 0.86f;
        float leftW = sw * 0.55f;
        float rightW = sw * 0.41f;
        float rightX = sw * 0.57f;
        float gap = sw * 0.015f;

        // Left Panel: 25 Champions Grid
        DrawCharacterGrid(new Rect(gap, contentY, leftW, contentH));

        // Right Panel: Preview & Details
        int displayID = (hoveredCharacterID >= 0) ? hoveredCharacterID : (selectedCharacterID >= 0 ? selectedCharacterID : 0);
        if (database != null)
        {
            CharacterData displayChar = database.GetCharacter(displayID);
            if (displayChar == null && database.characters != null && database.characters.Length > 0)
                displayChar = database.characters[0];

            if (displayChar != null)
            {
                DrawCharacterDetails(new Rect(rightX, contentY, rightW, contentH), displayChar);
            }
        }
    }

    // ==================== BACKGROUND ====================
    void DrawBackground(float sw, float sh)
    {
        GUI.color = new Color(0.02f, 0.05f, 0.03f, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

        GUI.color = new Color(0.05f, 0.14f, 0.07f, 0.25f);
        GUI.DrawTexture(new Rect(0, sh * 0.3f, sw, sh * 0.7f), gradientTex, ScaleMode.StretchToFill);

        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh * 0.085f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, sh * 0.97f, sw, sh * 0.03f), Texture2D.whiteTexture);

        float pulse = 0.6f + 0.2f * Mathf.Sin(headerPulse);
        GUI.color = new Color(0.7f * pulse, 0.55f * pulse, 0.15f * pulse, 0.8f);
        GUI.DrawTexture(new Rect(sw * 0.05f, sh * 0.09f, sw * 0.9f, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // ==================== HEADER ====================
    void DrawHeader(float sw, float sh)
    {
        // Top-left Back button returning to Team Selection
        float backW = Mathf.Max(145f, sw * 0.12f);
        float backH = 32f;
        float backX = sw * 0.02f;
        float backY = sh * 0.015f;
        string backText = LanguageSelectionUI.Localize(
            "⬅️ BACK TO TEAMS",
            ar: "⬅️ رجوع لاختيار الفريق",
            ru: "⬅️ НАЗАД К КОМАНДАМ",
            fr: "⬅️ RETOUR AUX ÉQUIPES",
            es: "⬅️ VOLVER A EQUIPOS",
            de: "⬅️ ZURÜCK ZU TEAMS",
            hi: "⬅️ वापस (टीम चयन)",
            zh: "⬅️ 返回队伍选择",
            ja: "⬅️ チーム選択へ戻る",
            pt: "⬅️ VOLTAR AOS TIMES"
        );
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(backX, backY, backW, backH), backText, new Color(0.85f, 0.45f, 0.35f), 10))
        {
            Hide();
            PlayerMovement player = GetLocalPlayer();
            if (player != null)
            {
                player.ResetTeamSelection();
            }
            TeamSelectionUI teamUI = FindAnyObjectByType<TeamSelectionUI>();
            if (teamUI != null)
            {
                teamUI.Reopen();
            }
            return;
        }

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.Max(14, (int)(sw * 0.022f));
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        float glow = 0.85f + 0.15f * Mathf.Sin(headerPulse);
        titleStyle.normal.textColor = new Color(0.95f * glow, 0.82f * glow, 0.25f, 1f);

        GUI.Label(new Rect(0, sh * 0.015f, sw, sh * 0.04f),
            LanguageSelectionUI.Localize("CHOOSE YOUR SACRED CHAMPION", "اختر بطلك المقدس", "ВЫБЕРИТЕ ВАШЕГО ЧЕМПИОНА", "ELIGE A TU CAMPEÓN SAGRADO", "CHOISISSEZ VOTRE CHAMPION SACRÉ"), titleStyle);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = Mathf.Max(10, (int)(sw * 0.011f));
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.normal.textColor = new Color(0.5f, 0.7f, 0.45f, 0.75f);
        subStyle.fontStyle = FontStyle.Italic;
        GUI.Label(new Rect(0, sh * 0.05f, sw, sh * 0.03f),
            LanguageSelectionUI.Localize("Hover or click any Guardian to inspect full 3D armor, powers, and combat stats", "مرر الفأرة أو انقر على أي حارس لفحص دروعه وقدراته وإحصائياته", "Наведите или нажмите на героя для просмотра брони и характеристик", "Pasa el cursor o haz clic para ver armadura y atributos", "Survolez ou cliquez pour inspecter l'armure et les stats"), subStyle);
    }

    // ==================== CHARACTER GRID ====================
    void DrawCharacterGrid(Rect area)
    {
        if (database == null || database.characters == null) return;

        EnchantedNatureGUI.DrawOrganicScroll(area, new Color(0.03f, 0.07f, 0.04f, 0.85f));

        GUIStyle gridTitle = new GUIStyle(GUI.skin.label);
        gridTitle.fontSize = Mathf.Max(11, (int)(area.width * 0.028f));
        gridTitle.fontStyle = FontStyle.Bold;
        gridTitle.alignment = TextAnchor.MiddleLeft;
        gridTitle.normal.textColor = new Color(0.6f, 0.85f, 0.5f, 0.9f);
        GUI.Label(new Rect(area.x + GRID_PADDING, area.y + 6, area.width * 0.6f, 28),
            LanguageSelectionUI.Localize("GUARDIANS OF THE WILD (25 HEROES)", "حراس البرية (25 بطلاً)", "СТРАЖИ ДИКОЙ ПРИРОДЫ (25 ГЕРОЕВ)", "GUARDIANES DE LA NATURALEZA (25 HÉROES)", "GARDIENS DE LA NATURE (25 HÉROS)"), gridTitle);

        GUIStyle countStyle = new GUIStyle(GUI.skin.label);
        countStyle.fontSize = Mathf.Max(9, (int)(area.width * 0.02f));
        countStyle.alignment = TextAnchor.MiddleRight;
        countStyle.normal.textColor = new Color(0.4f, 0.6f, 0.4f, 0.6f);
        int validCount = 0;
        foreach (var c in database.characters) if (c != null) validCount++;
        GUI.Label(new Rect(area.x, area.y + 8, area.width - GRID_PADDING, 24),
            validCount + " " + LanguageSelectionUI.Localize("Champions Ready", "أبطال جاهزون", "героев готовы", "Campeones Listos", "Champions Prêts"), countStyle);

        GUI.color = new Color(0.3f, 0.6f, 0.3f, 0.3f);
        GUI.DrawTexture(new Rect(area.x + GRID_PADDING, area.y + 34, area.width - GRID_PADDING * 2, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float gridY = area.y + 40;
        float gridH = area.height - 48;
        float gridInnerW = area.width - GRID_PADDING * 2;

        float cardW = (gridInnerW - (GRID_COLUMNS - 1) * CARD_SPACING) / GRID_COLUMNS;
        float cardH = cardW * CARD_ASPECT;

        int rows = Mathf.CeilToInt((float)database.characters.Length / GRID_COLUMNS);
        float totalGridH = rows * (cardH + CARD_SPACING);

        gridScrollPosition = GUI.BeginScrollView(
            new Rect(area.x + GRID_PADDING, gridY, gridInnerW + 16, gridH),
            gridScrollPosition,
            new Rect(0, 0, gridInnerW, totalGridH + GRID_PADDING)
        );

        int frameHovered = -1;

        for (int i = 0; i < database.characters.Length; i++)
        {
            CharacterData ch = database.characters[i];
            if (ch == null) continue;

            int col = i % GRID_COLUMNS;
            int row = i / GRID_COLUMNS;

            float cx = col * (cardW + CARD_SPACING);
            float cy = row * (cardH + CARD_SPACING);

            Rect cardRect = new Rect(cx, cy, cardW, cardH);

            bool isSelected = (selectedCharacterID == ch.characterID);
            bool isHovered = cardRect.Contains(Event.current.mousePosition);

            if (isHovered) frameHovered = ch.characterID;

            float animT = (cardAnimTimers != null && i < cardAnimTimers.Length) ? cardAnimTimers[i] : 0f;

            DrawCharacterCard(cardRect, ch, isSelected, isHovered, animT);

            // Responsive invisible button over entire card
            if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none))
            {
                selectedCharacterID = ch.characterID;
                selectionAnimTime = 0f;
                currentSkinIndex = 0;
                skinEquipped = false;
                availableSkins = CharacterSkinDatabase.GetSkinsForCharacter(ch.characterID);
                if (availableSkins != null && availableSkins.Length > 0)
                    currentSkin = availableSkins[0];
            }
        }

        GUI.EndScrollView();
        hoveredCharacterID = frameHovered;
    }

    // ==================== CHARACTER CARD ====================
    void DrawCharacterCard(Rect rect, CharacterData ch, bool isSelected, bool isHovered, float animT)
    {
        Color roleColor = RoleColors.ContainsKey(ch.role) ? RoleColors[ch.role] : Color.gray;

        float lift = animT * 3f;
        Rect drawRect = new Rect(rect.x, rect.y - lift, rect.width, rect.height);

        if (animT > 0.01f)
        {
            GUI.color = new Color(0, 0, 0, 0.4f * animT);
            GUI.DrawTexture(new Rect(drawRect.x + 3, drawRect.y + drawRect.height - 2, drawRect.width, 4), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        float bgBright = Mathf.Lerp(0.06f, 0.14f, animT);
        Color bgCol = isSelected
            ? new Color(roleColor.r * 0.2f, roleColor.g * 0.2f, roleColor.b * 0.2f, 0.95f)
            : new Color(bgBright * 0.7f, bgBright, bgBright * 0.8f, 0.92f);
        GUI.color = bgCol;
        GUI.DrawTexture(drawRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Portrait Area
        float portraitH = drawRect.height * 0.58f;
        Rect portraitRect = new Rect(drawRect.x, drawRect.y, drawRect.width, portraitH);

        GUI.color = new Color(0, 0, 0, 0.35f);
        GUI.DrawTexture(new Rect(portraitRect.x, portraitRect.y + portraitH * 0.4f,
            portraitRect.width, portraitH * 0.6f), gradientTex, ScaleMode.StretchToFill);
        GUI.color = Color.white;

        if (ch.characterIcon != null)
        {
            GUI.DrawTexture(
                new Rect(drawRect.x + drawRect.width * 0.1f, drawRect.y + 4,
                    drawRect.width * 0.8f, portraitH - 8),
                ch.characterIcon.texture, ScaleMode.ScaleToFit);
        }
        else
        {
            // Heraldic Crest Plaque for Champion
            float crestW = drawRect.width * 0.76f;
            float crestH = portraitH * 0.78f;
            float crestX = drawRect.x + (drawRect.width - crestW) / 2f;
            float crestY = drawRect.y + (portraitH - crestH) / 2f + 2f;
            Rect crestRect = new Rect(crestX, crestY, crestW, crestH);

            // Role background shield
            GUI.color = new Color(roleColor.r * 0.2f, roleColor.g * 0.2f, roleColor.b * 0.2f, 0.92f);
            GUI.DrawTexture(crestRect, Texture2D.whiteTexture);
            DrawBorder(crestRect, new Color(roleColor.r * 0.85f, roleColor.g * 0.85f, roleColor.b * 0.85f, 0.8f), 1.5f);
            GUI.color = Color.white;

            // Class Heraldry Icon
            GUIStyle iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = Mathf.Max(16, (int)(crestH * 0.48f));
            iconStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(crestX, crestY, crestW, crestH * 0.65f), GetRoleEmoji(ch.role), iconStyle);

            // Class Sub-title
            GUIStyle subTitle = new GUIStyle(GUI.skin.label);
            subTitle.fontSize = Mathf.Max(7, (int)(crestW * 0.15f));
            subTitle.fontStyle = FontStyle.Bold;
            subTitle.alignment = TextAnchor.LowerCenter;
            subTitle.normal.textColor = new Color(roleColor.r, roleColor.g, roleColor.b, 0.95f);
            string rName = GetRoleName(ch.role);
            GUI.Label(new Rect(crestX, crestY, crestW, crestH - 3f), rName, subTitle);
        }

        // Info Area
        float infoY = drawRect.y + portraitH;
        float infoH = drawRect.height - portraitH;

        GUI.color = new Color(roleColor.r, roleColor.g, roleColor.b, 0.85f);
        GUI.DrawTexture(new Rect(drawRect.x, infoY, drawRect.width, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Name
        GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.fontSize = Mathf.Max(9, (int)(drawRect.width * 0.12f));
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.alignment = TextAnchor.UpperCenter;
        nameStyle.normal.textColor = isSelected
            ? new Color(1f, 0.95f, 0.6f)
            : new Color(0.85f, 0.9f, 0.8f);
        string localizedCharName = LanguageSelectionUI.Localize(ch.characterName);
        GUI.Label(new Rect(drawRect.x + 2, infoY + 4, drawRect.width - 4, infoH * 0.38f),
            localizedCharName, nameStyle);

        // Role tag
        GUIStyle roleStyle = new GUIStyle(GUI.skin.label);
        roleStyle.fontSize = Mathf.Max(7, (int)(drawRect.width * 0.085f));
        roleStyle.alignment = TextAnchor.UpperCenter;
        roleStyle.normal.textColor = new Color(roleColor.r, roleColor.g, roleColor.b, 0.9f);
        string roleName = GetRoleName(ch.role);
        GUI.Label(new Rect(drawRect.x + 2, infoY + infoH * 0.38f, drawRect.width - 4, infoH * 0.25f),
            roleName, roleStyle);

        // Stat mini-bars
        float barY = infoY + infoH * 0.66f;
        float barH = 3f;
        float barW = drawRect.width * 0.72f;
        float barX = drawRect.x + drawRect.width * 0.14f;

        float healthNorm = Mathf.Clamp01(ch.maxHealth / 400f);
        GUI.color = new Color(0.1f, 0.15f, 0.1f, 0.6f);
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);
        GUI.color = new Color(0.2f, 0.8f, 0.3f, 0.75f);
        GUI.DrawTexture(new Rect(barX, barY, barW * healthNorm, barH), Texture2D.whiteTexture);

        float dmgNorm = Mathf.Clamp01(ch.attackDamage / 40f);
        GUI.color = new Color(0.1f, 0.15f, 0.1f, 0.6f);
        GUI.DrawTexture(new Rect(barX, barY + barH + 2, barW, barH), Texture2D.whiteTexture);
        GUI.color = new Color(0.9f, 0.3f, 0.2f, 0.75f);
        GUI.DrawTexture(new Rect(barX, barY + barH + 2, barW * dmgNorm, barH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Selection / Hover Border
        if (isSelected)
        {
            float selPulse = 0.75f + 0.25f * Mathf.Sin(selectionAnimTime * 2.5f);
            Color selBorder = new Color(roleColor.r * selPulse, roleColor.g * selPulse, roleColor.b * selPulse, 0.95f);
            DrawBorder(drawRect, selBorder, 2f);

            float glowSize = 12f;
            GUI.color = new Color(roleColor.r, roleColor.g, roleColor.b, 0.5f * selPulse);
            GUI.DrawTexture(new Rect(drawRect.x - 2, drawRect.y - 2, glowSize, glowSize), glowTex);
            GUI.DrawTexture(new Rect(drawRect.x + drawRect.width - glowSize + 2, drawRect.y - 2, glowSize, glowSize), glowTex);
            GUI.color = Color.white;
        }
        else if (isHovered)
        {
            Color hoverBorder = new Color(0.5f, 0.85f, 0.5f, 0.6f + 0.2f * Mathf.Sin(hoverAnimTime));
            DrawBorder(drawRect, hoverBorder, 1.5f);
        }
    }

    // ==================== CHARACTER DETAILS PANEL ====================
    void DrawCharacterDetails(Rect area, CharacterData ch)
    {
        Color roleColor = RoleColors.ContainsKey(ch.role) ? RoleColors[ch.role] : Color.gray;

        EnchantedNatureGUI.DrawOrganicScroll(area, new Color(0.03f, 0.06f, 0.04f, 0.94f));

        GUI.color = new Color(roleColor.r, roleColor.g, roleColor.b, 0.75f);
        GUI.DrawTexture(new Rect(area.x + 10, area.y + 2, area.width - 20, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Auto-load available skins and saved skin for this character
        if (availableSkins == null || availableSkins.Length == 0 || lastDetailsCharID != ch.characterID)
        {
            lastDetailsCharID = ch.characterID;
            availableSkins = CharacterSkinDatabase.GetSkinsForCharacter(ch.characterID);
            int savedSkin = PlayerProfile.Instance != null ? PlayerProfile.Instance.GetSelectedSkin(ch.characterID) : 0;
            currentSkinIndex = Mathf.Clamp(savedSkin, 0, availableSkins.Length - 1);
            currentSkin = availableSkins[currentSkinIndex];
            skinEquipped = (PlayerProfile.Instance == null || PlayerProfile.Instance.IsSkinUnlocked(ch.characterID, currentSkinIndex));
            ApplySkinToPreviewModel(ch, currentSkin);
        }

        // ==================== TOP PINNED HEADER ====================
        float pad = 12f;
        float headerH = 54f;

        // Champion Name
        GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.fontSize = Mathf.Max(15, (int)(area.width * 0.058f));
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.alignment = TextAnchor.MiddleCenter;
        nameStyle.normal.textColor = new Color(0.96f, 0.86f, 0.28f);
        string locNameHeader = LanguageSelectionUI.Localize(ch.characterName);
        GUI.Label(new Rect(area.x + pad, area.y + 4f, area.width - pad * 2, 26f), locNameHeader.ToUpper(), nameStyle);

        // Champion Role Badge
        string roleName = GetRoleName(ch.role);
        GUIStyle roleTagStyle = new GUIStyle(GUI.skin.label);
        roleTagStyle.fontSize = Mathf.Max(9, (int)(area.width * 0.028f));
        roleTagStyle.fontStyle = FontStyle.Bold;
        roleTagStyle.alignment = TextAnchor.MiddleCenter;
        roleTagStyle.normal.textColor = roleColor;

        float badgeW = Mathf.Min(160f, area.width * 0.45f);
        float badgeH = 18f;
        Rect badgeRect = new Rect(area.x + (area.width - badgeW) / 2f, area.y + 32f, badgeW, badgeH);
        GUI.color = new Color(roleColor.r * 0.15f, roleColor.g * 0.15f, roleColor.b * 0.15f, 0.85f);
        GUI.DrawTexture(badgeRect, Texture2D.whiteTexture);
        DrawBorder(badgeRect, new Color(roleColor.r, roleColor.g, roleColor.b, 0.6f), 1f);
        GUI.color = Color.white;
        GUI.Label(badgeRect, GetRoleEmoji(ch.role) + " " + roleName, roleTagStyle);

        // Header separator line
        GUI.color = new Color(0.35f, 0.6f, 0.35f, 0.35f);
        GUI.DrawTexture(new Rect(area.x + pad, area.y + headerH, area.width - pad * 2, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ==================== BOTTOM PINNED FOOTER ====================
        float footerH = 74f;
        float footerY = area.y + area.height - footerH - 4f;

        // Footer separator line
        GUI.color = new Color(0.35f, 0.6f, 0.35f, 0.35f);
        GUI.DrawTexture(new Rect(area.x + pad, footerY - 2f, area.width - pad * 2, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Player Economy Status / Notification Bar
        int curCoins = PlayerProfile.Instance != null ? PlayerProfile.Instance.Coins : 0;
        int curGems = PlayerProfile.Instance != null ? PlayerProfile.Instance.Gems : 0;
        int curLevel = PlayerProfile.Instance != null ? PlayerProfile.Instance.AccountLevel : 1;

        GUIStyle ecoStyle = new GUIStyle(GUI.skin.label);
        ecoStyle.fontSize = Mathf.Max(8, (int)(area.width * 0.024f));
        ecoStyle.alignment = TextAnchor.MiddleCenter;
        ecoStyle.fontStyle = FontStyle.Bold;

        if (skinNotificationTimer > 0f)
        {
            ecoStyle.normal.textColor = new Color(0.35f, 1f, 0.5f);
            GUI.Label(new Rect(area.x + pad, footerY + 2f, area.width - pad * 2, 18f), skinNotification, ecoStyle);
        }
        else
        {
            ecoStyle.normal.textColor = new Color(0.92f, 0.88f, 0.7f);
            string ecoStr = $"🪙 {curCoins} Coins   |   💎 {curGems} Gems   |   ⭐ Lv.{curLevel}";
            GUI.Label(new Rect(area.x + pad, footerY + 2f, area.width - pad * 2, 18f), ecoStr, ecoStyle);
        }

        // Fixed Confirm Button (Pinned at Bottom)
        float confirmH = 44f;
        Rect confirmRect = new Rect(area.x + pad, footerY + 22f, area.width - pad * 2, confirmH);

        float confirmPulse = 0.85f + 0.15f * Mathf.Sin(selectionAnimTime * 2.5f);
        Color confirmBg = new Color(0.12f * confirmPulse, 0.38f * confirmPulse, 0.12f * confirmPulse, 0.95f);
        GUI.color = confirmBg;
        GUI.DrawTexture(confirmRect, Texture2D.whiteTexture);
        DrawBorder(confirmRect, new Color(0.4f * confirmPulse, 0.85f * confirmPulse, 0.3f * confirmPulse, 0.9f), 2f);
        GUI.color = Color.white;

        GUIStyle confirmStyle = new GUIStyle(GUI.skin.label);
        confirmStyle.fontSize = Mathf.Max(11, (int)(area.width * 0.038f));
        confirmStyle.fontStyle = FontStyle.Bold;
        confirmStyle.alignment = TextAnchor.MiddleCenter;
        confirmStyle.normal.textColor = new Color(0.95f * confirmPulse, 0.98f * confirmPulse, 0.5f);

        string buttonText = "🌿 " + LanguageSelectionUI.Localize("COMMENCE EXPEDITION", "انطلق في الحملة", "НАЧАТЬ ПОХОД", "INICIAR EXPEDICIÓN", "COMMENCER L'EXPÉDITION") + " (" + LanguageSelectionUI.Localize(ch.characterName).ToUpper() + ") 🌿";
        GUI.Label(confirmRect, buttonText, confirmStyle);

        if (GUI.Button(confirmRect, GUIContent.none, GUIStyle.none))
        {
            selectedCharacterID = ch.characterID;
            ConfirmSelection();
        }

        // ==================== MIDDLE SCROLLABLE CONTENT ====================
        float scrollY = area.y + headerH + 2f;
        float scrollH = footerY - scrollY - 2f;
        Rect scrollRect = new Rect(area.x + 6f, scrollY, area.width - 12f, scrollH);

        float innerW = scrollRect.width - 18f;
        float totalVirtualH = 780f;
        Rect viewRect = new Rect(0, 0, innerW, totalVirtualH);

        detailsScrollPosition = GUI.BeginScrollView(scrollRect, detailsScrollPosition, viewRect, false, true);

        float curY = 4f;

        // 1. 3D LIVE TURNTABLE PREVIEW STAGE
        float previewH = 150f;
        Rect previewRect = new Rect(0, curY, innerW, previewH);

        GUI.color = new Color(0.02f, 0.04f, 0.03f, 0.95f);
        GUI.DrawTexture(previewRect, Texture2D.whiteTexture);
        DrawBorder(previewRect, new Color(roleColor.r * 0.6f, roleColor.g * 0.6f, roleColor.b * 0.6f, 0.6f), 1f);
        GUI.color = Color.white;

        if (previewRenderTexture != null)
        {
            GUI.DrawTexture(previewRect, previewRenderTexture, ScaleMode.ScaleToFit);
        }

        GUIStyle previewLabel = new GUIStyle(GUI.skin.label);
        previewLabel.fontSize = Mathf.Max(8, (int)(innerW * 0.022f));
        previewLabel.alignment = TextAnchor.LowerCenter;
        previewLabel.normal.textColor = new Color(0.5f, 0.7f, 0.5f, 0.6f);
        previewLabel.fontStyle = FontStyle.Italic;
        GUI.Label(new Rect(previewRect.x, previewRect.y, previewRect.width, previewRect.height - 4),
            LanguageSelectionUI.Localize("✦ Live 3D Turntable Preview ✦", "✦ عرض ثلاثي الأبعاد مباشر ✦", "✦ 3D Просмотр героя ✦", "✦ Vista previa 3D ✦", "✦ Aperçu 3D en direct ✦"), previewLabel);

        curY += previewH + 8f;

        // 2. CHAMPION COMBAT STATS
        GUIStyle statHeader = new GUIStyle(GUI.skin.label);
        statHeader.fontSize = Mathf.Max(10, (int)(innerW * 0.032f));
        statHeader.fontStyle = FontStyle.Bold;
        statHeader.normal.textColor = new Color(0.7f, 0.85f, 0.6f, 0.9f);
        GUI.Label(new Rect(0, curY, innerW, 20),
            LanguageSelectionUI.Localize("CHAMPION COMBAT STATS", "إحصائيات القتال للبطل", "БОЕВЫЕ ХАРАКТЕРИСТИКИ", "ESTADÍSTICAS DE COMBATE", "STATS DE COMBAT DU CHAMPION"), statHeader);
        curY += 22f;

        float statBarH = 13f;
        float statGap = 3f;
        int statFontSize = Mathf.Max(8, (int)(innerW * 0.024f));

        string healthBonus = (skinEquipped && currentSkin != null && currentSkin.healthMultiplier != 1f)
            ? $" (+{(currentSkin.healthMultiplier - 1f) * 100:F0}%)" : "";
        DrawStatBar(new Rect(0, curY, innerW, statBarH),
            LanguageSelectionUI.Localize("HP", "الصحة", "ЗДОРОВЬЕ", "SALUD", "VIE") + "  " + ch.maxHealth + healthBonus,
            ch.maxHealth / 400f,
            new Color(0.2f, 0.75f, 0.3f), statFontSize);
        curY += statBarH + statGap;

        string dmgBonus = (skinEquipped && currentSkin != null && currentSkin.damageMultiplier != 1f)
            ? $" (+{(currentSkin.damageMultiplier - 1f) * 100:F0}%)" : "";
        DrawStatBar(new Rect(0, curY, innerW, statBarH),
            LanguageSelectionUI.Localize("ATK", "الهجوم", "АТАКА", "ATAQUE", "ATTAQUE") + "  " + ch.attackDamage + dmgBonus,
            ch.attackDamage / 40f,
            new Color(0.9f, 0.3f, 0.2f), statFontSize);
        curY += statBarH + statGap;

        string spdBonus = (skinEquipped && currentSkin != null && currentSkin.speedMultiplier != 1f)
            ? $" (+{(currentSkin.speedMultiplier - 1f) * 100:F0}%)" : "";
        DrawStatBar(new Rect(0, curY, innerW, statBarH),
            LanguageSelectionUI.Localize("SPD", "السرعة", "СКОРОСТЬ", "VELOCIDAD", "VITESSE") + "  " + ch.moveSpeed + spdBonus,
            ch.moveSpeed / 12f,
            new Color(0.3f, 0.7f, 0.9f), statFontSize);
        curY += statBarH + statGap;

        DrawStatBar(new Rect(0, curY, innerW, statBarH),
            LanguageSelectionUI.Localize("RNG", "المدى", "ДАЛЬНОСТЬ", "RANGO", "PORTÉE") + "  " + ch.attackRange + "m",
            ch.attackRange / 20f,
            new Color(0.9f, 0.7f, 0.2f), statFontSize);
        curY += statBarH + statGap;

        DrawStatBar(new Rect(0, curY, innerW, statBarH),
            LanguageSelectionUI.Localize("COOLDOWN", "وقت الانتظار", "ПЕРЕЗАРЯДКА", "RECARGA", "RECHARGE") + "  " + ch.attackCooldown.ToString("F1") + "s",
            1f - (ch.attackCooldown / 3f),
            new Color(0.7f, 0.5f, 0.9f), statFontSize);
        curY += statBarH + statGap;

        string doubleJumpLabel = LanguageSelectionUI.Localize("(Double Jump)", "(قفز مزدوج)", "(Двойной прыжок)", "(Salto doble)", "(Double saut)");
        string jumpStr = ch.canDoubleJump ? ch.jumpForce + " " + doubleJumpLabel : ch.jumpForce.ToString();
        DrawStatBar(new Rect(0, curY, innerW, statBarH),
            LanguageSelectionUI.Localize("JUMP", "القفز", "ПРЫЖОК", "SALTO", "SAUT") + "  " + jumpStr,
            ch.jumpForce / 16f,
            new Color(0.5f, 0.9f, 0.7f), statFontSize);
        curY += statBarH + 6f;

        GUIStyle atkTypeStyle = new GUIStyle(GUI.skin.label);
        atkTypeStyle.fontSize = statFontSize;
        atkTypeStyle.alignment = TextAnchor.MiddleLeft;
        atkTypeStyle.normal.textColor = new Color(0.7f, 0.8f, 0.6f, 0.85f);
        GUI.Label(new Rect(0, curY, innerW, 16),
            LanguageSelectionUI.Localize("Attack Style: ", "أسلوب الهجوم: ", "Стиль атаки: ", "Estilo de ataque: ", "Style d'attaque : ") + GetAttackTypeLabel(ch.attackType), atkTypeStyle);
        curY += 18f;

        GUI.color = new Color(0.3f, 0.5f, 0.3f, 0.3f);
        GUI.DrawTexture(new Rect(0, curY, innerW, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;
        curY += 8f;

        // 3. SACRED SKINS, WEAPONS & UPGRADES SECTION (5 SKINS)
        GUIStyle skinsHeader = new GUIStyle(GUI.skin.label);
        skinsHeader.fontSize = Mathf.Max(10, (int)(innerW * 0.034f));
        skinsHeader.fontStyle = FontStyle.Bold;
        skinsHeader.normal.textColor = new Color(0.95f, 0.85f, 0.35f);
        GUI.Label(new Rect(0, curY, innerW, 20),
            LanguageSelectionUI.Localize("👑 SACRED SKINS & POWER UPGRADES", "👑 المظاهر والأسلحة والترقيات المقدسة", "👑 СВЯЩЕННЫЕ ОБЛИКИ И УЛУЧШЕНИЯ", "👑 ASPECTOS SAGRADOS Y MEJORAS", "👑 SKINS SACRÉS ET AMÉLIORATIONS"), skinsHeader);
        curY += 20f;

        GUIStyle subSkins = new GUIStyle(GUI.skin.label);
        subSkins.fontSize = Mathf.Max(8, (int)(innerW * 0.022f));
        subSkins.normal.textColor = new Color(0.6f, 0.8f, 0.6f, 0.75f);
        GUI.Label(new Rect(0, curY, innerW, 16),
            LanguageSelectionUI.Localize("Equip custom war outfits for permanent combat power buffs:", "جهز أزياء الحرب للحصول على مضاعفات قوة دائمة:", "Экипируйте облики для постоянных бонусов к характеристикам:", "Equipa aspectos de guerra para mejoras de combate:", "Équipez des skins pour des bonus de combat permanents :"), subSkins);
        curY += 20f;

        if (availableSkins != null)
        {
            for (int s = 0; s < availableSkins.Length; s++)
            {
                CharacterSkin skin = availableSkins[s];
                if (skin == null) continue;

                float cardH = 68f;
                Rect cardR = new Rect(0, curY, innerW, cardH);

                bool isUnlocked = (PlayerProfile.Instance == null) || PlayerProfile.Instance.IsSkinUnlocked(ch.characterID, s);
                bool isEquipped = (s == currentSkinIndex && skinEquipped);
                bool isPreviewing = (s == currentSkinIndex);

                // Card Background
                Color cardBg = isEquipped
                    ? new Color(0.08f, 0.22f, 0.12f, 0.95f)
                    : (isPreviewing ? new Color(0.12f, 0.16f, 0.14f, 0.95f) : new Color(0.05f, 0.08f, 0.06f, 0.92f));
                GUI.color = cardBg;
                GUI.DrawTexture(cardR, Texture2D.whiteTexture);

                Color borderCol = isEquipped
                    ? new Color(0.3f, 0.95f, 0.45f, 0.9f)
                    : (isUnlocked ? new Color(0.4f, 0.7f, 0.5f, 0.6f) : new Color(0.35f, 0.35f, 0.35f, 0.5f));
                DrawBorder(cardR, borderCol, isEquipped ? 2f : 1f);
                GUI.color = Color.white;

                // Left: Skin Color Swatch / Shield
                float iconBoxW = 34f;
                Rect iconBox = new Rect(cardR.x + 6f, cardR.y + 8f, iconBoxW, cardH - 16f);
                GUI.color = skin.skinColor;
                GUI.DrawTexture(iconBox, Texture2D.whiteTexture);
                DrawBorder(iconBox, Color.black, 1f);
                GUI.color = Color.white;

                string skinEmoji = (s == 0) ? "🛡️" : (s == 1 ? "🌿" : (s == 2 ? "🔥" : (s == 3 ? "⚡" : "👑")));
                GUIStyle sIconStyle = new GUIStyle(GUI.skin.label);
                sIconStyle.fontSize = 14;
                sIconStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(iconBox, skinEmoji, sIconStyle);

                // Middle: Name, Stat Buffs, and Description
                float textX = cardR.x + iconBoxW + 12f;
                float btnW = 105f;
                float textW = cardR.width - iconBoxW - btnW - 20f;

                GUIStyle sNameStyle = new GUIStyle(GUI.skin.label);
                sNameStyle.fontSize = Mathf.Max(9, (int)(innerW * 0.026f));
                sNameStyle.fontStyle = FontStyle.Bold;
                sNameStyle.normal.textColor = isEquipped ? new Color(0.4f, 1f, 0.55f) : (isUnlocked ? new Color(0.95f, 0.88f, 0.5f) : new Color(0.7f, 0.7f, 0.7f));
                GUI.Label(new Rect(textX, cardR.y + 4f, textW, 18f), skin.skinName, sNameStyle);

                // Stat Buff tags
                string buffText = "";
                if (skin.healthMultiplier > 1f) buffText += $"+{(skin.healthMultiplier - 1f) * 100:F0}% HP  ";
                if (skin.damageMultiplier > 1f) buffText += $"+{(skin.damageMultiplier - 1f) * 100:F0}% ATK  ";
                if (skin.speedMultiplier > 1f) buffText += $"+{(skin.speedMultiplier - 1f) * 100:F0}% SPD  ";
                if (string.IsNullOrEmpty(buffText)) buffText = LanguageSelectionUI.Localize("Standard Base Attributes", "السمات الأساسية القياسية", "Базовые характеристики", "Atributos básicos", "Attributs de base");

                GUIStyle buffStyle = new GUIStyle(GUI.skin.label);
                buffStyle.fontSize = Mathf.Max(8, (int)(innerW * 0.022f));
                buffStyle.fontStyle = FontStyle.Bold;
                buffStyle.normal.textColor = new Color(0.35f, 0.95f, 0.45f);
                GUI.Label(new Rect(textX, cardR.y + 22f, textW, 16f), buffText, buffStyle);

                GUIStyle descStyle = new GUIStyle(GUI.skin.label);
                descStyle.fontSize = Mathf.Max(7, (int)(innerW * 0.020f));
                descStyle.normal.textColor = new Color(0.75f, 0.8f, 0.75f, 0.75f);
                descStyle.wordWrap = true;
                GUI.Label(new Rect(textX, cardR.y + 38f, textW, 26f), skin.description, descStyle);

                // Right: Action / Unlock / Equip Button
                Rect btnRect = new Rect(cardR.x + cardR.width - btnW - 6f, cardR.y + (cardH - 30f) / 2f, btnW, 30f);

                if (isEquipped)
                {
                    GUIStyle eqLabelStyle = new GUIStyle(GUI.skin.label);
                    eqLabelStyle.fontSize = Mathf.Max(8, (int)(innerW * 0.024f));
                    eqLabelStyle.fontStyle = FontStyle.Bold;
                    eqLabelStyle.alignment = TextAnchor.MiddleCenter;
                    eqLabelStyle.normal.textColor = new Color(0.3f, 1f, 0.4f);
                    GUI.Label(btnRect, LanguageSelectionUI.Localize("✓ EQUIPPED", "✓ مجهّز", "✓ НАДЕТО", "✓ EQUIPADO", "✓ ÉQUIPÉ"), eqLabelStyle);
                }
                else if (isUnlocked)
                {
                    if (DrawActionButton(btnRect, LanguageSelectionUI.Localize("EQUIP", "تجهيز", "НАДЕТЬ", "EQUIPAR", "ÉQUIPER"), new Color(0.2f, 0.55f, 0.25f)))
                    {
                        EquipSkin(ch, s);
                    }
                }
                else
                {
                    // Locked Skin Button with cost / unlock mechanism
                    switch (skin.unlockMethod)
                    {
                        case CharacterSkin.UnlockMethod.AccountLevel:
                            bool canClaim = PlayerProfile.Instance != null && PlayerProfile.Instance.AccountLevel >= skin.requiredLevel;
                            string lvlBtn = canClaim
                                ? LanguageSelectionUI.Localize($"CLAIM (Lv.{skin.requiredLevel})", $"استلام (مستوى {skin.requiredLevel})", $"ЗАБРАТЬ (Ур.{skin.requiredLevel})", $"RECLAMAR (Nvl.{skin.requiredLevel})", $"RÉCLAMER (Niv.{skin.requiredLevel})")
                                : LanguageSelectionUI.Localize($"🪙 {skin.requiredCost}", $"🪙 {skin.requiredCost}", $"🪙 {skin.requiredCost}", $"🪙 {skin.requiredCost}", $"🪙 {skin.requiredCost}");
                            Color lvlCol = canClaim ? new Color(0.25f, 0.65f, 0.35f) : new Color(0.65f, 0.5f, 0.15f);
                            if (DrawActionButton(btnRect, lvlBtn, lvlCol))
                            {
                                UnlockSkin(ch, s, skin);
                            }
                            break;

                        case CharacterSkin.UnlockMethod.WatchAds:
                            string adBtn = LanguageSelectionUI.Localize("🎬 1 AD", "🎬 إعلان 1", "🎬 1 РЕКЛАМА", "🎬 1 ANUNCIO", "🎬 1 PUB");
                            if (DrawActionButton(btnRect, adBtn, new Color(0.25f, 0.5f, 0.75f)))
                            {
                                UnlockSkin(ch, s, skin);
                            }
                            break;

                        case CharacterSkin.UnlockMethod.PurchaseCoins:
                            string coinBtn = $"🪙 {skin.requiredCost}";
                            if (DrawActionButton(btnRect, coinBtn, new Color(0.7f, 0.55f, 0.15f)))
                            {
                                UnlockSkin(ch, s, skin);
                            }
                            break;

                        case CharacterSkin.UnlockMethod.PurchaseGems:
                            string gemBtn = $"💎 {skin.requiredCost}";
                            if (DrawActionButton(btnRect, gemBtn, new Color(0.6f, 0.25f, 0.75f)))
                            {
                                UnlockSkin(ch, s, skin);
                            }
                            break;
                    }
                }

                // If user clicks anywhere on the card, preview this skin on the 3D model
                if (GUI.Button(new Rect(cardR.x, cardR.y, cardR.width - btnW - 8f, cardR.height), GUIContent.none, GUIStyle.none))
                {
                    currentSkinIndex = s;
                    currentSkin = skin;
                    ApplySkinToPreviewModel(ch, skin);
                }

                curY += cardH + 6f;
            }
        }

        GUI.EndScrollView();
    }

    void EquipSkin(CharacterData ch, int skinIdx)
    {
        if (availableSkins == null || skinIdx < 0 || skinIdx >= availableSkins.Length) return;
        currentSkinIndex = skinIdx;
        currentSkin = availableSkins[skinIdx];
        skinEquipped = true;
        if (PlayerProfile.Instance != null)
        {
            PlayerProfile.Instance.SetSelectedSkin(ch.characterID, skinIdx);
        }
        ApplySkinToPreviewModel(ch, currentSkin);

        PlayerMovement localPlayer = GetLocalPlayer();
        if (localPlayer != null)
        {
            localPlayer.ApplyCharacter(ch.characterID);
        }

        skinNotification = LanguageSelectionUI.Localize(
            $"✓ EQUIPPED: {currentSkin.skinName}!",
            ar: $"✓ تم تجهيز: {currentSkin.skinName}!",
            ru: $"✓ НАДЕТ ОБЛИК: {currentSkin.skinName}!",
            es: $"✓ ¡EQUIPADO: {currentSkin.skinName}!",
            fr: $"✓ ÉQUIPÉ : {currentSkin.skinName}!"
        );
        skinNotificationTimer = 3f;
    }

    void UnlockSkin(CharacterData ch, int skinIdx, CharacterSkin skin)
    {
        if (PlayerProfile.Instance == null)
        {
            EquipSkin(ch, skinIdx);
            return;
        }

        switch (skin.unlockMethod)
        {
            case CharacterSkin.UnlockMethod.Free:
                PlayerProfile.Instance.SetSkinUnlocked(ch.characterID, skinIdx, true);
                EquipSkin(ch, skinIdx);
                break;

            case CharacterSkin.UnlockMethod.AccountLevel:
                if (PlayerProfile.Instance.AccountLevel >= skin.requiredLevel)
                {
                    PlayerProfile.Instance.SetSkinUnlocked(ch.characterID, skinIdx, true);
                    EquipSkin(ch, skinIdx);
                }
                else if (skin.requiredCost > 0 && PlayerProfile.Instance.Coins >= skin.requiredCost)
                {
                    if (PlayerProfile.Instance.SpendCoins(skin.requiredCost))
                    {
                        PlayerProfile.Instance.SetSkinUnlocked(ch.characterID, skinIdx, true);
                        EquipSkin(ch, skinIdx);
                    }
                }
                else
                {
                    skinNotification = LanguageSelectionUI.Localize(
                        $"Requires Account Lv. {skin.requiredLevel} or {skin.requiredCost} Coins!",
                        ar: $"يتطلب مستوى حساب {skin.requiredLevel} أو {skin.requiredCost} قطعة نقدية!",
                        ru: $"Требуется уровень {skin.requiredLevel} или {skin.requiredCost} монет!",
                        es: $"¡Requiere Nivel {skin.requiredLevel} o {skin.requiredCost} Monedas!",
                        fr: $"Niveau {skin.requiredLevel} ou {skin.requiredCost} Pièces requis !"
                    );
                    skinNotificationTimer = 3f;
                }
                break;

            case CharacterSkin.UnlockMethod.WatchAds:
                PlayerProfile.Instance.WatchRewardedAd((success) =>
                {
                    if (success)
                    {
                        PlayerProfile.Instance.SetSkinUnlocked(ch.characterID, skinIdx, true);
                        EquipSkin(ch, skinIdx);
                    }
                });
                break;

            case CharacterSkin.UnlockMethod.PurchaseCoins:
                if (PlayerProfile.Instance.SpendCoins(skin.requiredCost))
                {
                    PlayerProfile.Instance.SetSkinUnlocked(ch.characterID, skinIdx, true);
                    EquipSkin(ch, skinIdx);
                }
                else
                {
                    skinNotification = LanguageSelectionUI.Localize(
                        $"Not enough coins! Need {skin.requiredCost} Coins.",
                        ar: $"العملات غير كافية! تحتاج إلى {skin.requiredCost} قطعة نقدية.",
                        ru: $"Недостаточно монет! Нужно {skin.requiredCost} монет.",
                        es: $"¡No tienes suficientes monedas! Necesitas {skin.requiredCost}.",
                        fr: $"Pas assez de pièces ! Besoin de {skin.requiredCost}."
                    );
                    skinNotificationTimer = 3f;
                }
                break;

            case CharacterSkin.UnlockMethod.PurchaseGems:
                if (PlayerProfile.Instance.SpendGems(skin.requiredCost))
                {
                    PlayerProfile.Instance.SetSkinUnlocked(ch.characterID, skinIdx, true);
                    EquipSkin(ch, skinIdx);
                }
                else
                {
                    skinNotification = LanguageSelectionUI.Localize(
                        $"Not enough gems! Need {skin.requiredCost} Gems.",
                        ar: $"الجواهر غير كافية! تحتاج إلى {skin.requiredCost} جوهرة.",
                        ru: $"Недостаточно кристаллов! Нужно {skin.requiredCost} гемов.",
                        es: $"¡No tienes suficientes gemas! Necesitas {skin.requiredCost}.",
                        fr: $"Pas assez de gemmes ! Besoin de {skin.requiredCost}."
                    );
                    skinNotificationTimer = 3f;
                }
                break;
        }
    }

    void ApplySkinToPreviewModel(CharacterData ch, CharacterSkin skin)
    {
        if (currentPreviewModel == null || skin == null) return;

        Renderer[] rends = currentPreviewModel.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            if (r == null || r.materials == null) continue;
            foreach (var mat in r.materials)
            {
                if (mat == null) continue;
                if (skin.skinColor != Color.white)
                {
                    mat.color = skin.skinColor;
                }
                if (skin.emissionColor != Color.black)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", skin.emissionColor);
                }
            }
        }
    }

    // ==================== UTILITY DRAWING METHODS ====================
    void DrawStatBar(Rect rect, string label, float value, Color barColor, int fontSize)
    {
        value = Mathf.Clamp01(value);

        GUI.color = new Color(0.05f, 0.08f, 0.05f, 0.7f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color = new Color(barColor.r, barColor.g, barColor.b, 0.7f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * value, rect.height), Texture2D.whiteTexture);

        if (value > 0.02f)
        {
            float edgeX = rect.x + rect.width * value - 4;
            GUI.color = new Color(barColor.r, barColor.g, barColor.b, 0.4f);
            GUI.DrawTexture(new Rect(edgeX, rect.y, 4, rect.height), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = fontSize;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        labelStyle.normal.textColor = new Color(0.9f, 0.95f, 0.85f, 0.95f);
        labelStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(rect.x + 6, rect.y, rect.width - 12, rect.height), label, labelStyle);
    }

    void DrawBorder(Rect rect, Color color, float thickness)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    bool DrawArrowButton(Rect rect, bool isLeft)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        Color bg = hover ? new Color(0.15f, 0.3f, 0.15f, 0.9f) : new Color(0.08f, 0.15f, 0.08f, 0.8f);
        GUI.color = bg;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle arrowStyle = new GUIStyle(GUI.skin.label);
        arrowStyle.fontSize = Mathf.Max(12, (int)(rect.height * 0.6f));
        arrowStyle.alignment = TextAnchor.MiddleCenter;
        arrowStyle.normal.textColor = hover ? new Color(0.9f, 0.9f, 0.5f) : new Color(0.6f, 0.7f, 0.5f);
        arrowStyle.fontStyle = FontStyle.Bold;
        GUI.Label(rect, isLeft ? "<" : ">", arrowStyle);

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    bool DrawActionButton(Rect rect, string text, Color bgColor)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        float bright = hover ? 1.3f : 1f;
        GUI.color = new Color(bgColor.r * bright, bgColor.g * bright, bgColor.b * bright, 0.9f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        DrawBorder(rect, new Color(0.4f, 0.7f, 0.3f, 0.5f), 1f);
        GUI.color = Color.white;

        GUIStyle btnStyle = new GUIStyle(GUI.skin.label);
        btnStyle.fontSize = Mathf.Max(9, (int)(rect.width * 0.065f));
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.alignment = TextAnchor.MiddleCenter;
        btnStyle.normal.textColor = hover ? new Color(1f, 1f, 0.7f) : new Color(0.85f, 0.9f, 0.7f);
        GUI.Label(rect, text, btnStyle);

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    string GetRoleEmoji(CharacterData.CharacterRole role)
    {
        switch (role)
        {
            case CharacterData.CharacterRole.Tank: return "🛡️";
            case CharacterData.CharacterRole.Damage: return "⚔️";
            case CharacterData.CharacterRole.Support: return "💚";
            case CharacterData.CharacterRole.Control: return "✨";
            case CharacterData.CharacterRole.Specialist: return "🔮";
            default: return "🌿";
        }
    }

    string GetAttackTypeLabel(CharacterCombat.AttackType type)
    {
        switch (type)
        {
            case CharacterCombat.AttackType.Melee:
                return LanguageSelectionUI.Localize("Close-Range Melee", "قتال قريب", "Ближний бой", "Cuerpo a cuerpo", "Mêlée rapprochée");
            case CharacterCombat.AttackType.Ranged:
                return LanguageSelectionUI.Localize("Ranged Projectile", "مقذوف عن بعد", "Дальний бой", "Proyectil a distancia", "Projectile à distance");
            default:
                return LanguageSelectionUI.Localize("Unknown", "غير معروف", "Неизвестно", "Desconocido", "Inconnu");
        }
    }

    public PlayerMovement GetLocalPlayer()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            NetworkObject localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localObj != null)
            {
                PlayerMovement pm = localObj.GetComponent<PlayerMovement>();
                if (pm != null) return pm;
            }
        }

        PlayerMovement[] players = FindObjectsByType<PlayerMovement>();
        foreach (var p in players)
        {
            if (p.IsOwner) return p;
        }

        return FindAnyObjectByType<PlayerMovement>();
    }

    void ConfirmSelection()
    {
        int charID = selectedCharacterID >= 0 ? selectedCharacterID : (hoveredCharacterID >= 0 ? hoveredCharacterID : 5);

        PlayerMovement player = GetLocalPlayer();
        if (player != null)
        {
            // 1. Ensure team is set
            if (!player.hasSelectedTeam.Value)
            {
                player.RequestTeam(PlayerMovement.TeamType.Protector);
                player.team = PlayerMovement.TeamType.Protector;
            }

            // 2. Request character via Network RPC and apply locally immediately
            player.RequestCharacter(charID);
            player.ApplyCharacter(charID);

            // 3. Apply selected skin if equipped
            if (skinEquipped)
            {
                player.ApplySkinVisuals(currentSkin);
            }

            // 4. In Host / Offline battle: Fill teams with AI bots if not spawned yet
            if (BotManager.Instance != null && (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer))
            {
                BotManager.Instance.CreateOfflineBots(player);
            }

            // 5. Official match start: trigger countdown to transition state to Playing
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.RequestStartGame();
            }
        }

        Hide();
    }
}