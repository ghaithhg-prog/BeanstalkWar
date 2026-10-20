using UnityEngine;

/// <summary>
/// Shared procedural renderer for Enchanted Forest AAA GUI:
/// Renders crisp, sleek dark-emerald & gold beveled panels, headers, cards, and interactive buttons
/// without any blurry stretched textures.
/// </summary>
public static class EnchantedNatureGUI
{
    private static Texture2D panelGradientTex;
    private static Texture2D buttonGradientTex;

    static EnchantedNatureGUI()
    {
        GenerateTextures();
    }

    private static void GenerateTextures()
    {
        // 1. Sleek vertical gradient for panels
        panelGradientTex = new Texture2D(1, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
        {
            float t = y / 63f;
            Color c = Color.Lerp(new Color(0.04f, 0.08f, 0.06f, 0.98f), new Color(0.09f, 0.16f, 0.12f, 0.98f), t);
            panelGradientTex.SetPixel(0, y, c);
        }
        panelGradientTex.wrapMode = TextureWrapMode.Clamp;
        panelGradientTex.Apply();

        // 2. Crisp vertical gradient for buttons
        buttonGradientTex = new Texture2D(1, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            float t = y / 31f;
            float b = Mathf.Lerp(0.75f, 1.0f, t);
            buttonGradientTex.SetPixel(0, y, new Color(b, b, b, 1f));
        }
        buttonGradientTex.wrapMode = TextureWrapMode.Clamp;
        buttonGradientTex.Apply();
    }

    /// <summary>
    /// Draws a sleek, crisp dark-emerald & gold framed panel (replaces the old blurry stretched blob)
    /// </summary>
    public static void DrawOrganicScroll(Rect rect, Color tint)
    {
        if (panelGradientTex == null) GenerateTextures();

        // Outer soft shadow
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(rect.x + 4f, rect.y + 4f, rect.width, rect.height), Texture2D.whiteTexture);

        // Main dark emerald gradient backdrop
        Color bgTint = (tint.r > 0.7f && tint.g > 0.7f && tint.b > 0.7f)
            ? new Color(0.06f, 0.11f, 0.08f, 0.97f)
            : new Color(Mathf.Clamp01(tint.r), Mathf.Clamp01(tint.g), Mathf.Clamp01(tint.b), Mathf.Max(0.94f, tint.a));
        GUI.color = bgTint;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        // Subtle top sheen
        GUI.color = new Color(0.25f, 0.65f, 0.40f, 0.08f);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height * 0.35f), Texture2D.whiteTexture);

        // Outer gold/emerald border
        Color borderCol = new Color(0.32f, 0.78f, 0.45f, 0.85f);
        DrawCrispBorder(rect, borderCol, 2f);

        // Inner hairline gold trim
        Rect inner = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f);
        DrawCrispBorder(inner, new Color(0.95f, 0.82f, 0.28f, 0.25f), 1f);

        // Corner gold diamond accents
        DrawCornerAccents(rect, new Color(0.95f, 0.82f, 0.28f, 0.9f));
        GUI.color = Color.white;
    }

    /// <summary>
    /// Draws a symmetrical header crest
    /// </summary>
    public static void DrawButterflyCrest(Rect rect, Color tint)
    {
        DrawLeafPlaque(rect, new Color(0.06f, 0.12f, 0.08f, 0.96f), new Color(0.95f, 0.82f, 0.28f, 0.9f));
    }

    /// <summary>
    /// Renders a sleek, responsive AAA fantasy button with hover highlight and crisp gold/emerald border
    /// </summary>
    public static bool DrawOrganicLeafButton(Rect rect, string text, Color accent, int fontSize = 12)
    {
        if (buttonGradientTex == null) GenerateTextures();

        bool isHover = rect.Contains(Event.current.mousePosition);
        float boost = isHover ? 1.35f : 1.0f;

        // Drop shadow
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), Texture2D.whiteTexture);

        // Button fill
        Color fill = new Color(
            Mathf.Clamp01(accent.r * 0.28f * boost),
            Mathf.Clamp01(accent.g * 0.32f * boost),
            Mathf.Clamp01(accent.b * 0.28f * boost),
            0.96f
        );
        GUI.color = fill;
        GUI.DrawTexture(rect, buttonGradientTex, ScaleMode.StretchToFill);

        // Top gloss highlight
        GUI.color = new Color(1f, 1f, 1f, isHover ? 0.14f : 0.06f);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height * 0.42f), Texture2D.whiteTexture);

        // Crisp glowing rim
        Color rim = isHover
            ? new Color(1f, 0.92f, 0.45f, 1f)
            : new Color(accent.r, accent.g, accent.b, 0.92f);
        DrawCrispBorder(rect, rim, isHover ? 2f : 1.5f);
        GUI.color = Color.white;

        GUIStyle btnStyle = new GUIStyle(GUI.skin.label);
        btnStyle.fontSize = fontSize;
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.alignment = TextAnchor.MiddleCenter;
        btnStyle.normal.textColor = isHover ? new Color(1f, 0.98f, 0.78f) : Color.white;

        GUI.Label(rect, text, btnStyle);

        Color oldColor = GUI.color;
        GUI.color = Color.clear;
        bool clicked = GUI.Button(rect, "");
        GUI.color = oldColor;

        return clicked;
    }

    /// <summary>
    /// Draws a crisp card/plaque with customized background tint and border color
    /// </summary>
    public static void DrawLeafPlaque(Rect rect, Color bg, Color border)
    {
        // Subtle shadow
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, rect.width, rect.height), Texture2D.whiteTexture);

        // Solid crisp background
        GUI.color = new Color(bg.r, bg.g, bg.b, Mathf.Max(0.95f, bg.a));
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        // Subtle top highlight
        GUI.color = new Color(border.r, border.g, border.b, 0.08f);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, Mathf.Min(28f, rect.height * 0.3f)), Texture2D.whiteTexture);

        // Crisp border & corners
        DrawCrispBorder(rect, border, 2f);
        DrawCornerAccents(rect, border);
        GUI.color = Color.white;
    }

    /// <summary>
    /// Draws a clean plate row/card with background and subtle border
    /// </summary>
    public static void DrawWoodPlate(Rect rect, Color bg, Color border)
    {
        GUI.color = bg;
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);

        if (border.a > 0.05f)
        {
            DrawCrispBorder(rect, border, 1.5f);
        }
        GUI.color = Color.white;
    }

    /// <summary>
    /// Renders a golden-trimmed interactive button
    /// </summary>
    public static bool DrawWoodButton(Rect rect, string text, int fontSize = 13)
    {
        return DrawOrganicLeafButton(rect, text, new Color(0.95f, 0.82f, 0.28f), fontSize);
    }

    public static void DrawCrispBorder(Rect rect, Color color, float thickness)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private static void DrawCornerAccents(Rect rect, Color color)
    {
        float len = Mathf.Min(12f, rect.width * 0.08f, rect.height * 0.08f);
        float t = 3f;
        GUI.color = color;
        // Top-Left
        GUI.DrawTexture(new Rect(rect.x, rect.y, len, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, t, len), Texture2D.whiteTexture);
        // Top-Right
        GUI.DrawTexture(new Rect(rect.x + rect.width - len, rect.y, len, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width - t, rect.y, t, len), Texture2D.whiteTexture);
        // Bottom-Left
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - t, len, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - len, t, len), Texture2D.whiteTexture);
        // Bottom-Right
        GUI.DrawTexture(new Rect(rect.x + rect.width - len, rect.y + rect.height - t, len, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width - t, rect.y + rect.height - len, t, len), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
