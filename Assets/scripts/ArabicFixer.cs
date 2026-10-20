using System.Text;
using System.Collections.Generic;

/// <summary>
/// ArabicFixer:
/// Pure C# Arabic glyph shaper & bidirectional text reverser designed specifically for Unity IMGUI.
/// Resolves the issue of isolated/reversed Arabic characters in Unity GUI by mapping characters
/// to their proper cursive contextual presentation forms (Isolated, Initial, Medial, Final),
/// handling ligatures (such as Lam-Alef لا), and reversing glyph order so Unity IMGUI renders
/// Arabic from Right-to-Left seamlessly.
/// </summary>
public static class ArabicFixer
{
    private struct ArabicCharMapping
    {
        public char isolated;
        public char initial;
        public char medial;
        public char final;
        public bool connectsToNext;

        public ArabicCharMapping(char iso, char ini, char med, char fin, bool connects)
        {
            isolated = iso;
            initial = ini;
            medial = med;
            final = fin;
            connectsToNext = connects;
        }
    }

    private static readonly Dictionary<char, ArabicCharMapping> charMap = new Dictionary<char, ArabicCharMapping>()
    {
        // ء Hamza (Does not connect)
        { '\u0621', new ArabicCharMapping('\uFE80', '\uFE80', '\uFE80', '\uFE80', false) },
        // آ Alef with Madda (Connects from previous only)
        { '\u0622', new ArabicCharMapping('\uFE81', '\uFE81', '\uFE82', '\uFE82', false) },
        // أ Alef with Hamza above
        { '\u0623', new ArabicCharMapping('\uFE83', '\uFE83', '\uFE84', '\uFE84', false) },
        // ؤ Waw with Hamza
        { '\u0624', new ArabicCharMapping('\uFE85', '\uFE85', '\uFE86', '\uFE86', false) },
        // إ Alef with Hamza below
        { '\u0625', new ArabicCharMapping('\uFE87', '\uFE87', '\uFE88', '\uFE88', false) },
        // ئ Yeh with Hamza (Connects both ways)
        { '\u0626', new ArabicCharMapping('\uFE89', '\uFE8B', '\uFE8C', '\uFE8A', true) },
        // ا Alef
        { '\u0627', new ArabicCharMapping('\uFE8D', '\uFE8D', '\uFE8E', '\uFE8E', false) },
        // ب Beh
        { '\u0628', new ArabicCharMapping('\uFE8F', '\uFE91', '\uFE92', '\uFE90', true) },
        // ة Teh Marbuta (Connects from previous only)
        { '\u0629', new ArabicCharMapping('\uFE93', '\uFE93', '\uFE94', '\uFE94', false) },
        // ت Teh
        { '\u062A', new ArabicCharMapping('\uFE95', '\uFE97', '\uFE98', '\uFE96', true) },
        // ث Theh
        { '\u062B', new ArabicCharMapping('\uFE99', '\uFE9B', '\uFE9C', '\uFE9A', true) },
        // ج Jeem
        { '\u062C', new ArabicCharMapping('\uFE9D', '\uFE9F', '\uFEA0', '\uFE9E', true) },
        // ح Hah
        { '\u062D', new ArabicCharMapping('\uFEA1', '\uFEA3', '\uFEA4', '\uFEA2', true) },
        // خ Khah
        { '\u062E', new ArabicCharMapping('\uFEA5', '\uFEA7', '\uFEA8', '\uFEA6', true) },
        // د Dal
        { '\u062F', new ArabicCharMapping('\uFEA9', '\uFEA9', '\uFEAA', '\uFEAA', false) },
        // ذ Thal
        { '\u0630', new ArabicCharMapping('\uFEAB', '\uFEAB', '\uFEAC', '\uFEAC', false) },
        // ر Reh
        { '\u0631', new ArabicCharMapping('\uFEAD', '\uFEAD', '\uFEAE', '\uFEAE', false) },
        // ز Zain
        { '\u0632', new ArabicCharMapping('\uFEAF', '\uFEAF', '\uFEB0', '\uFEB0', false) },
        // س Seen
        { '\u0633', new ArabicCharMapping('\uFEB1', '\uFEB3', '\uFEB4', '\uFEB2', true) },
        // ش Sheen
        { '\u0634', new ArabicCharMapping('\uFEB5', '\uFEB7', '\uFEB8', '\uFEB6', true) },
        // ص Sad
        { '\u0635', new ArabicCharMapping('\uFEB9', '\uFEBB', '\uFEBC', '\uFEBA', true) },
        // ض Dad
        { '\u0636', new ArabicCharMapping('\uFEBD', '\uFEBF', '\uFEC0', '\uFEBE', true) },
        // ط Tah
        { '\u0637', new ArabicCharMapping('\uFEC1', '\uFEC3', '\uFEC4', '\uFEC2', true) },
        // ظ Zah
        { '\u0638', new ArabicCharMapping('\uFEC5', '\uFEC7', '\uFEC8', '\uFEC6', true) },
        // ع Ain
        { '\u0639', new ArabicCharMapping('\uFEC9', '\uFECB', '\uFECC', '\uFECA', true) },
        // غ Ghain
        { '\u063A', new ArabicCharMapping('\uFECD', '\uFECF', '\uFED0', '\uFECE', true) },
        // ف Feh
        { '\u0641', new ArabicCharMapping('\uFED1', '\uFED3', '\uFED4', '\uFED2', true) },
        // ق Qaf
        { '\u0642', new ArabicCharMapping('\uFED5', '\uFED7', '\uFED8', '\uFED6', true) },
        // ك Kaf
        { '\u0643', new ArabicCharMapping('\uFED9', '\uFEDB', '\uFEDC', '\uFEDA', true) },
        // ل Lam
        { '\u0644', new ArabicCharMapping('\uFEDD', '\uFEDF', '\uFEE0', '\uFEDE', true) },
        // م Meem
        { '\u0645', new ArabicCharMapping('\uFEE1', '\uFEE3', '\uFEE4', '\uFEE2', true) },
        // ن Noon
        { '\u0646', new ArabicCharMapping('\uFEE5', '\uFEE7', '\uFEE8', '\uFEE6', true) },
        // ه Heh
        { '\u0647', new ArabicCharMapping('\uFEE9', '\uFEEB', '\uFEEC', '\uFEEA', true) },
        // و Waw
        { '\u0648', new ArabicCharMapping('\uFEED', '\uFEED', '\uFEEE', '\uFEEE', false) },
        // ى Alef Maksura
        { '\u0649', new ArabicCharMapping('\uFEEF', '\uFEEF', '\uFEF0', '\uFEF0', false) },
        // ي Yeh
        { '\u064A', new ArabicCharMapping('\uFEF1', '\uFEF3', '\uFEF4', '\uFEF2', true) },
        // پ Peh (Perso-Arabic)
        { '\u067E', new ArabicCharMapping('\uFB56', '\uFB58', '\uFB59', '\uFB57', true) },
        // چ Tcheh
        { '\u0686', new ArabicCharMapping('\uFB7A', '\uFB7C', '\uFB7D', '\uFB7B', true) },
        // ژ Zheh (Perso-Arabic)
        { '\u0698', new ArabicCharMapping('\uFB8A', '\uFB8A', '\uFB8B', '\uFB8B', false) },
        // ک Keheh (Farsi Kaf)
        { '\u06A9', new ArabicCharMapping('\uFB8E', '\uFB90', '\uFB91', '\uFB8F', true) },
        // گ Gaf
        { '\u06AF', new ArabicCharMapping('\uFB92', '\uFB94', '\uFB95', '\uFB93', true) },
        // ی Farsi Yeh
        { '\u06CC', new ArabicCharMapping('\uFBFC', '\uFBFE', '\uFBFF', '\uFBFD', true) }
    };

