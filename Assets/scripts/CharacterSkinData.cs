using UnityEngine;
using System;

/// <summary>
/// بيانات السكنات ونقاط القوة المختلفة للشخصيات
/// تتفاوت القوة والمظهر بحسب:
/// 1. خبرة اللاعب ومستواه في اللعبة
/// 2. عدد الإعلانات التي شاهدها
/// 3. عمليات الشراء من المتجر
/// </summary>
[Serializable]
public class CharacterSkin
{
    public string skinName;
    public string description;
    public Color skinColor;
    public Color emissionColor;

    // مضاعفات القوة (Power Stats)
    public float healthMultiplier = 1.0f;
    public float damageMultiplier = 1.0f;
    public float speedMultiplier = 1.0f;

    // شروط الفتح (Unlock Criteria)
    public enum UnlockMethod
    {
        Free,           // مجاني ومتاح للجميع
        AccountLevel,   // يفتح بحسب خبرة اللاعب في اللعبة (Account Level)
        WatchAds,       // يفتح بعد مشاهدة عدد معين من الإعلانات
        PurchaseCoins,  // يفتح بعمليات الشراء بالعملات الذهبية
        PurchaseGems    // يفتح بالجواهر
    }

    public UnlockMethod unlockMethod = UnlockMethod.Free;
    public int requiredLevel = 1;
    public int requiredAds = 0;
    public int requiredCost = 0;
}

public static class CharacterSkinDatabase
{
    /// <summary>
    /// الحصول على قائمة السكنات المتاحة لأي شخصية مع ترقيات القوة ومصادر الفتح
    /// </summary>
    public static CharacterSkin[] GetSkinsForCharacter(int characterId)
    {
        return new CharacterSkin[]
        {
            // Skin 0: Standard Initiate Garb (Free)
            new CharacterSkin
            {
                skinName = "Standard Outfit",
                description = "Balanced initiate armor for forest warriors",
                skinColor = Color.white,
                emissionColor = Color.black,
                healthMultiplier = 1.0f,
                damageMultiplier = 1.0f,
                speedMultiplier = 1.0f,
                unlockMethod = CharacterSkin.UnlockMethod.Free
            },

            // Skin 1: Emerald Guardian (Account Level 2 or 150 Coins)
            new CharacterSkin
            {
                skinName = "Emerald Guardian",
                description = "Ancient forest barkmail: +20% HP & +10% Speed",
                skinColor = new Color(0.18f, 0.85f, 0.35f), // Emerald Green
                emissionColor = new Color(0.1f, 0.45f, 0.2f),
                healthMultiplier = 1.20f,
                damageMultiplier = 1.05f,
                speedMultiplier = 1.10f,
                unlockMethod = CharacterSkin.UnlockMethod.AccountLevel,
                requiredLevel = 2,
                requiredCost = 150
            },

            // Skin 2: Crimson Berserker (Watch 1 Ad or 200 Coins)
            new CharacterSkin
            {
                skinName = "Crimson Berserker",
                description = "Molten flame blades: +30% Attack Damage & +10% Speed",
                skinColor = new Color(0.95f, 0.25f, 0.15f), // Fire Crimson
                emissionColor = new Color(0.7f, 0.2f, 0.05f),
                healthMultiplier = 1.05f,
                damageMultiplier = 1.30f,
                speedMultiplier = 1.10f,
                unlockMethod = CharacterSkin.UnlockMethod.WatchAds,
                requiredAds = 1,
                requiredCost = 200
            },

            // Skin 3: Shadow Phantom (300 Acorn Coins)
            new CharacterSkin
            {
                skinName = "Shadow Phantom",
                description = "Stealth nightwalker cowl: +25% DMG & +35% Sprint Speed",
                skinColor = new Color(0.35f, 0.25f, 0.65f), // Dark Violet
                emissionColor = new Color(0.3f, 0.15f, 0.5f),
                healthMultiplier = 1.10f,
                damageMultiplier = 1.25f,
                speedMultiplier = 1.35f,
                unlockMethod = CharacterSkin.UnlockMethod.PurchaseCoins,
                requiredCost = 300
            },

            // Skin 4: Golden Celestial Titan (500 Coins or 25 Gems)
            new CharacterSkin
            {
                skinName = "Golden Celestial Titan",
                description = "Imperial Relic Armor: +40% HP, +40% DMG & +20% Speed",
                skinColor = new Color(1f, 0.85f, 0.2f), // Radiant Gold
                emissionColor = new Color(0.9f, 0.7f, 0.1f),
                healthMultiplier = 1.40f,
                damageMultiplier = 1.40f,
                speedMultiplier = 1.20f,
                unlockMethod = CharacterSkin.UnlockMethod.PurchaseGems,
                requiredCost = 25
            }
        };
    }
}
