using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinItemUI : MonoBehaviour
{
    private const int SecretUnlockWave = 30;
    private const int NormalMaxFontSize = 40;
    private const int LockedMaxFontSize = 30;

    [Header("UI Elements")]
    public Image skinImage;
    public Text skinNameText;
    public Text priceText;
    public Text rarityText;
    public Button actionButton;
    public Text buttonText;
    public GameObject equippedIndicator;
    
    private SkinData skinData;
    
    public void Setup(SkinData skin)
    {
        skinData = skin;
        
        skinImage.sprite = skin.skinSprite;
        skinNameText.text = skin.skinName;
        priceText.text = $"{skin.price}";
        rarityText.text = skin.rarity.ToString();
        
        SetRarityColor(skin.rarity);
        
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnButtonClick);
        UpdateUI();
    }
    
    public void UpdateUI()
    {
        bool isOwned = SkinManager.Instance.IsSkinOwned(skinData);
        bool isSecret = skinData.rarity == SkinData.Rarity.Secret;

        bool isEquipped = false;
        if (skinData.skinType == SkinData.SkinType.Character)
            isEquipped = SkinManager.Instance.currentCharacterSkin == skinData;
        else if (skinData.skinType == SkinData.SkinType.Bullet)
            isEquipped = SkinManager.Instance.currentBulletSkin == skinData;

        // 🔥 Sembunyikan harga untuk secret skin yang belum di-unlock
        if (isSecret && !isOwned)
        {
            priceText.gameObject.SetActive(false);
        }
        else
        {
            priceText.text = $"{skinData.price}";
            priceText.gameObject.SetActive(!isOwned);
        }

        equippedIndicator.SetActive(isEquipped);

        if (!isOwned)
        {
            if (isSecret)
            {
                // 🔥 Secret skin belum di-unlock → LOCKED
                buttonText.text = "LOCKED";
                buttonText.resizeTextMaxSize = LockedMaxFontSize;
                actionButton.gameObject.SetActive(true);
                actionButton.interactable = false;
            }
            else
            {
                // 🔥 Skin biasa belum dibeli → BUY
                buttonText.text = "BUY";
                buttonText.resizeTextMaxSize = NormalMaxFontSize;
                actionButton.gameObject.SetActive(true);
                actionButton.interactable = true;
            }
        }
        else if (!isEquipped)
        {
            // 🔥 Skin sudah dimiliki tapi belum dipakai → EQUIP
            buttonText.text = "EQUIP";
            buttonText.resizeTextMaxSize = NormalMaxFontSize;
            actionButton.gameObject.SetActive(true);
            actionButton.interactable = true;
        }
        else
        {
            // 🔥 Skin sedang dipakai → sembunyikan tombol
            actionButton.gameObject.SetActive(false);
        }
    }
    
    void SetRarityColor(SkinData.Rarity rarity)
    {
        switch (rarity)
        {
            case SkinData.Rarity.Common:
                rarityText.color = Color.white;
                break;
            case SkinData.Rarity.Rare:
                rarityText.color = new Color(0.27f, 0.53f, 1f);
                break;
            case SkinData.Rarity.Epic:
                rarityText.color = new Color(0.6f, 0f, 1f);
                break;
            case SkinData.Rarity.Legendary:
                rarityText.color = new Color(1f, 0.8f, 0f);
                break;
            case SkinData.Rarity.Secret:
                rarityText.color = new Color(1f, 0.25f, 0.65f);
                break;
        }
    }

    void OnButtonClick()
    {
        // 🔥 Secret skin yang masih LOCKED tidak bisa diklik
        if (skinData.rarity == SkinData.Rarity.Secret && !SkinManager.Instance.IsSkinOwned(skinData))
            return;

        bool isOwned = SkinManager.Instance.IsSkinOwned(skinData);
    
        if (!isOwned)
        {
            // 🔥 BELI SKIN (tidak auto-equip)
            if (SkinManager.Instance.BuySkin(skinData))
            {
                AudioManager.Instance?.PlaySFX("Click");
                // Setelah beli, tombol berubah jadi EQUIP
            }
            else
            {
                AudioManager.Instance?.PlaySFX("Error");
                FindObjectOfType<SkinShopUI>().ShowNotEnoughCoins();
            }
        }
        else
        {
            // 🔥 EQUIP SKIN (baik skin beli maupun secret unlock)
            SkinManager.Instance.EquipSkin(skinData);
            AudioManager.Instance?.PlaySFX("Click");
        }
    
        FindObjectOfType<SkinShopUI>().UpdateUI();
    }
}