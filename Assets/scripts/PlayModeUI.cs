using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Ancient Enchanted Forest Mode Selection Scroll:
/// Inscribed on ancient undulating tree bark scrolls without harsh straight lines,
/// featuring organic leaf contours, druidic glyphs, and natural curved tablets.
/// </summary>
public class PlayModeUI : MonoBehaviour
{
    public enum PlayMode
    {
        None,
        Online,
        Offline,
        Friendly
    }

    public static PlayMode SelectedMode = PlayMode.None;
    public static PlayModeUI Instance { get; private set; }

    [Header("Legacy UI Panel (Auto-hidden)")]
    public GameObject panel;
    public Button onlineButton;
    public Button offlineButton;

    private bool isMenuVisible = true;
    private bool inOnlineSubmenu = false;
    private string onlineConnectIP = "127.0.0.1";

    void Awake()
    {
        Instance = this;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    void Start()
    {
        ResetMenu();
    }

    public void ResetMenu()
    {
        if (MatchEndStatsUI.isAutoRestartingMatch)
        {
            isMenuVisible = false;
            return;
        }

        SelectedMode = PlayMode.None;
        inOnlineSubmenu = false;
        isMenuVisible = true;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void StartOffline()
    {
        SelectedMode = PlayMode.Offline;
        isMenuVisible = false;
        Debug.Log("🌲 [PlayModeUI] Starting OFFLINE forest battle (Host + AI Bots)");

        if (panel != null) panel.SetActive(false);

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
        }
    }

    public void StartOnlineHost()
    {
        SelectedMode = PlayMode.Online;
        isMenuVisible = false;
        Debug.Log("👑 [PlayModeUI] Starting ONLINE forest realm as HOST");

        if (panel != null) panel.SetActive(false);

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
        }
    }

