using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpOptionUI : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public Text nameText;
    public Text descriptionText;
    public Text rarityText;
    public Text durationText; // 🔥 TAMBAHKAN INI
    public Button selectButton;

    private PowerUpData data;
    private PowerUpSelectionUI parentUI;

    public void Setup(PowerUpData powerUpData, PowerUpSelectionUI ui)
    {
        data = powerUpData;
        parentUI = ui;

        iconImage.sprite = data.icon;
        nameText.text = data.powerUpName;
        descriptionText.text = data.description;

        // Rarity
        rarityText.text = data.rarity.ToString().ToUpper();
        switch (data.rarity)
        {
            case PowerUpData.Rarity.Common:
                rarityText.color = Color.white;
                break;
            case PowerUpData.Rarity.Rare:
                rarityText.color = new Color(0.27f, 0.53f, 1f); // #4488FF
                break;
            case PowerUpData.Rarity.Legendary:
                rarityText.color = new Color(1f, 0.8f, 0f); // #FFCC00
                break;
        }

        // 🔥 DURATION
        if (data.duration == PowerUpData.DurationType.Permanent)
        {
            durationText.text = "PERMANENT";
            durationText.color = new Color(0.5f, 0.5f, 0.5f);
        }
        else
        {
            durationText.text = $"{data.temporaryWaveCount} WAVES";
            durationText.color = new Color(1f, 0.6f, 0.2f); // Orange
        }

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => parentUI.OnOptionSelected(data));
    }
}