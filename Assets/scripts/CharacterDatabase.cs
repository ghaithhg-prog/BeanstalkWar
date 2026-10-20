using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterDatabase",
    menuName = "Game/Character Database"
)]
public class CharacterDatabase : ScriptableObject
{
    [Header("All Characters")]
    public CharacterData[] characters;

    public CharacterData GetCharacter(int characterID)
    {
        foreach (CharacterData character in characters)
        {
            if (character != null &&
                character.characterID == characterID)
            {
                return character;
            }
        }

        Debug.LogWarning(
            "Character ID not found: " + characterID
        );

        return null;
    }
}