    public void StartOnlineClient()
    {
        SelectedMode = PlayMode.Online;
        isMenuVisible = false;
        Debug.Log($"🎮 [PlayModeUI] Connecting to ONLINE forest realm at {onlineConnectIP}");

        if (panel != null) panel.SetActive(false);

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null && !string.IsNullOrEmpty(onlineConnectIP))
            {
                transport.ConnectionData.Address = onlineConnectIP.Trim();
            }
            NetworkManager.Singleton.StartClient();
        }
    }

    public void StartFriendlyMode()
    {
        SelectedMode = PlayMode.Friendly;
        isMenuVisible = false;
        Debug.Log("🤝 [PlayModeUI] Opening FRIENDLY forest lobby");

        if (panel != null) panel.SetActive(false);

        if (FriendlyLobbyUI.Instance != null)
        {
            FriendlyLobbyUI.Instance.OpenLobby();
        }
    }

    public static bool IsPlayModeMenuOpen => Instance != null && Instance.isMenuVisible && SelectedMode == PlayMode.None;

    void OnGUI()
    {
        if (LanguageSelectionUI.IsLanguageModalOpen) return;
        if (!isMenuVisible || SelectedMode != PlayMode.None) return;
        if (GameIntroBriefingUI.IsBriefingOpen) return;
        if (PlayerProgressionAndShopUI.IsShopOpen) return;
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying()) return;
        if (FriendlyLobbyUI.Instance != null && FriendlyLobbyUI.Instance.isLobbyActive) return;

        // Dark enchanted emerald haze behind the scroll
        GUI.color = new Color(0.04f, 0.09f, 0.05f, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (inOnlineSubmenu)
        {
            DrawOnlineSubmenu();
        }
        else
        {
            DrawMainModeSelect();
        }
    }

    // =========================================================================
    // ANCIENT TREE BARK PARCHMENT SCROLL
    // =========================================================================
    void DrawMainModeSelect()
    {
        float modalWidth = Mathf.Min(820f, Screen.width - 24f);
        float modalHeight = Mathf.Min(490f, Screen.height - 24f);
        float modalX = (Screen.width - modalWidth) / 2f;
        // Positioned with safe clearance to never overlap with the top status bar!
        float modalY = Mathf.Max(62f, (Screen.height - modalHeight) / 2f + 25f);

        // 1. Organic Tree Bark Scroll Background
        EnchantedNatureGUI.DrawOrganicScroll(new Rect(modalX, modalY, modalWidth, modalHeight), Color.white);

        // Top-left Language Quick Re-select Button
        float langBtnW = 150f;
        float langBtnH = 30f;
        string langBtnText = LanguageSelectionUI.Localize(
            "🌐 LANGUAGE",
            ar: "🌐 تغيير اللغة",
            ru: "🌐 СМЕНИТЬ ЯЗЫК",
            fr: "🌐 CHANGER LANGUE",
            es: "🌐 CAMBIAR IDIOMA",
            de: "🌐 SPRACHE",
            hi: "🌐 भाषा बदलें",
            zh: "🌐 更改语言",
            ja: "🌐 言語変更",
            pt: "🌐 MUDAR IDIOMA"
        );
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(modalX + 18f, modalY + 14f, langBtnW, langBtnH), langBtnText, new Color(0.35f, 0.85f, 0.95f), 10))
        {
            if (LanguageSelectionUI.Instance != null)
            {
                LanguageSelectionUI.Instance.OpenModal();
            }
        }

        string title = LanguageSelectionUI.Localize("❦ THE SACRED BEANSTALK: FOREST WAR ❦",
            ar: "❦ حرب نبتة الفاصولياء المقدسة ❦",
            ru: "❦ ВОЙНА СВЯЩЕННОГО БОБА ❦",
            es: "❦ LA GUERRA DEL TALLO SAGRADO ❦",
            fr: "❦ LA GUERRE DU HARICOT SACRÉ ❦",
            de: "❦ DER KRIEG DER BOHNENRANKE ❦",
            hi: "❦ पवित्र सेम के पौधे का युद्ध ❦",
            zh: "❦ 圣豆蔓森林之战 ❦",
            ja: "❦ 聖なる豆の木の戦い ❦",
            pt: "❦ A GUERRA DO FEIJÃO SAGRADO ❦");

        string subTitle = LanguageSelectionUI.Localize("~ Inscribed on Ancient Oak Bark Two Millennia Past ~",
            ar: "~ نُقشت على لحاء البلوط القديم منذ ألفي عام ~",
            ru: "~ Высечено на коре древнего дуба два тысячелетия назад ~",
            es: "~ Inscrito en la corteza de roble ancestral ~",
            fr: "~ Gravé sur l'écorce de chêne ancestral ~",
            de: "~ Vor zwei Jahrtausenden in alte Eichenrinde geritzt ~",
            hi: "~ दो सहस्राब्दी पहले प्राचीन ओक की छाल पर अंकित ~",
            zh: "~ 两千年前铭刻于古橡木树皮之上 ~",
            ja: "~ 二千年の昔、古代の樫の樹皮に刻まれし記録 ~",
            pt: "~ Gravado na casca de carvalho ancestral há dois milênios ~");

        // 2. Ancient Druid Header
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0.98f, 0.86f, 0.35f);

        GUI.Label(new Rect(modalX, modalY + 16f, modalWidth, 28f), title, titleStyle);

        GUIStyle subTitleStyle = new GUIStyle(GUI.skin.label);
        subTitleStyle.fontSize = 11;
        subTitleStyle.fontStyle = FontStyle.Italic;
        subTitleStyle.alignment = TextAnchor.MiddleCenter;
        subTitleStyle.normal.textColor = new Color(0.55f, 0.90f, 0.65f);

        GUI.Label(new Rect(modalX, modalY + 44f, modalWidth, 18f), subTitle, subTitleStyle);

        // 3. Three Organic Leaf Tablets
        float cardY = modalY + 70f;
        float cardHeight = 120f;
        float cardSpacing = 14f;

        // Card 1: OFFLINE EXPEDITION (SOLO vs BOTS)
        string c1Title = LanguageSelectionUI.Localize("⚔️ OFFLINE EXPEDITION (SOLO vs BOTS)",
            ar: "⚔️ معركة فردية (ضد البوتات)",
            ru: "⚔️ ОДИНОЧНЫЙ БОЙ (ПРОТИВ БОТОВ)",
            es: "⚔️ EXPEDICIÓN INDIVIDUAL (SOLO vs BOTS)",
            fr: "⚔️ EXPÉDITION SOLO (vs BOTS)",
            de: "⚔️ OFFLINE-EXPEDITION (SOLO vs BOTS)",
            hi: "⚔️ ऑफ़लाइन अभियान (एकल बनाम बॉट्स)",
            zh: "⚔️ 离线远征 (单人对人机)",
            ja: "⚔️ オフライン遠征 (ソロ vs ボット)",
            pt: "⚔️ EXPEDIÇÃO OFFLINE (SOLO vs BOTS)");

        string c1Badge = LanguageSelectionUI.Localize("🍃 SACRED TRIAL",
            ar: "🍃 تجربة مقدسة",
            ru: "🍃 ИСПЫТАНИЕ",
            es: "🍃 PRUEBA SAGRADA",
            fr: "🍃 ÉPREUVE SACRÉE",
            de: "🍃 HEILIGE PRÜFUNG",
            hi: "🍃 पवित्र परीक्षण",
            zh: "🍃 神圣试炼",
            ja: "🍃 聖なる試練",
            pt: "🍃 PROVA SAGRADA");

        string c1Desc = LanguageSelectionUI.Localize(
            "Venture into the secluded groves to test thy valor against forest bot warriors. Nurture the sacred beanstalk or harvest its ancient timber, challenge towering titans, and harvest gold & XP without internet.",
            ar: "انطلق في الغابة لتختبر شجاعتك ضد محاربي البوتات. اهزم الزعماء الثلاثة، اجمع الكريستال أو دمر الشجرة، واكسب الذهب ونقاط الخبرة بدون إنترنت.",
            ru: "Сразитесь с лесными ботами без интернета. Победите трех боссов, захватите кристалл или срубите дерево для получения золота и опыта.",
            es: "Adéntrate en el bosque contra bots guerreros. Derrota a los 3 jefes, protege o destruye el árbol sagrado y gana oro y EXP sin internet.",
            fr: "Aventurez-vous dans les bois contre des guerriers bots. Battez les 3 boss, protégez ou détruisez l'arbre sacré et gagnez de l'or et de l'EXP sans connexion.",
            de: "Begebt euch in die Wälder gegen Bot-Krieger. Besiegt 3 Bosse, nährt oder fällt den Baum und sammelt Gold & XP ohne Internet.",
            hi: "जंगल में बॉट योद्धाओं के विरुद्ध अपने साहस का परीक्षण करें। 3 बॉसों को हराएं, क्रिस्टल लें या पेड़ काटें, और बिना इंटरनेट सोना व XP प्राप्त करें।",
            zh: "深入密林与森林人机卫士一战高下。击败三大Boss，滋养或砍伐古树，无需联网即可斩获黄金与经验。",
            ja: "森のボット戦士たちと腕試し。3体のボスを討伐し、神木を守るか切り倒して、オフラインで報酬とXPを獲得しよう。",
            pt: "Aventure-se na floresta contra bots guerreiros. Derrote os 3 chefes, proteja ou destrua a árvore e ganhe ouro e EXP offline.");

        string c1Btn = LanguageSelectionUI.Localize("🌲 ENTER GROVE",
            ar: "🌲 ابدأ المعركة",
            ru: "🌲 ВОЙТИ В ЛЕС",
            es: "🌲 ENTRAR AL BOSQUE",
            fr: "🌲 ENTRER DANS LE BOIS",
            de: "🌲 HAIN BETRETEN",
            hi: "🌲 ग्रोव में प्रवेश करें",
            zh: "🌲 进入丛林",
            ja: "🌲 森に入る",
            pt: "🌲 ENTRAR NO BOSQUE");

        DrawOrganicModeCard(
            modalX + 22f, cardY, modalWidth - 44f, cardHeight,
            new Color(0.24f, 0.88f, 0.42f),
            c1Title,
            c1Badge,
            c1Desc,
            c1Btn,
            () => StartOffline()
        );

        // Card 2: REALM EXPEDITION (ONLINE MULTIPLAYER)
        cardY += cardHeight + cardSpacing;
        string c2Title = LanguageSelectionUI.Localize("🌐 REALM EXPEDITION (ONLINE MULTIPLAYER)",
            ar: "🌐 معركة أونلاين (متعدد اللاعبين)",
            ru: "🌐 СЕТЕВАЯ ИГРА (ОНЛАЙН)",
            es: "🌐 EXPEDICIÓN ONLINE (MULTIJUGADOR)",
            fr: "🌐 EXPÉDITION EN LIGNE (MULTIJOUEUR)",
            de: "🌐 REALM-EXPEDITION (ONLINE-MEHRSPIELER)",
            hi: "🌐 ऑनलाइन अभियान (मल्टीप्लेयर)",
            zh: "🌐 在线远征 (多人联机)",
            ja: "🌐 オンライン遠征 (マルチプレイ)",
            pt: "🌐 EXPEDIÇÃO ONLINE (MULTIJUGADOR)");

        string c2Badge = LanguageSelectionUI.Localize("💧 RIVER REALMS",
            ar: "💧 عوالم الأنهار",
            ru: "💧 РЕЧНЫЕ ЗЕМЛИ",
            es: "💧 REINOS DEL RÍO",
            fr: "💧 ROYAUMES DU FLEUVE",
            de: "💧 FLUSS-REICHE",
            hi: "💧 नदी क्षेत्र",
            zh: "💧 江河领域",
            ja: "💧 河川の領域",
            pt: "💧 REINOS DO RIO");

        string c2Desc = LanguageSelectionUI.Localize(
            "Unite with kindred druids and warriors across distant realms. Awaken a public sanctuary or journey through active network battles across waterfalls and sacred canopies.",
            ar: "اتحد مع حلفائك عبر السيرفرات في معارك جماعية مباشرة عبر الشلالات ومناطق الغابة المقدسة.",
            ru: "Объединяйтесь с игроками по сети. Создайте открытую комнату или подключайтесь к серверам для битв у священных водопадов.",
            es: "Únete a druidas y guerreros en reinos lejanos. Abre un santuario público o combate a través de cascadas sagradas en red.",
            fr: "Rejoignez d'autres guerriers à travers les royaumes. Ouvrez un sanctuaire public ou combattez à travers cascades et canopées en réseau.",
            de: "Verbündet euch online mit anderen Druiden und Kriegern. Erschafft ein öffentliches Heiligtum oder tretet Netzwerk-Schlachten bei.",
            hi: "दूर के क्षेत्रों के योद्धाओं के साथ एकजुट हों। पवित्र झरनों और जंगलों में ऑनलाइन नेटवर्क लड़ाइयों में भाग लें।",
            zh: "与天各一方的德鲁伊和勇士们并肩作战。开启公开圣所，穿行于瀑布与神圣树冠间的网络战役。",
            ja: "世界中の戦士たちとオンラインで共闘。公開ロビーを作成するか既存の対戦に参加して激戦を繰り広げよう。",
            pt: "Una-se a aliados em servidores online. Crie uma sala pública ou participe de batalhas multiplayer pela floresta sagrada.");

        string c2Btn = LanguageSelectionUI.Localize("🌊 EMBARK ONLINE",
            ar: "🌊 دخول أونلاين",
            ru: "🌊 ИГРАТЬ ОНЛАЙН",
            es: "🌊 ENTRAR ONLINE",
            fr: "🌊 JOUER EN LIGNE",
            de: "🌊 ONLINE STARTEN",
            hi: "🌊 ऑनलाइन शुरू करें",
            zh: "🌊 在线出发",
            ja: "🌊 オンラインへ",
            pt: "🌊 EMBARCAR ONLINE");

        DrawOrganicModeCard(
            modalX + 22f, cardY, modalWidth - 44f, cardHeight,
            new Color(0.20f, 0.80f, 1.0f),
            c2Title,
            c2Badge,
            c2Desc,
            c2Btn,
            () => { inOnlineSubmenu = true; }
        );

        // Card 3: TRIBE SANCTUARY (FRIENDLY LOBBY)
        cardY += cardHeight + cardSpacing;
        string c3Title = LanguageSelectionUI.Localize("🤝 TRIBE SANCTUARY (FRIENDLY LOBBY)",
            ar: "🤝 غرفة خاصة للأصدقاء (لوبي)",
            ru: "🤝 ДРУЖЕСКОЕ ЛОББИ (СВОЯ КОМНАТА)",
            es: "🤝 SANTUARIO DE AMIGOS (LOBBY PRIVADO)",
            fr: "🤝 SANCTUAIRE D'AMIS (SALON PRIVÉ)",
            de: "🤝 STAMMESHEILIGTUM (FREUNDES-LOBBY)",
            hi: "🤝 मित्र अभयारण्य (निजी लॉबी)",
            zh: "🤝 部落圣所 (好友房间)",
            ja: "🤝 仲間たちの聖域 (フレンドロビー)",
            pt: "🤝 SANTUÁRIO DA TRIBO (LOBBY PRIVADO)");

        string c3Badge = LanguageSelectionUI.Localize("🌳 SACRED COVEN",
            ar: "🌳 تحالف سري",
            ru: "🌳 СВОЙ КЛАН",
            es: "🌳 COFRADÍA SAGRADA",
            fr: "🌳 ALLIANCE PRIVÉE",
            de: "🌳 HEILIGER ZIRKEL",
            hi: "🌳 पवित्र गठबंधन",
            zh: "🌳 神圣秘社",
            ja: "🌳 聖なる盟約",
            pt: "🌳 CLÃ SAGRADO");

        string c3Desc = LanguageSelectionUI.Localize(
            "Gather thy chosen companions under a secret room cipher. Divide thy allies between Tree Protectors and Tree Destroyers with custom coven rules and optional bot fill.",
            ar: "اجمع أصدقاءك برمز غرفة خاص، قسّم الفرق بين حماة الشجرة ومدمرِيها مع تخصيص القواعد وإضافة البوتات.",
            ru: "Соберите друзей по секретному коду комнаты. Разделитесь на Защитников и Разрушителей с гибкими правилами и ботами.",
            es: "Reúne a tus amigos con un código secreto. Divide a tus aliados entre Protectores y Destructores con reglas personalizadas.",
            fr: "Rassemblez vos compagnons avec un code privé. Répartissez vos alliés entre Protecteurs et Destructeurs avec règles personnalisées.",
            de: "Versammelt eure Freunde mit einem Geheimcode. Teilt Teams zwischen Beschützern und Zerstörern mit eigenen Regeln ein.",
            hi: "गुप्त कमरे के कोड के साथ अपने मित्रों को इकट्ठा करें। कस्टम नियमों और बॉट्स के साथ टीमों को विभाजित करें।",
            zh: "凭借私密房间码召集好友。在守护者与破坏者间自由分配阵营，设定自定义对战规则与人机填充。",
            ja: "秘密のルームコードで仲間を招集。自由なルールとボット追加で守護者と破壊者のチームに分かれて対戦しよう。",
            pt: "Reúna seus amigos com um código privado. Divida as equipes entre Protetores e Destruidores com regras personalizadas.");

        string c3Btn = LanguageSelectionUI.Localize("🤝 GATHER TRIBE",
            ar: "🤝 إنشاء اللوبي",
            ru: "🤝 СОЗДАТЬ ЛОББИ",
            es: "🤝 CREAR LOBBY",
            fr: "🤝 CRÉER LE SALON",
            de: "🤝 STAMM VERSAMMELN",
            hi: "🤝 कबीला इकट्ठा करें",
            zh: "🤝 召集部落",
            ja: "🤝 仲間を集める",
            pt: "🤝 REUNIR A TRIBO");

        DrawOrganicModeCard(
            modalX + 22f, cardY, modalWidth - 44f, cardHeight,
            new Color(1f, 0.75f, 0.20f),
            c3Title,
            c3Badge,
            c3Desc,
            c3Btn,
            () => StartFriendlyMode()
        );
    }

    void DrawOrganicModeCard(float x, float y, float w, float h, Color accent, string title, string badge, string desc, string btnText, System.Action onClick)
    {
        // Organic Leaf Tablet Background
        EnchantedNatureGUI.DrawOrganicScroll(new Rect(x, y, w, h), new Color(0.85f, 0.95f, 0.88f, 0.96f));

        // Leaf Badge
        GUIStyle badgeStyle = new GUIStyle(GUI.skin.label);
        badgeStyle.fontSize = 10;
        badgeStyle.fontStyle = FontStyle.Bold;
        badgeStyle.alignment = TextAnchor.MiddleCenter;
        badgeStyle.normal.textColor = accent;

        float badgeW = 125f;
        GUI.color = new Color(accent.r * 0.18f, accent.g * 0.18f, accent.b * 0.18f, 0.85f);
        GUI.DrawTexture(new Rect(x + 20f, y + 10f, badgeW, 20f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 20f, y + 10f, badgeW, 20f), badge, badgeStyle);

        // Title
        GUIStyle cardTitleStyle = new GUIStyle(GUI.skin.label);
        cardTitleStyle.fontSize = 13;
        cardTitleStyle.fontStyle = FontStyle.Bold;
        cardTitleStyle.alignment = TextAnchor.MiddleLeft;
        cardTitleStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(x + 20f + badgeW + 12f, y + 10f, w - 340f, 20f), title, cardTitleStyle);

        // Description
        GUIStyle descStyle = new GUIStyle(GUI.skin.label);
        descStyle.fontSize = 11;
        descStyle.wordWrap = true;
        descStyle.normal.textColor = new Color(0.82f, 0.90f, 0.84f);

        GUI.Label(new Rect(x + 20f, y + 36f, w - 210f, 75f), desc, descStyle);

        // Organic Leaf Button
        float btnW = 160f;
        float btnH = 46f;
        float btnX = x + w - btnW - 18f;
        float btnY = y + (h - btnH) / 2f;

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(btnX, btnY, btnW, btnH), btnText, accent, 12))
        {
            onClick?.Invoke();
        }
    }

    void DrawOnlineSubmenu()
    {
        float modalWidth = Mathf.Min(720f, Screen.width - 24f);
        float modalHeight = Mathf.Min(430f, Screen.height - 24f);
        float modalX = (Screen.width - modalWidth) / 2f;
        float modalY = Mathf.Max(62f, (Screen.height - modalHeight) / 2f + 25f);

        EnchantedNatureGUI.DrawOrganicScroll(new Rect(modalX, modalY, modalWidth, modalHeight), Color.white);

        string titleText = LanguageSelectionUI.Localize("🌊 ONLINE FOREST REALM SETUP",
            ar: "🌊 إعدادات الاتصال أونلاين",
            ru: "🌊 НАСТРОЙКА ОНЛАЙН СЕРВЕРА",
            es: "🌊 CONFIGURACIÓN DEL REINO ONLINE",
            fr: "🌊 CONFIGURATION DU ROYAUME EN LIGNE",
            de: "🌊 ONLINE-WALDREICH-EINSTELLUNGEN",
            hi: "🌊 ऑनलाइन वन क्षेत्र सेटअप",
            zh: "🌊 在线森林领域设置",
            ja: "🌊 オンライン領域の設定",
            pt: "🌊 CONFIGURAÇÃO DE REINO ONLINE");

        string subTitleText = LanguageSelectionUI.Localize("~ Awaken a Wild Sanctuary or Step Across Distant Rivers ~",
            ar: "~ أنشئ خادمًا جديدًا أو انضم إلى معركة عبر الشبكة ~",
            ru: "~ Создайте священную комнату или подключитесь по сети ~",
            es: "~ Despierta un santuario o viaja a través de ríos lejanos ~",
            fr: "~ Réveillez un sanctuaire ou franchissez des fleuves lointains ~",
            de: "~ Erweckt ein Heiligtum oder schreitet über ferne Flüsse ~",
            hi: "~ एक अभयारण्य बनाएं या दूर की नदियों के पार कदम रखें ~",
            zh: "~ 唤醒狂野圣所，或跨越遥远之河 ~",
            ja: "~ 荒野の聖域を開くか、遥かなる大河を渡れ ~",
            pt: "~ Desperte um santuário ou atravesse rios distantes ~");

        string hostTitle = LanguageSelectionUI.Localize("👑 HOST A FOREST REALM (SERVER)",
            ar: "👑 استضافة سيرفر للغابة (مضيف)",
            ru: "👑 СОЗДАТЬ СЕРВЕР (ХОСТ)",
            es: "👑 CREAR SERVIDOR (HOST)",
            fr: "👑 HÉBERGER UN SERVEUR (HÔTE)",
            de: "👑 WALDREICH BEHERBERGEN (SERVER)",
            hi: "👑 वन क्षेत्र होस्ट करें (सर्वर)",
            zh: "👑 创立森林领域 (主机)",
            ja: "👑 サーバーをホストする",
            pt: "👑 HOSPEDAR REINO (SERVIDOR)");

        string hostDesc = LanguageSelectionUI.Localize("Awaken the ancient grove on your device. Other warriors can connect directly to your forest using your IP address.",
            ar: "افتح عالم الغابة على جهازك ليتمكن اللاعبون الآخرون من الاتصال بك مباشرة عبر الـ IP.",
            ru: "Запустите сервер на своем устройстве. Другие воины смогут подключиться напрямую по вашему IP.",
            es: "Despierta el bosque en tu dispositivo. Otros guerreros podrán conectarse directamente con tu dirección IP.",
            fr: "Réveillez le bois sacré sur votre appareil. Les autres guerriers pourront s'y connecter via votre IP.",
            de: "Erweckt den alten Hain auf eurem Gerät. Andere Krieger verbinden sich direkt über eure IP-Adresse.",
            hi: "अपने डिवाइस पर प्राचीन ग्रोव को जगाएं। अन्य योद्धा आपके आईपी पते का उपयोग करके जुड़ सकते हैं।",
            zh: "在您的设备上唤醒古老树林。其他勇士可直接通过您的IP地址连接加入森林战场。",
            ja: "お使いの端末で古代の木立ちを開設。他のプレイヤーがIPアドレスで直接接続可能になります。",
            pt: "Desperte o bosque no seu dispositivo. Outros jogadores podem conectar-se diretamente via seu IP.");

        string hostBtn = LanguageSelectionUI.Localize("👑 HOST REALM",
            ar: "👑 استضافة السيرفر",
            ru: "👑 ЗАПУСТИТЬ ХОСТ",
            es: "👑 CREAR SERVIDOR",
            fr: "👑 HÉBERGER",
            de: "👑 REICH STARTEN",
            hi: "👑 सर्वर बनाएं",
            zh: "👑 创建领域",
            ja: "👑 ホスト開始",
            pt: "👑 HOSPEDAR REINO");

        string joinTitle = LanguageSelectionUI.Localize("🎮 JOURNEY TO A REMOTE REALM",
            ar: "🎮 الانضمام إلى سيرفر آخر",
            ru: "🎮 ПОДКЛЮЧИТЬСЯ К СЕРВЕРУ",
            es: "🎮 VIAJAR A UN REINO REMOTO",
            fr: "🎮 REJOINDRE UN ROYAUME",
            de: "🎮 ZU EINEM REICH REISEN",
            hi: "🎮 दूर के क्षेत्र में प्रवेश करें",
            zh: "🎮 前往远程领域",
            ja: "🎮 遠隔サーバーへ参加",
            pt: "🎮 JUNTAR-SE A UM REINO");

        string joinDesc = LanguageSelectionUI.Localize("Enter the host's sacred IP address to step into their live arena:",
            ar: "أدخل عنوان الـ IP الخاص بالمضيف للدخول في معركتهم المباشرة:",
            ru: "Введите IP-адрес хоста для входа в активную сетевую битву:",
            es: "Introduce la dirección IP del anfitrión para unirte a su batalla:",
            fr: "Entrez l'adresse IP de l'hôte pour entrer dans son arène:",
            de: "Gebt die IP-Adresse des Gastgebers ein, um seiner Arena beizutreten:",
            hi: "लाइव अखाड़े में कदम रखने के लिए होस्ट का आईपी पता दर्ज करें:",
            zh: "输入主机的IP地址以踏入其战局领域：",
            ja: "ホストのIPアドレスを入力してリアルタイムバトルへ参加：",
            pt: "Insira o IP do anfitrião para entrar na arena:");

        string joinBtn = LanguageSelectionUI.Localize("🌊 JOIN REALM",
            ar: "🌊 دخول السيرفر",
            ru: "🌊 ПОДКЛЮЧИТЬСЯ",
            es: "🌊 UNIRSE AL REINO",
            fr: "🌊 REJOINDRE",
            de: "🌊 BEITRETEN",
            hi: "🌊 क्षेत्र में शामिल हों",
            zh: "🌊 加入领域",
            ja: "🌊 参加する",
            pt: "🌊 ENTRAR NO REINO");

        string backBtn = LanguageSelectionUI.Localize("⬅️ Return to Scrolls",
            ar: "⬅️ رجوع إلى القائمة",
            ru: "⬅️ Вернуться назад",
            es: "⬅️ Volver a la lista",
            fr: "⬅️ Retour au menu",
            de: "⬅️ Zurück zur Auswahl",
            hi: "⬅️ वापस लौटें",
            zh: "⬅️ 返回列表",
            ja: "⬅️ メニューへ戻る",
            pt: "⬅️ Voltar ao menu");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0.25f, 0.85f, 1f);

        GUI.Label(new Rect(modalX, modalY + 16f, modalWidth, 26f), titleText, titleStyle);

        GUIStyle subTitleStyle = new GUIStyle(GUI.skin.label);
        subTitleStyle.fontSize = 11;
        subTitleStyle.fontStyle = FontStyle.Italic;
        subTitleStyle.alignment = TextAnchor.MiddleCenter;
        subTitleStyle.normal.textColor = new Color(0.7f, 0.90f, 0.8f);

        GUI.Label(new Rect(modalX, modalY + 44f, modalWidth, 20f), subTitleText, subTitleStyle);

        float boxW = modalWidth - 44f;
        float boxX = modalX + 22f;
        float box1Y = modalY + 80f;

        // Host Card
        EnchantedNatureGUI.DrawOrganicScroll(new Rect(boxX, box1Y, boxW, 95f), new Color(0.85f, 0.95f, 0.9f, 0.95f));

        GUIStyle boldStyle = new GUIStyle(GUI.skin.label);
        boldStyle.fontSize = 14;
        boldStyle.fontStyle = FontStyle.Bold;
        boldStyle.normal.textColor = Color.yellow;

        GUI.Label(new Rect(boxX + 18f, box1Y + 12f, boxW - 200f, 22f), hostTitle, boldStyle);

        GUIStyle infoStyle = new GUIStyle(GUI.skin.label);
        infoStyle.fontSize = 11;
        infoStyle.wordWrap = true;
        infoStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(boxX + 18f, box1Y + 38f, boxW - 200f, 48f), hostDesc, infoStyle);

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(boxX + boxW - 175f, box1Y + 24f, 155f, 46f), hostBtn, new Color(0.95f, 0.75f, 0.15f), 12))
        {
            StartOnlineHost();
        }

        // Join Card
        float box2Y = box1Y + 110f;
        EnchantedNatureGUI.DrawOrganicScroll(new Rect(boxX, box2Y, boxW, 115f), new Color(0.85f, 0.95f, 0.9f, 0.95f));

        boldStyle.normal.textColor = new Color(0.2f, 0.8f, 1f);
        GUI.Label(new Rect(boxX + 18f, box2Y + 12f, boxW - 200f, 22f), joinTitle, boldStyle);

        GUI.Label(new Rect(boxX + 18f, box2Y + 36f, boxW - 200f, 20f), joinDesc, infoStyle);

        onlineConnectIP = GUI.TextField(new Rect(boxX + 18f, box2Y + 62f, 240f, 32f), onlineConnectIP);

        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(boxX + boxW - 175f, box2Y + 45f, 155f, 46f), joinBtn, new Color(0.2f, 0.8f, 1f), 12))
        {
            StartOnlineClient();
        }

        // Back button
        float btmY = modalY + modalHeight - 48f;
        if (EnchantedNatureGUI.DrawOrganicLeafButton(new Rect(modalX + (modalWidth - 200f) / 2f, btmY, 200f, 34f), backBtn, Color.white, 11))
        {
            inOnlineSubmenu = false;
        }
    }
}