    public static bool IsArabicChar(char c)
    {
        return (c >= '\u0600' && c <= '\u06FF') || (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF');
    }

    private static char MirrorBracket(char c)
    {
        switch (c)
        {
            case '(': return ')';
            case ')': return '(';
            case '[': return ']';
            case ']': return '[';
            case '{': return '}';
            case '}': return '{';
            case '<': return '>';
            case '>': return '<';
            case '«': return '»';
            case '»': return '«';
            default: return c;
        }
    }

    private static string RemoveTashkeel(string str)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            if (c >= '\u064B' && c <= '\u065F') continue; // Fatha, Damma, Kasra, Sukun, Tanween
            if (c == '\u0670') continue; // Dagger alef
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Shapes Arabic cursive characters and reverses them so Unity IMGUI renders RTL correctly.
    /// Preserves numbers, Latin words, symbols, and punctuation in their correct natural order.
    /// </summary>
    public static string Fix(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Strip diacritics to guarantee smooth glyph shaping in IMGUI fonts
        string clean = RemoveTashkeel(input);

        // Break text into lines so multiline text formats correctly
        string[] lines = clean.Split('\n');
        StringBuilder result = new StringBuilder();

        for (int l = 0; l < lines.Length; l++)
        {
            if (l > 0) result.Append('\n');
            result.Append(FixLine(lines[l]));
        }

        return result.ToString();
    }

    private static string FixLine(string line)
    {
        if (string.IsNullOrEmpty(line)) return line;

        // First pass: Shape the Arabic characters
        StringBuilder shaped = new StringBuilder();
        int len = line.Length;

        for (int i = 0; i < len; i++)
        {
            char current = line[i];

            // Handle Lam-Alef ligatures
            if (current == '\u0644' && i + 1 < len)
            {
                char next = line[i + 1];
                char ligature = '\0';
                bool prevConnects = (i > 0 && charMap.ContainsKey(line[i - 1]) && charMap[line[i - 1]].connectsToNext);

                if (next == '\u0627') ligature = prevConnects ? '\uFEFC' : '\uFEFB'; // لا
                else if (next == '\u0623') ligature = prevConnects ? '\uFEF8' : '\uFEF7'; // لأ
                else if (next == '\u0625') ligature = prevConnects ? '\uFEFA' : '\uFEF9'; // لإ
                else if (next == '\u0622') ligature = prevConnects ? '\uFEF6' : '\uFEF5'; // لآ

                if (ligature != '\0')
                {
                    shaped.Append(ligature);
                    i++; // skip next alef
                    continue;
                }
            }

            if (charMap.ContainsKey(current))
            {
                ArabicCharMapping map = charMap[current];
                bool prevConnects = (i > 0 && charMap.ContainsKey(line[i - 1]) && charMap[line[i - 1]].connectsToNext);
                bool nextConnects = false;

                if (i + 1 < len && charMap.ContainsKey(line[i + 1]))
                {
                    nextConnects = true;
                }

                if (prevConnects && nextConnects && map.connectsToNext)
                {
                    shaped.Append(map.medial);
                }
                else if (prevConnects)
                {
                    shaped.Append(map.final);
                }
                else if (nextConnects && map.connectsToNext)
                {
                    shaped.Append(map.initial);
                }
                else
                {
                    shaped.Append(map.isolated);
                }
            }
            else
            {
                shaped.Append(current);
            }
        }

        // Second pass: Reverse segments of Arabic text while keeping numbers and Latin segments in natural order
        string shapedStr = shaped.ToString();
        List<string> tokens = new List<string>();
        StringBuilder currentToken = new StringBuilder();
        bool inArabic = false;

        for (int i = 0; i < shapedStr.Length; i++)
        {
            char c = shapedStr[i];
            bool isAr = IsArabicChar(c);

            if (i == 0)
            {
                inArabic = isAr;
                currentToken.Append(c);
            }
            else if (isAr == inArabic)
            {
                currentToken.Append(c);
            }
            else
            {
                tokens.Add(currentToken.ToString());
                currentToken.Clear();
                inArabic = isAr;
                currentToken.Append(c);
            }
        }

        if (currentToken.Length > 0)
        {
            tokens.Add(currentToken.ToString());
        }

        // Check if line contains any Arabic
        bool hasArabic = false;
        foreach (string t in tokens)
        {
            if (t.Length > 0 && IsArabicChar(t[0]))
            {
                hasArabic = true;
                break;
            }
        }

        if (!hasArabic) return line;

        // Invert overall token order for RTL, and reverse letters within Arabic words
        StringBuilder reversedLine = new StringBuilder();
        for (int t = tokens.Count - 1; t >= 0; t--)
        {
            string token = tokens[t];
            if (token.Length > 0 && IsArabicChar(token[0]))
            {
                // Reverse characters within Arabic word
                for (int c = token.Length - 1; c >= 0; c--)
                {
                    reversedLine.Append(token[c]);
                }
            }
            else
            {
                // Non-Arabic (numbers, Latin words, punctuation) preserved with mirrored brackets
                for (int c = 0; c < token.Length; c++)
                {
                    reversedLine.Append(MirrorBracket(token[c]));
                }
            }
        }

        return reversedLine.ToString();
    }
}
