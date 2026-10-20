using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Medieval Archery Crosshair HUD (نظام التنشين وقنص السهام الكلاسيكي الأسطوري):
/// 1. Replaces the primitive white square with a sleek, authentic medieval fantasy archery reticle
///    (Curved bow brackets, fine center aiming pip, elevation calibration marks, subtle dark drop shadow).
/// 2. Dynamic Target Detection: Turns glowing red when locked onto an enemy or the sacred tree.
/// 3. Dynamic Shot Pulse: Gently expands and contracts upon firing an arrow for satisfying game feel.
/// 4. Auto-hides when dead or during pause/language modals.
/// </summary>
public class MedievalCrosshairHUD : MonoBehaviour
{
    private Image crosshairImage;
    private RectTransform rectTransform;
    private static Sprite cachedReticleSprite;
    private float currentScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitializeCrosshair()
    {
        Image[] allImages = FindObjectsByType<Image>(FindObjectsInactive.Include);
        foreach (var img in allImages)
        {
            if (img != null && img.gameObject.name == "Crosshair")
            {
                if (img.GetComponent<MedievalCrosshairHUD>() == null)
                {
                    img.gameObject.AddComponent<MedievalCrosshairHUD>();
                }
                return;
            }
        }

        GameObject crosshairObj = GameObject.Find("Crosshair");
        if (crosshairObj != null && crosshairObj.GetComponent<MedievalCrosshairHUD>() == null)
        {
            crosshairObj.AddComponent<MedievalCrosshairHUD>();
        }
    }

    void Awake()
    {
        crosshairImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        if (crosshairImage == null)
        {
            crosshairImage = gameObject.AddComponent<Image>();
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        SetupMedievalReticle();
    }

    void SetupMedievalReticle()
    {
        if (cachedReticleSprite == null)
        {
            cachedReticleSprite = GenerateMedievalArcherySprite();
        }

        if (crosshairImage != null)
        {
            crosshairImage.sprite = cachedReticleSprite;
            crosshairImage.raycastTarget = false; // لا يعترض نقرات الماوس إطلاقاً
            crosshairImage.color = new Color(1f, 0.95f, 0.88f, 0.92f); // عاجي ذهبي أثري
        }

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(56f, 56f);
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    void Update()
    {
        if (crosshairImage == null) return;

        // إخفاء الكروس إير إذا كان اللاعب ميتاً أو في القوائم
        if (Health.IsLocalPlayerDead || 
            InGamePauseMenuUI.IsPauseMenuOpen || 
            LanguageSelectionUI.IsLanguageModalOpen || 
            CharacterSelectionUI.IsSelectionOpen)
        {
            crosshairImage.enabled = false;
            return;
        }

        crosshairImage.enabled = true;

        // 1. فحص الهدف تحت الكروس إير (Target Lock Highlighting)
        bool isTargetingEnemy = false;
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 150f);

            GameObject localPlayerObj = PlayerMovement.LocalPlayer != null ? PlayerMovement.LocalPlayer.gameObject : null;
            PlayerMovement.TeamType localTeam = PlayerMovement.LocalPlayer != null ? PlayerMovement.LocalPlayer.team : PlayerMovement.TeamType.Protector;

            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                if (localPlayerObj != null && (h.collider.gameObject == localPlayerObj || h.collider.transform.IsChildOf(localPlayerObj.transform)))
                    continue;

                Health targetHealth = h.collider.GetComponentInParent<Health>();
                if (targetHealth != null && !targetHealth.isDead)
                {
                    CharacterCombat otherCombat = targetHealth.GetComponent<CharacterCombat>();
                    PlayerMovement otherPm = targetHealth.GetComponent<PlayerMovement>();
                    BotController otherBot = targetHealth.GetComponent<BotController>();

                    PlayerMovement.TeamType targetTeam = PlayerMovement.TeamType.Protector;
                    if (otherCombat != null) targetTeam = otherCombat.team;
                    else if (otherPm != null) targetTeam = otherPm.team;
                    else if (otherBot != null) targetTeam = otherBot.team;

                    if (targetTeam != localTeam)
                    {
                        isTargetingEnemy = true;
                        break;
                    }
                }

                // أو تصويب المدمر نحو الشجرة
                if (localTeam == PlayerMovement.TeamType.Destroyer)
                {
                    if (h.collider.GetComponentInParent<TreeGrowth>() != null)
                    {
                        isTargetingEnemy = true;
                        break;
                    }
                }
            }
        }

