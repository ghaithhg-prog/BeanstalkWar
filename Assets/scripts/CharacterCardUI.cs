using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CharacterCardUI : MonoBehaviour
{
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI roleText;
    public TextMeshProUGUI countText;
    public Button selectButton;

    private CharacterData characterData;
    private Action<int> onSelected;

    public void Setup(
        CharacterData data,
        int currentCount,
        Action<int> selectCallback)
    {
        characterData = data;
        onSelected = selectCallback;

        characterNameText.text = data.characterName;
        roleText.text = data.role.ToString();

        UpdateCount(currentCount);

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(Select);
    }

    void Select()
    {
        if (characterData == null)
            return;

        onSelected?.Invoke(
            characterData.characterID
        );
    }

    public void UpdateCount(int currentCount)
    {
        if (characterData == null)
            return;

        countText.text =
            currentCount + "/" +
            characterData.maxPerTeam;

        selectButton.interactable =
            currentCount <
            characterData.maxPerTeam;
    }

    public int GetCharacterID()
    {
        return characterData != null
            ? characterData.characterID
            : -1;
    }
}