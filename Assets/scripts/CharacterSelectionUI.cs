using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// CharacterSelectionUI — "The Hall of Sacred Ancients"
/// AAA PC Fantasy Game Character Selection System:
/// - 3-Column Cinematic Layout (Left Roster, Center 3D Altar Stage, Right Combat Codex & Wardrobe)
/// - Role Filtering Tabs: ALL (25), TANK (5), DAMAGE (7), SUPPORT (4), CONTROL (4), SPECIALIST (5)
/// - Interactive 3D Turntable with Mouse Drag Spin & Dual Studio Lighting (Warm Key + Emerald Rim)
/// - True Combat Attributes & Sacred Skins Multipliers (+HP, +ATK, +SPD)
/// - 100% Comprehensive Localization across 11 Languages (English, Arabic, Persian, French, Russian, Spanish, German, Hindi, Chinese, Japanese, Portuguese)
/// </summary>
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
    private int selectedCharacterID = 0; // Default to Archer (ID: 0)
    private bool isShowing = false;
    private int currentSkinIndex = 0;
    private bool skinEquipped = false;
    private CharacterSkin[] availableSkins;
    private CharacterSkin currentSkin;

    // Filter by Role (-1 = All, 0 = Tank, 1 = Damage, 2 = Support, 3 = Control, 4 = Specialist)
    private int selectedRoleFilter = -1;

    // UI Scroll & Animation State
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

    // Interactive 3D Drag Rotation
    private bool isDraggingModel = false;
    private float lastMouseX = 0f;

    // Cached procedural textures
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

    // Role colors
    public static readonly Dictionary<CharacterData.CharacterRole, Color> RoleColors = new Dictionary<CharacterData.CharacterRole, Color>
    {
        { CharacterData.CharacterRole.Tank, new Color(0.28f, 0.60f, 0.95f) },
        { CharacterData.CharacterRole.Damage, new Color(0.95f, 0.28f, 0.20f) },
        { CharacterData.CharacterRole.Support, new Color(0.25f, 0.88f, 0.45f) },
        { CharacterData.CharacterRole.Control, new Color(0.75f, 0.35f, 0.95f) },
        { CharacterData.CharacterRole.Specialist, new Color(0.98f, 0.68f, 0.18f) }
    };

    public static string GetRoleName(CharacterData.CharacterRole role)
    {
        switch (role)
        {
            case CharacterData.CharacterRole.Tank:
                return LanguageSelectionUI.Localize("TANK", "مدرع", "ТАНК", "TANQUE", "TANK", "TANK", "टैंक", "坦克", "タンク", "TANQUE", "تانک");
            case CharacterData.CharacterRole.Damage:
                return LanguageSelectionUI.Localize("DAMAGE", "هجوم", "УРОН", "DAÑO", "DÉGÂTS", "SCHADEN", "क्षति", "伤害", "ダメージ", "DANO", "حمله");
            case CharacterData.CharacterRole.Support:
                return LanguageSelectionUI.Localize("SUPPORT", "دعم", "ПОДДЕРЖКА", "APOYO", "SOUTIEN", "UNTERSTÜTZUNG", "समर्थन", "辅助", "サポート", "SUPORTE", "پشتیبانی");
            case CharacterData.CharacterRole.Control:
                return LanguageSelectionUI.Localize("CONTROL", "تحكم", "КОНТРОЛЬ", "CONTROL", "CONTRÔLE", "KONTROLLE", "नियंत्रण", "控制", "コントロール", "CONTROLE", "کنترل");
            case CharacterData.CharacterRole.Specialist:
                return LanguageSelectionUI.Localize("SPECIALIST", "مختص", "СПЕЦИАЛИСТ", "ESPECIALISTA", "SPÉCIALISTE", "SPEZIALIST", "विशेषज्ञ", "专家", "スペシャリスト", "ESPECIALISTA", "متخصص");
            default:
                return LanguageSelectionUI.Localize("HERO", "بطل", "ГЕРОЙ", "HÉROE", "HÉROS", "HELD", "नायक", "英雄", "ヒーロー", "HERÓI", "قهرمان");
        }
    }

    public static string GetRoleEmoji(CharacterData.CharacterRole role)
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
        // Smooth Vertical gradient
        gradientTex = new Texture2D(1, 64);
        for (int y = 0; y < 64; y++)
        {
            float t = y / 63f;
            gradientTex.SetPixel(0, y, new Color(0, 0, 0, Mathf.Lerp(0.95f, 0.35f, t)));
        }
        gradientTex.Apply();
        gradientTex.wrapMode = TextureWrapMode.Clamp;

        // Radial glow disc
        glowTex = new Texture2D(32, 32);
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                float dx = (x - 15.5f) / 16f;
                float dy = (y - 15.5f) / 16f;
                float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                glowTex.SetPixel(x, y, new Color(1, 1, 1, d * d));
            }
        }
        glowTex.Apply();

        // Card background texture
        cardBgTex = new Texture2D(1, 32);
        for (int y = 0; y < 32; y++)
        {
            float t = y / 31f;
            float v = Mathf.Lerp(0.05f, 0.12f, t);
            cardBgTex.SetPixel(0, y, new Color(v * 0.7f, v, v * 0.8f, 1f));
        }
        cardBgTex.Apply();
        cardBgTex.wrapMode = TextureWrapMode.Clamp;
    }

    // =========================================================================
    // 3D PREVIEW STUDIO (TURNTABLE + DUAL LIGHTING + HIGH-RES CAMERA)
    // =========================================================================
    void SetupPreviewStudio()
    {
        if (previewStudioRoot != null) return;

        previewStudioRoot = new GameObject("ChampionPreviewStudio");
        previewStudioRoot.transform.position = new Vector3(STUDIO_X, STUDIO_Y, STUDIO_Z);

        // High-Definition Render Texture (1024x1024)
        previewRenderTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
        previewRenderTexture.Create();

        // Studio Camera
        GameObject camObj = new GameObject("PreviewCamera");
        camObj.transform.SetParent(previewStudioRoot.transform, false);
        camObj.transform.localPosition = new Vector3(0f, 1.05f, -2.65f);
        camObj.transform.localRotation = Quaternion.Euler(6.5f, 0f, 0f);

        previewCamera = camObj.AddComponent<Camera>();
        previewCamera.targetTexture = previewRenderTexture;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.015f, 0.035f, 0.025f, 1f);
        previewCamera.fieldOfView = 34f;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 30f;
        previewCamera.enabled = true;

        // Key Studio Light (Warm Solar Gold)
        GameObject lightObj = new GameObject("StudioKeyLight");
        lightObj.transform.SetParent(previewStudioRoot.transform, false);
        lightObj.transform.localPosition = new Vector3(1.6f, 3.8f, -2.2f);
        lightObj.transform.localRotation = Quaternion.Euler(42f, -32f, 0f);
        Light keyLight = lightObj.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(1f, 0.96f, 0.86f);
        keyLight.intensity = 1.45f;

        // Rim Light (Sacred Emerald Glow)
        GameObject rimObj = new GameObject("StudioRimLight");
        rimObj.transform.SetParent(previewStudioRoot.transform, false);
        rimObj.transform.localPosition = new Vector3(-2.2f, 2.4f, 2.2f);
        rimObj.transform.localRotation = Quaternion.Euler(28f, 138f, 0f);
        Light rimLight = rimObj.AddComponent<Light>();
        rimLight.type = LightType.Directional;
        rimLight.color = new Color(0.22f, 0.92f, 0.48f);
        rimLight.intensity = 0.95f;

        // Turntable Root
        GameObject ttObj = new GameObject("Turntable");
        ttObj.transform.SetParent(previewStudioRoot.transform, false);
        ttObj.transform.localPosition = Vector3.zero;
        turntable = ttObj.transform;

        // Sacred Pedestal Dais
        GameObject dais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dais.name = "SacredDais";
        dais.transform.SetParent(turntable, false);
        dais.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        dais.transform.localScale = new Vector3(1.75f, 0.04f, 1.75f);
        Collider dCol = dais.GetComponent<Collider>();
        if (dCol != null) Destroy(dCol);
        Renderer dRend = dais.GetComponent<Renderer>();
        if (dRend != null)
        {
            dRend.material.color = new Color(0.08f, 0.14f, 0.10f);
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
            currentPreviewModel.transform.localPosition = Vector3.zero;
            currentPreviewModel.transform.localRotation = Quaternion.identity;
            currentPreviewModel.transform.localScale = Vector3.one * 1.08f;

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

                // Attach 3D Bow if not present
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

        // Live sync with player standing outside in the 3D scene
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
            selectedCharacterID = 0; // Default to Archer

        hoveredCharacterID = -1;
        selectedRoleFilter = -1; // Default to ALL
        currentSkinIndex = 0;
        skinEquipped = false;
        hoverAnimTime = 0f;
        selectionAnimTime = 0f;
        isDraggingModel = false;

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
        isDraggingModel = false;
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

        // Turntable smooth auto-rotation when not interactively dragging
        if (!isDraggingModel && turntable != null)
        {
            turntable.Rotate(Vector3.up * 26f * Time.deltaTime, Space.World);
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

        // 1. Full Screen Translucent Ambient Background
        DrawBackground(sw, sh);

        // 2. Top Header (Back Button, Title, Subtitle)
        DrawHeader(sw, sh);

        // 3. Top Role Filter Ribbon (ALL, TANK, DAMAGE, SUPPORT, CONTROL, SPECIALIST)
        float filterY = sh * 0.075f;
        float filterH = 32f;
        DrawRoleFilterRibbon(new Rect(sw * 0.02f, filterY, sw * 0.96f, filterH));

        // 4. AAA 3-Column Layout:
        // Left: Hero Roster Gallery (28% width)
        // Center: Grand 3D Hero Altar Showcase (41% width)
        // Right: Combat Codex, Sacred Wardrobe & Confirm Button (28% width)
        float contentY = sh * 0.125f;
        float contentH = sh * 0.845f;
        float pad = sw * 0.015f;
        float leftW = sw * 0.28f;
        float centerW = sw * 0.40f;
        float rightW = sw * 0.28f;

        float leftX = pad;
        float centerX = leftX + leftW + pad * 0.5f;
        float rightX = centerX + centerW + pad * 0.5f;

        // Active Display Character
        int displayID = (hoveredCharacterID >= 0) ? hoveredCharacterID : (selectedCharacterID >= 0 ? selectedCharacterID : 0);
        CharacterData displayChar = (database != null) ? database.GetCharacter(displayID) : null;
        if (displayChar == null && database != null && database.characters != null && database.characters.Length > 0)
            displayChar = database.characters[0];

        // --- Column 1: Left Hero Roster ---
        DrawCharacterGrid(new Rect(leftX, contentY, leftW, contentH));

        // --- Column 2: Center Grand 3D Hero Altar ---
        if (displayChar != null)
        {
            DrawCenterHeroStage(new Rect(centerX, contentY, centerW, contentH), displayChar);
        }

        // --- Column 3: Right Combat Codex & Wardrobe ---
        if (displayChar != null)
        {
            DrawCharacterDetails(new Rect(rightX, contentY, rightW, contentH), displayChar);
        }
    }

    // ==================== BACKGROUND ====================
    void DrawBackground(float sw, float sh)
    {
        // Translucent deep obsidian-emerald haze
        GUI.color = new Color(0.018f, 0.045f, 0.030f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

        GUI.color = new Color(0.04f, 0.15f, 0.08f, 0.28f);
        GUI.DrawTexture(new Rect(0, sh * 0.25f, sw, sh * 0.75f), gradientTex, ScaleMode.StretchToFill);

        // Header and Footer obsidian bars
        GUI.color = new Color(0.01f, 0.02f, 0.015f, 0.95f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh * 0.072f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, sh * 0.975f, sw, sh * 0.025f), Texture2D.whiteTexture);

        // Radiant antique gold trim line below header
        float pulse = 0.7f + 0.3f * Mathf.Sin(headerPulse);
        GUI.color = new Color(0.85f * pulse, 0.70f * pulse, 0.25f * pulse, 0.9f);
        GUI.DrawTexture(new Rect(sw * 0.02f, sh * 0.072f, sw * 0.96f, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // ==================== HEADER ====================
    void DrawHeader(float sw, float sh)
    {
        // Top-left Back button returning to Team Selection
        float backW = Mathf.Max(150f, sw * 0.13f);
        float backH = 30f;
        float backX = sw * 0.02f;
        float backY = sh * 0.016f;

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
            pt: "⬅️ VOLTAR AOS TIMES",
            fa: "⬅️ بازگشت به انتخاب تیم"
        );

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(backX, backY, backW, backH), backText, new Color(0.85f, 0.45f, 0.35f), 11))
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

        // Center Title & Lore Subtitle
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.Max(15, (int)(sw * 0.018f));
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        float glow = 0.88f + 0.12f * Mathf.Sin(headerPulse);
        titleStyle.normal.textColor = new Color(1.0f * glow, 0.86f * glow, 0.38f, 1f);

        string titleStr = LanguageSelectionUI.Localize(
            "✦ CHOOSE YOUR SACRED CHAMPION ✦",
            ar: "✦ اختر بطلك المقدس ✦",
            ru: "✦ ВЫБЕРИТЕ ВАШЕГО ЧЕМПИОНА ✦",
            fr: "✦ CHOISISSEZ VOTRE CHAMPION SACRÉ ✦",
            es: "✦ ELIGE A TU CAMPEÓN SAGRADO ✦",
            de: "✦ WÄHLE DEINEN HEILIGEN HELDEN ✦",
            hi: "✦ अपने पवित्र नायक को चुनें ✦",
            zh: "✦ 选择你的神圣英雄 ✦",
            ja: "✦ 聖なる英雄を選択せよ ✦",
            pt: "✦ ESCOLHA SEU CAMPEÃO SAGRADO ✦",
            fa: "✦ قهرمان مقدس خود را انتخاب کنید ✦"
        );
        GUI.Label(new Rect(0, sh * 0.010f, sw, sh * 0.034f), titleStr, titleStyle);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = Mathf.Max(9, (int)(sw * 0.0095f));
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.normal.textColor = new Color(0.65f, 0.88f, 0.72f, 0.85f);
        subStyle.fontStyle = FontStyle.Italic;

        string subStr = LanguageSelectionUI.Localize(
            "Inspect your Guardian's 3D armor, sacred powers, and combat stats",
            ar: "مرر الفأرة أو انقر على أي حارس لفحص دروعه وقدراته وإحصائياته",
            ru: "Наведите курсор для просмотра брони и характеристик",
            fr: "Survolez ou cliquez pour inspecter l'armure et les stats",
            es: "Pasa el cursor para ver armadura y atributos",
            de: "Untersuche Rüstung, Kräfte und Kampfwerte",
            hi: "कवच, शक्ति और आँकड़े देखने के लिए क्लिक करें",
            zh: "查看守护者的3D装甲、神圣力量与战斗属性",
            ja: "鎧、能力、ステータスを確認せよ",
            pt: "Inspecione a armadura 3D, poderes e atributos de combate",
            fa: "برای بررسی زره، قدرت‌ها و مشخصات رزمی کلیک یا هاور کنید"
        );
        GUI.Label(new Rect(0, sh * 0.042f, sw, sh * 0.024f), subStr, subStyle);
    }

    // ==================== ROLE FILTER RIBBON ====================
    void DrawRoleFilterRibbon(Rect rect)
    {
        int[] filterRoles = new int[] { -1, 0, 1, 2, 3, 4 };
        string[] filterLabels = new string[]
        {
            LanguageSelectionUI.Localize("ALL (25)", "الكل (25)", "ВСЕ (25)", "TOUS (25)", "TODOS (25)", "ALLE (25)", "सभी (25)", "全部 (25)", "すべて (25)", "TODOS (25)", "همه (25)"),
            "🛡️ " + LanguageSelectionUI.Localize("TANK (5)", "مدرع (5)", "ТАНК (5)", "TANK (5)", "TANQUE (5)", "TANK (5)", "टैंक (5)", "坦克 (5)", "タンク (5)", "TANQUE (5)", "تانک (5)"),
            "⚔️ " + LanguageSelectionUI.Localize("DAMAGE (7)", "هجوم (7)", "УРОН (7)", "DÉGÂTS (7)", "DAÑO (7)", "SCHADEN (7)", "क्षति (7)", "伤害 (7)", "ダメージ (7)", "DANO (7)", "حمله (7)"),
            "💚 " + LanguageSelectionUI.Localize("SUPPORT (4)", "دعم (4)", "ПОДДЕРЖКА (4)", "SOUTIEN (4)", "APOYO (4)", "UNTERSTÜTZUNG (4)", "समर्थن (4)", "辅助 (4)", "サポート (4)", "SUPORTE (4)", "پشتیبانی (4)"),
            "✨ " + LanguageSelectionUI.Localize("CONTROL (4)", "تحكم (4)", "КОНТРОЛЬ (4)", "CONTRÔLE (4)", "CONTROL (4)", "KONTROLLE (4)", "नियंत्रण (4)", "控制 (4)", "コントロール (4)", "CONTROLE (4)", "کنترل (4)"),
            "🔮 " + LanguageSelectionUI.Localize("SPECIALIST (5)", "مختص (5)", "СПЕЦИАЛИСТ (5)", "SPÉCIALISTE (5)", "ESPECIALISTA (5)", "SPEZIALIST (5)", "विशेषज्ञ (5)", "专家 (5)", "スペシャリスト (5)", "ESPECIALISTA (5)", "متخصص (5)")
        };

        float btnW = (rect.width - 10f * (filterRoles.Length - 1)) / filterRoles.Length;
        for (int i = 0; i < filterRoles.Length; i++)
        {
            int r = filterRoles[i];
            bool isActive = (selectedRoleFilter == r);
            Rect bRect = new Rect(rect.x + i * (btnW + 10f), rect.y, btnW, rect.height);

            Color tabColor = isActive
                ? new Color(1.0f, 0.85f, 0.35f)
                : new Color(0.35f, 0.85f, 0.55f);

            if (EnchantedNatureGUI.DrawOrganicLeafButton(bRect, filterLabels[i], tabColor, 11))
            {
                selectedRoleFilter = r;
            }
        }
    }

    // ==================== COLUMN 1: LEFT HERO ROSTER ====================
    void DrawCharacterGrid(Rect area)
    {
        if (database == null || database.characters == null) return;

        // Obsidian Glass Frame with Antique Gold Filigree
        EnchantedNatureGUI.DrawAntiqueGoldFrame(area, new Color(0.24f, 0.85f, 0.45f));

        float pad = 10f;
        float headerH = 34f;

        // Title Header
        GUIStyle gridTitle = new GUIStyle(GUI.skin.label);
        gridTitle.fontSize = Mathf.Max(11, (int)(area.width * 0.044f));
        gridTitle.fontStyle = FontStyle.Bold;
        gridTitle.alignment = TextAnchor.MiddleLeft;
        gridTitle.normal.textColor = new Color(0.70f, 0.95f, 0.65f, 0.95f);

        string rosterTitle = LanguageSelectionUI.Localize("GUARDIANS OF THE WILD", "حراس البرية", "СТРАЖИ ДИКОЙ ПРИРОДЫ", "GARDIENS DE LA NATURE", "GUARDIANES DE LA NATURALEZA", "WÄCHTER DER WILDNIS", "जंगल के संरक्षक", "荒野守护者", "大自然の守護者", "GUARDIÕES DA NATUREZA", "نگهبانان طبیعت وحشی");
        GUI.Label(new Rect(area.x + pad, area.y + 6f, area.width * 0.65f, 24f), rosterTitle, gridTitle);

        // Filter Matching Characters
        List<CharacterData> filteredList = new List<CharacterData>();
        for (int i = 0; i < database.characters.Length; i++)
        {
            CharacterData c = database.characters[i];
            if (c == null) continue;
            if (selectedRoleFilter < 0 || (int)c.role == selectedRoleFilter)
            {
                filteredList.Add(c);
            }
        }

        GUIStyle countStyle = new GUIStyle(GUI.skin.label);
        countStyle.fontSize = Mathf.Max(9, (int)(area.width * 0.032f));
        countStyle.alignment = TextAnchor.MiddleRight;
        countStyle.normal.textColor = new Color(0.85f, 0.78f, 0.35f, 0.85f);
        GUI.Label(new Rect(area.x, area.y + 8f, area.width - pad, 20f), $"{filteredList.Count} " + LanguageSelectionUI.Localize("Ready", "جاهزون", "готовы", "Prêts", "Listos", "Bereit", "तैयार", "就绪", "待機中", "Prontos", "آماده"), countStyle);

        // Separator line
        GUI.color = new Color(0.35f, 0.65f, 0.40f, 0.45f);
        GUI.DrawTexture(new Rect(area.x + pad, area.y + headerH, area.width - pad * 2, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Scrollable 2-Column Monolith Cards
        float scrollY = area.y + headerH + 4f;
        float scrollH = area.height - headerH - 8f;
        float innerW = area.width - pad * 2f;

        const int COLUMNS = 2;
        float cardSpacing = 6f;
        float cardW = (innerW - cardSpacing) / COLUMNS;
        float cardH = cardW * 1.25f;

        int totalRows = Mathf.CeilToInt((float)filteredList.Count / COLUMNS);
        float totalVirtualH = totalRows * (cardH + cardSpacing) + 12f;

        gridScrollPosition = GUI.BeginScrollView(
            new Rect(area.x + pad, scrollY, innerW + 16f, scrollH),
            gridScrollPosition,
            new Rect(0, 0, innerW, totalVirtualH)
        );

        int frameHovered = -1;

        for (int i = 0; i < filteredList.Count; i++)
        {
            CharacterData ch = filteredList[i];
            if (ch == null) continue;

            int col = i % COLUMNS;
            int row = i / COLUMNS;

            float cx = col * (cardW + cardSpacing);
            float cy = row * (cardH + cardSpacing);

            Rect cardRect = new Rect(cx, cy, cardW, cardH);
            bool isSelected = (selectedCharacterID == ch.characterID);
            bool isHovered = cardRect.Contains(Event.current.mousePosition);

            if (isHovered) frameHovered = ch.characterID;

            float animT = (cardAnimTimers != null && ch.characterID < cardAnimTimers.Length) ? cardAnimTimers[ch.characterID] : 0f;

            DrawCharacterCard(cardRect, ch, isSelected, isHovered, animT);

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

    // ==================== MONOLITH CHARACTER CARD ====================
    void DrawCharacterCard(Rect rect, CharacterData ch, bool isSelected, bool isHovered, float animT)
    {
        Color roleColor = RoleColors.ContainsKey(ch.role) ? RoleColors[ch.role] : Color.gray;

        float lift = animT * 3f;
        Rect drawRect = new Rect(rect.x, rect.y - lift, rect.width, rect.height);

        // Soft drop shadow on hover
        if (animT > 0.01f)
        {
            GUI.color = new Color(0, 0, 0, 0.45f * animT);
            GUI.DrawTexture(new Rect(drawRect.x + 2, drawRect.y + drawRect.height - 1, drawRect.width, 3), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Obsidian Glass Body
        float bgBright = Mathf.Lerp(0.045f, 0.12f, animT);
        Color bgCol = isSelected
            ? new Color(roleColor.r * 0.22f, roleColor.g * 0.22f, roleColor.b * 0.22f, 0.98f)
            : new Color(bgBright * 0.7f, bgBright, bgBright * 0.8f, 0.94f);
        GUI.color = bgCol;
        GUI.DrawTexture(drawRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Crest Area
        float crestH = drawRect.height * 0.54f;
        Rect crestRect = new Rect(drawRect.x, drawRect.y, drawRect.width, crestH);

        // Top role gradient banner
        GUI.color = new Color(roleColor.r * 0.35f, roleColor.g * 0.35f, roleColor.b * 0.35f, 0.75f);
        GUI.DrawTexture(crestRect, gradientTex, ScaleMode.StretchToFill);
        GUI.color = Color.white;

        // Class Heraldry Emoji & Glow
        GUIStyle iconStyle = new GUIStyle(GUI.skin.label);
        iconStyle.fontSize = Mathf.Max(18, (int)(crestH * 0.46f));
        iconStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(crestRect, GetRoleEmoji(ch.role), iconStyle);

        // Role Separator Trim
        GUI.color = new Color(roleColor.r, roleColor.g, roleColor.b, 0.9f);
        GUI.DrawTexture(new Rect(drawRect.x, drawRect.y + crestH, drawRect.width, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Hero Name & Role Tag Area
        float textY = drawRect.y + crestH + 4f;
        GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.fontSize = Mathf.Max(9, (int)(drawRect.width * 0.13f));
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.alignment = TextAnchor.UpperCenter;
        nameStyle.normal.textColor = isSelected ? new Color(1f, 0.92f, 0.45f) : new Color(0.90f, 0.95f, 0.88f);

        string localizedCharName = LanguageSelectionUI.Localize(ch.characterName);
        GUI.Label(new Rect(drawRect.x + 2, textY, drawRect.width - 4, 18f), localizedCharName, nameStyle);

        GUIStyle roleStyle = new GUIStyle(GUI.skin.label);
        roleStyle.fontSize = Mathf.Max(7, (int)(drawRect.width * 0.095f));
        roleStyle.alignment = TextAnchor.UpperCenter;
        roleStyle.normal.textColor = new Color(roleColor.r, roleColor.g, roleColor.b, 0.95f);
        GUI.Label(new Rect(drawRect.x + 2, textY + 16f, drawRect.width - 4, 14f), GetRoleName(ch.role), roleStyle);

        // Mini Stat Preview Pips (Health & Damage)
        float barY = drawRect.y + drawRect.height - 12f;
        float barH = 3f;
        float barW = drawRect.width * 0.76f;
        float barX = drawRect.x + drawRect.width * 0.12f;

        float hpNorm = Mathf.Clamp01(ch.maxHealth / 400f);
        GUI.color = new Color(0.08f, 0.15f, 0.08f, 0.7f);
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);
        GUI.color = new Color(0.25f, 0.85f, 0.35f, 0.9f);
        GUI.DrawTexture(new Rect(barX, barY, barW * hpNorm, barH), Texture2D.whiteTexture);

        float dmgNorm = Mathf.Clamp01(ch.attackDamage / 40f);
        GUI.color = new Color(0.08f, 0.15f, 0.08f, 0.7f);
        GUI.DrawTexture(new Rect(barX, barY + 4f, barW, barH), Texture2D.whiteTexture);
        GUI.color = new Color(0.95f, 0.30f, 0.20f, 0.9f);
        GUI.DrawTexture(new Rect(barX, barY + 4f, barW * dmgNorm, barH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Selection & Hover Borders
        if (isSelected)
        {
            float selPulse = 0.8f + 0.2f * Mathf.Sin(selectionAnimTime * 2.8f);
            Color selBorder = new Color(roleColor.r * selPulse, roleColor.g * selPulse, roleColor.b * selPulse, 0.95f);
            DrawBorder(drawRect, selBorder, 2f);

            // Diamond studs in corners
            EnchantedNatureGUI.DrawCornerFiligree(drawRect, new Color(1f, 0.88f, 0.4f, selPulse), 10f);
        }
        else if (isHovered)
        {
            Color hoverBorder = new Color(0.5f, 0.9f, 0.6f, 0.75f + 0.25f * Mathf.Sin(hoverAnimTime));
            DrawBorder(drawRect, hoverBorder, 1.5f);
        }
        else
        {
            DrawBorder(drawRect, new Color(roleColor.r * 0.45f, roleColor.g * 0.45f, roleColor.b * 0.45f, 0.4f), 1f);
        }
    }

    // ==================== COLUMN 2: CENTER GRAND 3D HERO STAGE ====================
    void DrawCenterHeroStage(Rect area, CharacterData ch)
    {
        Color roleColor = RoleColors.ContainsKey(ch.role) ? RoleColors[ch.role] : Color.gray;

        // Antique Gold Frame for Center Stage
        EnchantedNatureGUI.DrawAntiqueGoldFrame(area, roleColor);

        // 3D Render Viewport inside stage
        float stagePad = 6f;
        Rect renderRect = new Rect(area.x + stagePad, area.y + stagePad, area.width - stagePad * 2, area.height - stagePad * 2);

        if (previewRenderTexture != null)
        {
            GUI.DrawTexture(renderRect, previewRenderTexture, ScaleMode.ScaleToFit);
        }

        // Interactive Mouse Drag for 360-Degree Turntable Rotation
        Event e = Event.current;
        if (renderRect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                isDraggingModel = true;
                lastMouseX = e.mousePosition.x;
            }
        }
        if (e.rawType == EventType.MouseUp || e.type == EventType.MouseUp)
        {
            isDraggingModel = false;
        }
        if (isDraggingModel && e.type == EventType.MouseDrag && turntable != null)
        {
            float deltaX = e.mousePosition.x - lastMouseX;
            lastMouseX = e.mousePosition.x;
            turntable.Rotate(Vector3.up * -deltaX * 0.65f, Space.World);
        }

        // Top Floating Hero Title Watermark
        string locName = LanguageSelectionUI.Localize(ch.characterName).ToUpper();
        string titleWatermark = $"✦ {locName} ✦";

        GUIStyle wmStyle = new GUIStyle(GUI.skin.label);
        wmStyle.fontSize = Mathf.Max(16, (int)(area.width * 0.065f));
        wmStyle.fontStyle = FontStyle.Bold;
        wmStyle.alignment = TextAnchor.MiddleCenter;
        wmStyle.normal.textColor = new Color(1.0f, 0.90f, 0.45f, 0.95f);
        GUI.Label(new Rect(area.x, area.y + 14f, area.width, 30f), titleWatermark, wmStyle);

        // Floating Role Insignia Plaque below title
        string roleTag = GetRoleEmoji(ch.role) + "  " + GetRoleName(ch.role);
        float badgeW = Mathf.Min(190f, area.width * 0.52f);
        float badgeH = 22f;
        Rect badgeRect = new Rect(area.x + (area.width - badgeW) / 2f, area.y + 46f, badgeW, badgeH);

        GUI.color = new Color(roleColor.r * 0.25f, roleColor.g * 0.25f, roleColor.b * 0.25f, 0.88f);
        GUI.DrawTexture(badgeRect, Texture2D.whiteTexture);
        DrawBorder(badgeRect, new Color(roleColor.r, roleColor.g, roleColor.b, 0.85f), 1.2f);
        GUI.color = Color.white;

        GUIStyle roleBadgeStyle = new GUIStyle(GUI.skin.label);
        roleBadgeStyle.fontSize = Mathf.Max(10, (int)(area.width * 0.035f));
        roleBadgeStyle.fontStyle = FontStyle.Bold;
        roleBadgeStyle.alignment = TextAnchor.MiddleCenter;
        roleBadgeStyle.normal.textColor = Color.white;
        GUI.Label(badgeRect, roleTag, roleBadgeStyle);

        // Bottom Drag-to-Rotate Hint
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = Mathf.Max(8, (int)(area.width * 0.026f));
        hintStyle.alignment = TextAnchor.LowerCenter;
        hintStyle.normal.textColor = new Color(0.65f, 0.85f, 0.70f, 0.75f);
        hintStyle.fontStyle = FontStyle.Italic;

        string hintStr = LanguageSelectionUI.Localize(
            "✦ Drag mouse horizontally to rotate champion ✦",
            ar: "✦ اسحب بالفأرة أفقياً لتدوير البطل ✦",
            ru: "✦ Перетащите мышь для вращения героя ✦",
            fr: "✦ Glissez la souris pour faire pivoter ✦",
            es: "✦ Arrastra el ratón para rotar al héroe ✦",
            de: "✦ Ziehen zum Drehen des Helden ✦",
            hi: "✦ घुमाने के लिए माउस खींचें ✦",
            zh: "✦ 按住鼠标水平拖动以旋转英雄 ✦",
            ja: "✦ ドラッグして英雄を回転 ✦",
            pt: "✦ Arraste o mouse para girar o campeão ✦",
            fa: "✦ برای چرخاندن قهرمان، ماوس را بکشید ✦"
        );
        GUI.Label(new Rect(area.x, area.y + area.height - 24f, area.width, 20f), hintStr, hintStyle);
    }

    // ==================== COLUMN 3: RIGHT COMBAT CODEX & SACRED SKINS ====================
    void DrawCharacterDetails(Rect area, CharacterData ch)
    {
        Color roleColor = RoleColors.ContainsKey(ch.role) ? RoleColors[ch.role] : Color.gray;

        // Frame
        EnchantedNatureGUI.DrawAntiqueGoldFrame(area, roleColor);

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

        float pad = 10f;

        // Pinned Header: CODEX & POWERS
        float headerH = 34f;
        GUIStyle codexTitle = new GUIStyle(GUI.skin.label);
        codexTitle.fontSize = Mathf.Max(11, (int)(area.width * 0.044f));
        codexTitle.fontStyle = FontStyle.Bold;
        codexTitle.alignment = TextAnchor.MiddleCenter;
        codexTitle.normal.textColor = new Color(1.0f, 0.88f, 0.40f);

        string codexTitleStr = LanguageSelectionUI.Localize(
            "CHAMPION COMBAT STATS",
            ar: "إحصائيات القتال للبطل",
            ru: "БОЕВЫЕ ХАРАКТЕРИСТИКИ",
            fr: "STATS DE COMBAT DU CHAMPION",
            es: "ESTADÍSTICAS DE COMBATE",
            de: "KAMPFWERTE DES HELDEN",
            hi: "नायक मुकाबला आँकड़े",
            zh: "英雄战斗属性",
            ja: "戦闘ステータス",
            pt: "ATRIBUTOS DE COMBATE",
            fa: "مشخصات رزمی قهرمان"
        );
        GUI.Label(new Rect(area.x + pad, area.y + 6f, area.width - pad * 2, 24f), codexTitleStr, codexTitle);

        // Separator
        GUI.color = new Color(0.35f, 0.65f, 0.40f, 0.45f);
        GUI.DrawTexture(new Rect(area.x + pad, area.y + headerH, area.width - pad * 2, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Pinned Bottom Section: Grand Expedition Confirmation Gateway (Height 76px)
        float footerH = 76f;
        float footerY = area.y + area.height - footerH - 4f;

        // Separator above footer
        GUI.color = new Color(0.35f, 0.65f, 0.40f, 0.45f);
        GUI.DrawTexture(new Rect(area.x + pad, footerY - 2f, area.width - pad * 2, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Player Economy Status
        int curCoins = PlayerProfile.Instance != null ? PlayerProfile.Instance.Coins : 0;
        int curGems = PlayerProfile.Instance != null ? PlayerProfile.Instance.Gems : 0;
        int curLevel = PlayerProfile.Instance != null ? PlayerProfile.Instance.AccountLevel : 1;

        GUIStyle ecoStyle = new GUIStyle(GUI.skin.label);
        ecoStyle.fontSize = Mathf.Max(8, (int)(area.width * 0.026f));
        ecoStyle.alignment = TextAnchor.MiddleCenter;
        ecoStyle.fontStyle = FontStyle.Bold;

        if (skinNotificationTimer > 0f)
        {
            ecoStyle.normal.textColor = new Color(0.35f, 1f, 0.5f);
            GUI.Label(new Rect(area.x + pad, footerY + 2f, area.width - pad * 2, 16f), skinNotification, ecoStyle);
        }
        else
        {
            ecoStyle.normal.textColor = new Color(0.92f, 0.88f, 0.7f);
            string ecoStr = $"🪙 {curCoins} Coins   |   💎 {curGems} Gems   |   ⭐ Lv.{curLevel}";
            GUI.Label(new Rect(area.x + pad, footerY + 2f, area.width - pad * 2, 16f), ecoStr, ecoStyle);
        }

        // Pinned Grand Confirmation Button
        float confirmH = 46f;
        Rect confirmRect = new Rect(area.x + pad, footerY + 22f, area.width - pad * 2, confirmH);
        float confirmPulse = 0.85f + 0.15f * Mathf.Sin(selectionAnimTime * 2.8f);

        string locChar = LanguageSelectionUI.Localize(ch.characterName).ToUpper();
        string buttonText = "🌿 " + LanguageSelectionUI.Localize(
            "COMMENCE EXPEDITION",
            ar: "انطلق في الحملة",
            ru: "НАЧАТЬ ПОХОД",
            fr: "COMMENCER L'EXPÉDITION",
            es: "INICIAR EXPEDICIÓN",
            de: "EXPEDITION STARTEN",
            hi: "अभियान शुरू करें",
            zh: "开始远征",
            ja: "遠征を開始",
            pt: "INICIAR EXPEDIÇÃO",
            fa: "آغاز نبرد"
        ) + $" ({locChar}) 🌿";

        if (EnchantedNatureGUI.DrawOrganicLeafButton(confirmRect, buttonText, new Color(0.25f, 0.95f, 0.45f), 12))
        {
            selectedCharacterID = ch.characterID;
            ConfirmSelection();
        }

        // Middle Scrollable Area: Combat Stats + 5 Sacred Skins
        float scrollY = area.y + headerH + 2f;
        float scrollH = footerY - scrollY - 2f;
        Rect scrollRect = new Rect(area.x + 4f, scrollY, area.width - 8f, scrollH);

        float innerW = scrollRect.width - 16f;
        float totalVirtualH = 750f;
        Rect viewRect = new Rect(0, 0, innerW, totalVirtualH);

        detailsScrollPosition = GUI.BeginScrollView(scrollRect, detailsScrollPosition, viewRect, false, true);

        float curY = 4f;

        // 1. COMBAT ATTRIBUTE BARS
        float statBarH = 14f;
        float statGap = 4f;
        int statFontSize = Mathf.Max(8, (int)(innerW * 0.034f));

        string healthBonus = (skinEquipped && currentSkin != null && currentSkin.healthMultiplier != 1f)
            ? $" (+{(currentSkin.healthMultiplier - 1f) * 100:F0}%)" : "";
        DrawStatBar(new Rect(0, curY, innerW, statBarH),
            LanguageSelectionUI.Localize("HP", "الصحة", "ЗДОРОВЬЕ", "SALUD", "VIE", "LEBEN", "स्वास्थ्य", "生命", "HP", "VIDA", "سلامت") + "  " + ch.maxHealth + healthBonus,
            ch.maxHealth / 400f,
            new Color(0.25f, 0.85f, 0.35f), statFontSize);
        curY += statBarH + statGap;

        string dmgBonus = (skinEquipped && currentSkin != null && currentSkin.damageMultiplier != 1f)
            ? $" (+{(currentSkin.damageMultiplier - 1f) * 100:F0}%)" : "";
        DrawStatBar(new Rect(0, curY, innerW, statBarH),
            LanguageSelectionUI.Localize("ATK", "الهجوم", "АТАКА", "ATAQUE", "ATTAQUE", "ANGRIFF", "हमला", "攻击", "攻撃", "ATAQUE", "حمله") + "  " + ch.attackDamage + dmgBonus,
            ch.attackDamage / 40f,
            new Color(0.95f, 0.30f, 0.20f), statFontSize);
        curY += statBarH + statGap;

        string spdBonus = (skinEquipped && currentSkin != null && currentSkin.speedMultiplier != 1f)
            ? $" (+{(currentSkin.speedMultiplier - 1f) * 100:F0}%)" : "";
        DrawStatBar(new Rect(0, curY, innerW, statBarH),
            LanguageSelectionUI.Localize("SPD", "السرعة", "СКОРОСТЬ", "VELOCIDAD", "VITESSE", "TEMPO", "गति", "速度", "速度", "VELOCIDADE", "سرعت") + "  " + ch.moveSpeed + spdBonus,
            ch.moveSpeed / 12f,
            new Color(0.30f, 0.75f, 0.95f), statFontSize);
        curY += statBarH + statGap;

        DrawStatBar(new Rect(0, curY, innerW, statBarH),
            LanguageSelectionUI.Localize("RNG", "المدى", "ДАЛЬНОСТЬ", "RANGO", "PORTÉE", "REICHWEITE", "सीमा", "范围", "射程", "ALCANCE", "برد") + "  " + ch.attackRange + "m",
            ch.attackRange / 20f,
            new Color(0.95f, 0.75f, 0.25f), statFontSize);
        curY += statBarH + statGap;

        DrawStatBar(new Rect(0, curY, innerW, statBarH),
            LanguageSelectionUI.Localize("COOLDOWN", "وقت الانتظار", "ПЕРЕЗАРЯДКА", "RECARGA", "RECHARGE", "ABKLINGZEIT", "कूलडाउन", "冷却", "クールダウン", "RECARGA", "زمان خنک‌شدن") + "  " + ch.attackCooldown.ToString("F1") + "s",
            1f - (ch.attackCooldown / 3f),
            new Color(0.75f, 0.55f, 0.95f), statFontSize);
        curY += statBarH + statGap;

        string doubleJumpLabel = LanguageSelectionUI.Localize("(Double Jump)", "(قفز مزدوج)", "(Двойной прыжок)", "(Salto doble)", "(Double saut)", "(Doppelsprung)", "(दोहरी छलांग)", "(二段跳)", "(二段ジャンプ)", "(Pulo Duplo)", "(پرش دوگانه)");
        string jumpStr = ch.canDoubleJump ? ch.jumpForce + " " + doubleJumpLabel : ch.jumpForce.ToString();
        DrawStatBar(new Rect(0, curY, innerW, statBarH),
            LanguageSelectionUI.Localize("JUMP", "القفز", "ПРЫЖОК", "SALTO", "SAUT", "SPRUNG", "छलांग", "跳跃", "ジャンプ", "SALTO", "پرش") + "  " + jumpStr,
            ch.jumpForce / 16f,
            new Color(0.55f, 0.95f, 0.75f), statFontSize);
        curY += statBarH + 6f;

        // Attack Style label
        GUIStyle atkTypeStyle = new GUIStyle(GUI.skin.label);
        atkTypeStyle.fontSize = statFontSize;
        atkTypeStyle.alignment = TextAnchor.MiddleLeft;
        atkTypeStyle.normal.textColor = new Color(0.75f, 0.88f, 0.70f, 0.90f);
        string atkLabel = LanguageSelectionUI.Localize("Attack Style: ", "أسلوب الهجوم: ", "Стиль атаки: ", "Estilo de ataque: ", "Style d'attaque : ", "Angriffsstil: ", "हमले की शैली: ", "攻击方式: ", "攻撃スタイル: ", "Estilo de Ataque: ", "سبک حمله: ") + GetAttackTypeLabel(ch.attackType);
        GUI.Label(new Rect(0, curY, innerW, 18), atkLabel, atkTypeStyle);
        curY += 22f;

        // Separator
        GUI.color = new Color(0.35f, 0.65f, 0.40f, 0.35f);
        GUI.DrawTexture(new Rect(0, curY, innerW, 1.5f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        curY += 8f;

        // 2. SACRED SKINS & POWER UPGRADES SECTION
        GUIStyle skinsHeader = new GUIStyle(GUI.skin.label);
        skinsHeader.fontSize = Mathf.Max(10, (int)(innerW * 0.040f));
        skinsHeader.fontStyle = FontStyle.Bold;
        skinsHeader.normal.textColor = new Color(1.0f, 0.88f, 0.40f);

        string skinsTitle = LanguageSelectionUI.Localize(
            "👑 SACRED SKINS & POWER UPGRADES",
            ar: "👑 المظاهر والأسلحة والترقيات المقدسة",
            ru: "👑 СВЯЩЕННЫЕ ОБЛИКИ И УЛУЧШЕНИЯ",
            fr: "👑 SKINS SACRÉS ET AMÉLIORATIONS",
            es: "👑 ASPECTOS SAGRADOS Y MEJORAS",
            de: "👑 HEILIGE SKINS & UPGRADES",
            hi: "👑 पवित्र खाल और शक्ति संवर्धन",
            zh: "👑 神圣皮肤与战力升级",
            ja: "👑 聖なるスキンと能力強化",
            pt: "👑 SKINS SAGRADAS E MELHORIAS",
            fa: "👑 پوسته‌ها و ارتقاهای مقدس"
        );
        GUI.Label(new Rect(0, curY, innerW, 20), skinsTitle, skinsHeader);
        curY += 22f;

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
                    ? new Color(0.35f, 0.98f, 0.50f, 0.95f)
                    : (isUnlocked ? new Color(0.4f, 0.7f, 0.5f, 0.6f) : new Color(0.35f, 0.35f, 0.35f, 0.5f));
                DrawBorder(cardR, borderCol, isEquipped ? 2f : 1f);
                GUI.color = Color.white;

                // Left: Skin Color Swatch
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
                float textX = cardR.x + iconBoxW + 10f;
                float btnW = 98f;
                float textW = cardR.width - iconBoxW - btnW - 18f;

                GUIStyle sNameStyle = new GUIStyle(GUI.skin.label);
                sNameStyle.fontSize = Mathf.Max(9, (int)(innerW * 0.030f));
                sNameStyle.fontStyle = FontStyle.Bold;
                sNameStyle.normal.textColor = isEquipped ? new Color(0.4f, 1f, 0.55f) : (isUnlocked ? new Color(0.95f, 0.88f, 0.5f) : new Color(0.7f, 0.7f, 0.7f));
                GUI.Label(new Rect(textX, cardR.y + 4f, textW, 18f), skin.skinName, sNameStyle);

                // Stat Buff tags
                string buffText = "";
                if (skin.healthMultiplier > 1f) buffText += $"+{(skin.healthMultiplier - 1f) * 100:F0}% HP  ";
                if (skin.damageMultiplier > 1f) buffText += $"+{(skin.damageMultiplier - 1f) * 100:F0}% ATK  ";
                if (skin.speedMultiplier > 1f) buffText += $"+{(skin.speedMultiplier - 1f) * 100:F0}% SPD  ";
                if (string.IsNullOrEmpty(buffText)) buffText = LanguageSelectionUI.Localize("Standard Base Attributes", "السمات الأساسية القياسية", "Базовые характеристики", "Attributs de base", "Atributos básicos", "Standard-Attribute", "मानक आधार विशेषताएँ", "标准基础属性", "基本ステータス", "Atributos Básicos", "ویژگی‌های پایه استاندارد");

                GUIStyle buffStyle = new GUIStyle(GUI.skin.label);
                buffStyle.fontSize = Mathf.Max(8, (int)(innerW * 0.024f));
                buffStyle.fontStyle = FontStyle.Bold;
                buffStyle.normal.textColor = new Color(0.35f, 0.95f, 0.45f);
                GUI.Label(new Rect(textX, cardR.y + 22f, textW, 16f), buffText, buffStyle);

                GUIStyle descStyle = new GUIStyle(GUI.skin.label);
                descStyle.fontSize = Mathf.Max(7, (int)(innerW * 0.022f));
                descStyle.normal.textColor = new Color(0.75f, 0.82f, 0.75f, 0.80f);
                descStyle.wordWrap = true;
                GUI.Label(new Rect(textX, cardR.y + 38f, textW, 26f), skin.description, descStyle);

                // Right: Action Button
                Rect btnRect = new Rect(cardR.x + cardR.width - btnW - 6f, cardR.y + (cardH - 30f) / 2f, btnW, 30f);

                if (isEquipped)
                {
                    GUIStyle eqLabelStyle = new GUIStyle(GUI.skin.label);
                    eqLabelStyle.fontSize = Mathf.Max(8, (int)(innerW * 0.028f));
                    eqLabelStyle.fontStyle = FontStyle.Bold;
                    eqLabelStyle.alignment = TextAnchor.MiddleCenter;
                    eqLabelStyle.normal.textColor = new Color(0.35f, 1f, 0.45f);
                    string eqStr = LanguageSelectionUI.Localize("✓ EQUIPPED", "✓ مجهّز", "✓ НАДЕТО", "✓ EQUIPADO", "✓ ÉQUIPÉ", "✓ AUSGERÜSTET", "✓ सुसज्जित", "✓ 已装备", "✓ 装備中", "✓ EQUIPADO", "✓ مجهز شده");
                    GUI.Label(btnRect, eqStr, eqLabelStyle);
                }
                else if (isUnlocked)
                {
                    string eqBtnStr = LanguageSelectionUI.Localize("EQUIP", "تجهيز", "НАДЕТЬ", "EQUIPAR", "ÉQUIPER", "AUSRÜSTEN", "सुसज्जित करें", "装备", "装備する", "EQUIPAR", "تجهیز");
                    if (DrawActionButton(btnRect, eqBtnStr, new Color(0.2f, 0.60f, 0.28f)))
                    {
                        EquipSkin(ch, s);
                    }
                }
                else
                {
                    // Locked Skin Button
                    switch (skin.unlockMethod)
                    {
                        case CharacterSkin.UnlockMethod.AccountLevel:
                            bool canClaim = PlayerProfile.Instance != null && PlayerProfile.Instance.AccountLevel >= skin.requiredLevel;
                            string lvlBtn = canClaim
                                ? LanguageSelectionUI.Localize($"CLAIM (Lv.{skin.requiredLevel})", $"استلام (مستوى {skin.requiredLevel})", $"ЗАБРАТЬ (Ур.{skin.requiredLevel})", $"RECLAMAR (Nvl.{skin.requiredLevel})", $"RÉCLAMER (Niv.{skin.requiredLevel})", $"BEANSPRUCHEN (St.{skin.requiredLevel})", $"दावा करें (स्तर {skin.requiredLevel})", $"领取 (等级 {skin.requiredLevel})", $"獲得 (Lv.{skin.requiredLevel})", $"REIVINDICAR (Nvl.{skin.requiredLevel})", $"دریافت (سطح {skin.requiredLevel})")
                                : $"🪙 {skin.requiredCost}";
                            Color lvlCol = canClaim ? new Color(0.25f, 0.65f, 0.35f) : new Color(0.65f, 0.5f, 0.15f);
                            if (DrawActionButton(btnRect, lvlBtn, lvlCol))
                            {
                                UnlockSkin(ch, s, skin);
                            }
                            break;

                        case CharacterSkin.UnlockMethod.WatchAds:
                            string adBtn = LanguageSelectionUI.Localize("🎬 1 AD", "🎬 إعلان 1", "🎬 1 РЕКЛАМА", "🎬 1 ANUNCIO", "🎬 1 PUB", "🎬 1 WERBUNG", "🎬 1 विज्ञापन", "🎬 1 广告", "🎬 1 広告", "🎬 1 ANÚNCIO", "🎬 ۱ تبلیغ");
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

                // Click to preview skin on model
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
            fr: $"✓ ÉQUIPÉ : {currentSkin.skinName}!",
            de: $"✓ AUSGERÜSTET: {currentSkin.skinName}!",
            hi: $"✓ सुसज्जित: {currentSkin.skinName}!",
            zh: $"✓ 已装备: {currentSkin.skinName}!",
            ja: $"✓ 装備完了: {currentSkin.skinName}!",
            pt: $"✓ EQUIPADO: {currentSkin.skinName}!",
            fa: $"✓ مجهز شد: {currentSkin.skinName}!"
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
                        fr: $"Niveau {skin.requiredLevel} ou {skin.requiredCost} Pièces requis !",
                        de: $"Erfordert Kontostufe {skin.requiredLevel} oder {skin.requiredCost} Münzen!",
                        hi: $"खाता स्तर {skin.requiredLevel} या {skin.requiredCost} सिक्के चाहिए!",
                        zh: $"需要账号等级 {skin.requiredLevel} 或 {skin.requiredCost} 金币！",
                        ja: $"アカウントLv.{skin.requiredLevel}または{skin.requiredCost}コインが必要です！",
                        pt: $"Requer Nível {skin.requiredLevel} ou {skin.requiredCost} Moedas!",
                        fa: $"نیاز به سطح حساب {skin.requiredLevel} یا {skin.requiredCost} سکه دارد!"
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
                        fr: $"Pas assez de pièces ! Besoin de {skin.requiredCost}.",
                        de: $"Nicht genug Münzen! Benötige {skin.requiredCost} Münzen.",
                        hi: $"पर्याप्त सिक्के नहीं हैं! {skin.requiredCost} सिक्के चाहिए।",
                        zh: $"金币不足！需要 {skin.requiredCost} 金币。",
                        ja: $"コインが不足しています！{skin.requiredCost}コイン必要です。",
                        pt: $"Moedas insuficientes! Precisa de {skin.requiredCost}.",
                        fa: $"سکه کافی نیست! نیاز به {skin.requiredCost} سکه دارید."
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
                        fr: $"Pas assez de gemmes ! Besoin de {skin.requiredCost}.",
                        de: $"Nicht genug Edelsteine! Benötige {skin.requiredCost} Edelsteine.",
                        hi: $"पर्याप्त रत्न नहीं हैं! {skin.requiredCost} रत्न चाहिए।",
                        zh: $"宝石不足！需要 {skin.requiredCost} 宝石。",
                        ja: $"宝石が不足しています！{skin.requiredCost}宝石必要です。",
                        pt: $"Gemas insuficientes! Precisa de {skin.requiredCost}.",
                        fa: $"الماس کافی نیست! نیاز به {skin.requiredCost} الماس دارید."
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

        // Background groove
        GUI.color = new Color(0.04f, 0.08f, 0.05f, 0.85f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        // Active Fill Bar
        GUI.color = new Color(barColor.r, barColor.g, barColor.b, 0.85f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * value, rect.height), Texture2D.whiteTexture);

        // Specular highlight at leading edge
        if (value > 0.02f)
        {
            float edgeX = rect.x + rect.width * value - 4f;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.DrawTexture(new Rect(edgeX, rect.y, 4f, rect.height), Texture2D.whiteTexture);
        }

        // Crisp border
        DrawBorder(rect, new Color(barColor.r * 0.5f, barColor.g * 0.5f, barColor.b * 0.5f, 0.45f), 1f);
        GUI.color = Color.white;

        // Label
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = fontSize;
        labelStyle.alignment = TextAnchor.MiddleLeft;
        labelStyle.normal.textColor = new Color(0.92f, 0.96f, 0.88f);
        labelStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(rect.x + 6f, rect.y, rect.width - 12f, rect.height), label, labelStyle);
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

    bool DrawActionButton(Rect rect, string text, Color bgColor)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        float bright = hover ? 1.25f : 1f;
        GUI.color = new Color(bgColor.r * bright, bgColor.g * bright, bgColor.b * bright, 0.95f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        DrawBorder(rect, new Color(0.45f, 0.85f, 0.4f, 0.7f), 1f);
        GUI.color = Color.white;

        GUIStyle btnStyle = new GUIStyle(GUI.skin.label);
        btnStyle.fontSize = Mathf.Max(9, (int)(rect.width * 0.080f));
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.alignment = TextAnchor.MiddleCenter;
        btnStyle.normal.textColor = hover ? new Color(1f, 1f, 0.6f) : new Color(0.90f, 0.95f, 0.85f);
        GUI.Label(rect, text, btnStyle);

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    string GetAttackTypeLabel(CharacterCombat.AttackType type)
    {
        switch (type)
        {
            case CharacterCombat.AttackType.Melee:
                return LanguageSelectionUI.Localize("Close-Range Melee", "قتال قريب", "Ближний бой", "Cuerpo a cuerpo", "Mêlée rapprochée", "Nahkampf", "हाथापाई युद्ध", "近战肉搏", "近接攻撃", "Corpo a corpo", "نبرد نزدیک");
            case CharacterCombat.AttackType.Ranged:
                return LanguageSelectionUI.Localize("Ranged Projectile", "مقذوف عن بعد", "Дальний бой", "Proyectil a distancia", "Projectile à distance", "Fernkampf-Projektil", "दूरी प्रक्षेप्य", "远程弹道", "遠距離射撃", "Projétil à distância", "پرتابه از دور");
            default:
                return LanguageSelectionUI.Localize("Unknown", "غير معروف", "Неизвестно", "Desconocido", "Inconnu", "Unbekannt", "अज्ञात", "未知", "不明", "Desconhecido", "نامشخص");
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
        int charID = selectedCharacterID >= 0 ? selectedCharacterID : (hoveredCharacterID >= 0 ? hoveredCharacterID : 0);

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