        // 2. تحديث اللون (أحمر قتالي متوهج عند وجود هدف، أو عاجي ذهبي نقي في الوضع العادي)
        Color targetColor = isTargetingEnemy 
            ? new Color(1.0f, 0.22f, 0.18f, 0.98f) 
            : new Color(1.0f, 0.95f, 0.88f, 0.90f);

        crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.deltaTime * 14f);

        // 3. نبض الارتداد عند إطلاق السهم
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F) || CharacterCombat.MobileAttackHeld)
        {
            currentScale = 1.25f;
        }

        currentScale = Mathf.Lerp(currentScale, 1.0f, Time.deltaTime * 10f);
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(56f * currentScale, 56f * currentScale);
        }
    }

    /// <summary>
    /// توليد ريتيكل قنص السهام الكلاسيكي الأسطوري بدقة عالية وتدرج ألفا فائق النعومة (128x128)
    /// </summary>
    private static Sprite GenerateMedievalArcherySprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = 0f;
                Color col = Color.white;

                // 1. النقطة المركزية الفائقة الدقة (Center Aiming Pip)
                if (dist <= 3.2f)
                {
                    alpha = Mathf.Clamp01(3.2f - dist);
                    col = Color.white;
                }
                // هالة داكنة خفيفة حول نقطة التنشين لعزلها عن أي خلفية فاتحة
                else if (dist > 3.2f && dist <= 5.0f)
                {
                    alpha = Mathf.Clamp01(1f - (dist - 3.2f) / 1.8f) * 0.45f;
                    col = new Color(0f, 0f, 0f, 1f);
                }

                // 2. قوسي القوس الأيسر والأيمن (Curved Archery Chevrons: ⟨ • ⟩)
                float absDx = Mathf.Abs(dx);
                if (absDx >= 12f && absDx <= 28f && Mathf.Abs(dy) <= 24f)
                {
                    // مسار القوس المنحني
                    float curve = 18f + (dy * dy) * 0.015f;
                    float diff = Mathf.Abs(absDx - curve);
                    if (diff <= 1.8f)
                    {
                        float a = Mathf.Clamp01(1.8f - diff);
                        if (a > alpha)
                        {
                            alpha = a * 0.95f;
                            col = new Color(1f, 0.98f, 0.92f);
                        }
                    }
                    // ظل خارجي للقوس
                    else if (diff <= 3.2f)
                    {
                        float a = Mathf.Clamp01(3.2f - diff) * 0.4f;
                        if (a > alpha)
                        {
                            alpha = a;
                            col = new Color(0.05f, 0.05f, 0.05f);
                        }
                    }
                }

                // 3. علامات المسافة وتقدير هبوط السهم عمودياً (Elevation Calibration Ticks: - - -)
                if (Mathf.Abs(dx) <= 4.5f && dy < -10f && dy > -45f)
                {
                    float[] tickYs = new float[] { -15f, -27f, -39f };
                    float[] tickWidths = new float[] { 4.0f, 3.0f, 2.0f };

                    for (int t = 0; t < tickYs.Length; t++)
                    {
                        float tDiffY = Mathf.Abs(dy - tickYs[t]);
                        if (tDiffY <= 1.2f && Mathf.Abs(dx) <= tickWidths[t])
                        {
                            float a = Mathf.Clamp01(1.2f - tDiffY) * 0.85f;
                            if (a > alpha)
                            {
                                alpha = a;
                                col = new Color(1f, 0.95f, 0.85f);
                            }
                        }
                    }
                }

                // 4. رأس السهم العلوي المصغر (Top Arrow Guide Tip: ^)
                if (dy >= 14f && dy <= 22f)
                {
                    float expectedY = 22f - Mathf.Abs(dx) * 1.4f;
                    float diffY = Mathf.Abs(dy - expectedY);
                    if (diffY <= 1.3f && Mathf.Abs(dx) <= 5.5f)
                    {
                        float a = Mathf.Clamp01(1.3f - diffY) * 0.9f;
                        if (a > alpha)
                        {
                            alpha = a;
                            col = new Color(1f, 0.95f, 0.85f);
                        }
                    }
                }

                pixels[y * size + x] = new Color(col.r, col.g, col.b, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
