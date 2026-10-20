using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Sacred Allegiance (Team Selection) UI:
/// Inscribed on ancient undulating tree bark scrolls without harsh straight lines.
/// Features symmetrical curved leaf wings and detailed lore explaining why to choose
/// Tree Protectors vs Tree Destroyers.
/// </summary>
public class TeamSelectionUI : MonoBehaviour
{
    [Header("Legacy Panel (Auto-hidden)")]
    public GameObject panel;
    public Button protectorButton;
    public Button destroyerButton;

    [Header("Next Screen")]
    public CharacterSelectionUI characterSelectionUI;

    public static TeamSelectionUI Instance { get; private set; }
    public static bool IsTeamSelectionOpen => Instance != null && Instance.showTeamMenu;

    private bool characterScreenOpened = false;
    private bool showTeamMenu = false;

    public void Reopen()
    {
        characterScreenOpened = false;
        showTeamMenu = true;
    }

    void Awake()
    {
        Instance = this;
        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (protectorButton != null)
            protectorButton.onClick.AddListener(() => SelectTeam(PlayerMovement.TeamType.Protector));

        if (destroyerButton != null)
            destroyerButton.onClick.AddListener(() => SelectTeam(PlayerMovement.TeamType.Destroyer));
    }

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            showTeamMenu = false;
            return;
        }

        NetworkObject player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (player == null)
        {
            showTeamMenu = false;
            return;
        }

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            showTeamMenu = false;
            return;
        }

        if (MatchEndStatsUI.isAutoRestartingMatch)
        {
            showTeamMenu = false;
            characterScreenOpened = true;
            return;
        }

        // إذا كان اللاعب قد اختار شخصيته بالفعل (كما في إعادة اللعب التلقائي) أو بدأت المباراة، لا نفتح القائمة أبداً
        if (movement.hasSelectedCharacter.Value || movement.selectedCharacterID.Value >= 0 ||
            (GameStateManager.Instance != null && GameStateManager.Instance.currentState != GameStateManager.GameState.WaitingForPlayers))
        {
            characterScreenOpened = true;
            showTeamMenu = false;
            return;
        }

        // Show choice if team not selected yet
        if (!movement.hasSelectedTeam.Value)
        {
            showTeamMenu = true;
            if (panel != null) panel.SetActive(false); // Hide legacy ugly buttons
            return;
        }

        // Team selected
        showTeamMenu = false;

        // Open Character Selection once
        if (!characterScreenOpened)
        {
            characterScreenOpened = true;
            if (characterSelectionUI != null)
                characterSelectionUI.Show();
        }
    }

    void OnGUI()
    {
        if (!showTeamMenu) return;
        if (GameIntroBriefingUI.IsBriefingOpen) return;
        if (PlayerProgressionAndShopUI.IsShopOpen) return;
        if (CharacterSelectionUI.IsSelectionOpen) return;
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying()) return;

        // Dark enchanted emerald haze
        GUI.color = new Color(0.04f, 0.08f, 0.05f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float modalW = Mathf.Min(840f, Screen.width - 24f);
        float modalH = Mathf.Min(470f, Screen.height - 24f);
        float modalX = (Screen.width - modalW) / 2f;
        float modalY = (Screen.height - modalH) / 2f;

        // Main Ancient Bark Scroll
        EnchantedNatureGUI.DrawOrganicScroll(new Rect(modalX, modalY, modalW, modalH), Color.white);

        // Top-left Back button
        float backW = 150f;
        float backH = 30f;
        string backText = LanguageSelectionUI.Localize(
            "⬅️ BACK",
            ar: "⬅️ رجوع للخلف",
            ru: "⬅️ НАЗАД",
            fr: "⬅️ RETOUR",
            es: "⬅️ ATRÁS",
            de: "⬅️ ZURÜCK",
            hi: "⬅️ वापस",
            zh: "⬅️ 返回",
            ja: "⬅️ 戻る",
            pt: "⬅️ VOLTAR"
        );
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(modalX + 18f, modalY + 14f, backW, backH), backText, new Color(0.85f, 0.45f, 0.35f), 10))
        {
            showTeamMenu = false;
            characterScreenOpened = false;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            if (PlayModeUI.Instance != null)
            {
                PlayModeUI.Instance.ResetMenu();
            }
            return;
        }

        // Header Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0.98f, 0.86f, 0.35f);

        GUI.Label(new Rect(modalX, modalY + 16f, modalW, 28f), LanguageSelectionUI.Localize(
            "❦ SACRED ALLEGIANCE: CHOOSE THY PATH ❦",
            "❦ الولاء المقدس: اختر مسارك ❦",
            "❦ СВЯЩЕННАЯ ВЕРНОСТЬ: ВЫБЕРИ СВОЙ ПУТЬ ❦",
            "❦ LEALTAD SAGRADA: ELIGE TU CAMINO ❦",
            "❦ ALLÉGEANCE SACRÉE : CHOISISSEZ VOTRE VOIE ❦"), titleStyle);

        GUIStyle subTitleStyle = new GUIStyle(GUI.skin.label);
        subTitleStyle.fontSize = 11;
        subTitleStyle.fontStyle = FontStyle.Italic;
        subTitleStyle.alignment = TextAnchor.MiddleCenter;
        subTitleStyle.normal.textColor = new Color(0.55f, 0.90f, 0.65f);

        GUI.Label(new Rect(modalX, modalY + 44f, modalW, 18f), LanguageSelectionUI.Localize(
            "~ Inscribed Two Thousand Years Ago in the Heart of the Sacred Oak ~",
            "~ كُتبت قبل ألفي عام في قلب شجرة البلوط المقدسة ~",
            "~ Начертано две тысячи лет назад в сердце Священного Дуба ~",
            "~ Inscrito hace dos mil años en el corazón del Roble Sagrado ~",
            "~ Gravé il y a deux mille ans au cœur du Chêne Sacré ~"), subTitleStyle);

        // Symmetrical Wing Tablets (Left: Protectors, Right: Destroyers)
        float colW = (modalW - 55f) / 2f;
        float colH = modalH - 95f;
        float leftX = modalX + 22f;
        float rightX = modalX + 33f + colW;
        float colY = modalY + 72f;

        // 1. LEFT WING: TREE PROTECTORS
        DrawAllegianceWing(
            leftX, colY, colW, colH,
            new Color(0.20f, 0.78f, 1.0f),
            LanguageSelectionUI.Localize("🛡️ TREE PROTECTORS", "🛡️ حماة الشجرة", "🛡️ ЗАЩИТНИКИ ДРЕВА", "🛡️ PROTECTORES DEL ÁRBOL", "🛡️ PROTECTEURS DE L'ARBRE"),
            LanguageSelectionUI.Localize("🌿 GUARDIANS OF LIFE", "🌿 حراس الحياة", "🌿 СТРАЖИ ЖИЗНИ", "🌿 GUARDIANES DE LA VIDA", "🌿 GARDIENS DE LA VIE"),
            LanguageSelectionUI.Localize(
                "Nurture and shield the sacred beanstalk from fell axes.\n\n" +
                "• Slay all 3 Guardian Bosses to awaken the Celestial Crystal.\n" +
                "• Carry the fallen crystal to the ancient altar.\n" +
                "• Hunt down enemy woodcutters and claim eternal victory!",
                "احمِ شجرة الفاصولياء المقدسة من الفؤوس.\n\n" +
                "• اهزم الزعماء الثلاثة لإيقاظ الكريستالة السماوية.\n" +
                "• انقل الكريستالة الساقطة إلى المذبح القديم.\n" +
                "• اطرد المدمرين وحقق النصر الأبدي!",
                "Защитите священный стебель от топоров.\n\n" +
                "• Победите 3 Боссов, чтобы пробудить Кристалл.\n" +
                "• Доставьте кристалл к древнему алтарю.\n" +
                "• Отразите атаки разрушителей!",
                "Protege el tallo sagrado de las hachas.\n\n" +
                "• Derrota a los 3 Jefes para despertar el Cristal.\n" +
                "• Lleva el cristal caído al altar ancestral.\n" +
                "• ¡Vence a los destructores y triunfa!",
                "Protégez le haricot magique sacré des haches.\n\n" +
                "• Battez les 3 Boss pour éveiller le Cristal Céleste.\n" +
                "• Transportez le cristal à l'autel antique.\n" +
                "• Repoussez les destructeurs et triomphez !"),
            LanguageSelectionUI.Localize("🌿 BECOME A PROTECTOR", "🌿 انضم إلى الحماة", "🌿 СТАТЬ ЗАЩИТНИКОМ", "🌿 SÉ UN PROTECTOR", "🌿 DEVENIR UN PROTECTEUR"),
            () => SelectTeam(PlayerMovement.TeamType.Protector)
        );

        // 2. RIGHT WING: TREE DESTROYERS
        DrawAllegianceWing(
            rightX, colY, colW, colH,
            new Color(1f, 0.32f, 0.18f),
            LanguageSelectionUI.Localize("⚔️ TREE DESTROYERS", "⚔️ مدمر الشجرة", "⚔️ РАЗРУШИТЕЛИ ДРЕВА", "⚔️ DESTRUCTORES DEL ÁRBOL", "⚔️ DESTRUCTEURS DE L'ARBRE"),
            LanguageSelectionUI.Localize("🔥 AXEMEN OF CHAOS", "🔥 حطابو الفوضى", "🔥 СЕКИРЫ ХАОСА", "🔥 LEÑADORES DEL CAOS", "🔥 BÛCHERONS DU CHAOS"),
            LanguageSelectionUI.Localize(
                "Fell the towering colossus and claim its ancient power.\n\n" +
                "• Slay all 3 Guardian Bosses to break the Tree Shield.\n" +
                "• Strike down the giant trunk with relentless woodcutter fury.\n" +
                "• Stop protectors from delivering crystals and conquer the realm!",
                "أسقط الشجرة العملاقة واستولِ على قوتها القديمة.\n\n" +
                "• اهزم زعماء الحراسة الثلاثة لكسر درع الشجرة.\n" +
                "• اضرب الجذع العملاق بقوة فأسك التي لا ترحم.\n" +
                "• امنع الحماة من إيصال الكريستالة وأسقط الشجرة!",
                "Срубите великое древо и заберите силу.\n\n" +
                "• Убейте 3 Боссов-хранителей, чтобы снять щит древа.\n" +
                "• Рубите ствол без пощады.\n" +
                "• Не дайте защитникам донести кристалл!",
                "Derriba el coloso y reclama su poder ancestral.\n\n" +
                "• Elimina a los 3 Jefes para romper el escudo del Árbol.\n" +
                "• Tala el tronco gigante con furia implacable.\n" +
                "• ¡Evita que lleven los cristales y gana!",
                "Abattez le colosse et emparez-vous de son pouvoir.\n\n" +
                "• Éliminez les 3 Boss pour briser le bouclier de l'arbre.\n" +
                "• Frappez le tronc avec une fureur implacable.\n" +
                "• Empêchez la livraison des cristaux et gagnez !"),
            LanguageSelectionUI.Localize("🔥 BECOME A DESTROYER", "🔥 انضم إلى المدمرين", "🔥 СТАТЬ РАЗРУШИТЕЛЕМ", "🔥 SÉ UN DESTRUCTOR", "🔥 DEVENIR UN DESTRUCTEUR"),
            () => SelectTeam(PlayerMovement.TeamType.Destroyer)
        );
    }

    void DrawAllegianceWing(float x, float y, float w, float h, Color accent, string title, string badge, string lore, string btnText, System.Action onChoose)
    {
        // Curved Organic Leaf Scroll for each wing
        EnchantedNatureGUI.DrawOrganicScroll(new Rect(x, y, w, h), new Color(accent.r * 0.2f + 0.8f, accent.g * 0.2f + 0.8f, accent.b * 0.2f + 0.8f, 0.96f));

        // Badge
        GUIStyle badgeStyle = new GUIStyle(GUI.skin.label);
        badgeStyle.fontSize = 10;
        badgeStyle.fontStyle = FontStyle.Bold;
        badgeStyle.alignment = TextAnchor.MiddleCenter;
        badgeStyle.normal.textColor = accent;

        float badgeW = 160f;
        GUI.color = new Color(accent.r * 0.15f, accent.g * 0.15f, accent.b * 0.15f, 0.85f);
        GUI.DrawTexture(new Rect(x + (w - badgeW) / 2f, y + 14f, badgeW, 22f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + (w - badgeW) / 2f, y + 14f, badgeW, 22f), badge, badgeStyle);

        // Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 16;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(x + 10f, y + 42f, w - 20f, 24f), title, titleStyle);

        // Lore & Explanation
        GUIStyle loreStyle = new GUIStyle(GUI.skin.label);
        loreStyle.fontSize = 11;
        loreStyle.wordWrap = true;
        loreStyle.normal.textColor = new Color(0.85f, 0.90f, 0.86f);

        GUI.Label(new Rect(x + 20f, y + 74f, w - 40f, h - 145f), lore, loreStyle);

        // Action Leaf Button
        float btnW = w - 40f;
        float btnH = 46f;
        float btnX = x + 20f;
        float btnY = y + h - btnH - 18f;

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(btnX, btnY, btnW, btnH), btnText, accent, 13))
        {
            onChoose?.Invoke();
        }
    }

    void SelectTeam(PlayerMovement.TeamType selectedTeam)
    {
        if (NetworkManager.Singleton == null) return;
        NetworkObject player = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (player == null) return;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement == null) return;

        movement.RequestTeam(selectedTeam);
        movement.team = selectedTeam;
        movement.ApplyTeamVisualAura();
        showTeamMenu = false;
    }
}