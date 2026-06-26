using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class SkinShopUI : MonoBehaviour
{
    [Header("Main UI")]
    public GameObject shopPanel;
    public Text coinText;
    public Button closeButton;
    public Button openShopButton;
    public Button settingButton;
    
    [Header("Character Skins")]
    public Transform characterContainer;
    public GameObject characterItemPrefab;
    
    [Header("Bullet Skins")]
    public Transform bulletContainer;
    public GameObject bulletItemPrefab;
    
    [Header("Section Titles")]
    public GameObject characterTitle;
    public GameObject bulletTitle;
    public GameObject aboutButton;
    
    private List<SkinItemUI> characterItems = new List<SkinItemUI>();
    private List<SkinItemUI> bulletItems = new List<SkinItemUI>();
    
    [Header("Not Enough Coins")]
    public GameObject notEnoughText;
    
    void Start()
    {
        if (openShopButton != null) openShopButton.onClick.AddListener(OpenShop);
        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);
        
        CreateCharacterItems();
        CreateBulletItems();
        shopPanel.SetActive(false);
        
        if (openShopButton != null) openShopButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(false);
        
        // 🔥 SUBSCRIBE COIN UPDATE
        if (SkinManager.Instance != null)
            SkinManager.Instance.OnCoinsUpdated += UpdateCoinDisplay;
    }
    
    void OnDestroy()
    {
        if (SkinManager.Instance != null)
            SkinManager.Instance.OnCoinsUpdated -= UpdateCoinDisplay;
    }
    
    void CreateCharacterItems()
    {
        foreach (Transform child in characterContainer) Destroy(child.gameObject);
        characterItems.Clear();
        
        foreach (SkinData skin in SkinManager.Instance.allCharacterSkins)
        {
            GameObject itemObj = Instantiate(characterItemPrefab, characterContainer);
            SkinItemUI itemUI = itemObj.GetComponent<SkinItemUI>();
            itemUI.Setup(skin);
            characterItems.Add(itemUI);
        }
    }
    
    void CreateBulletItems()
    {
        foreach (Transform child in bulletContainer) Destroy(child.gameObject);
        bulletItems.Clear();
        
        foreach (SkinData skin in SkinManager.Instance.allBulletSkins)
        {
            GameObject itemObj = Instantiate(bulletItemPrefab, bulletContainer);
            SkinItemUI itemUI = itemObj.GetComponent<SkinItemUI>();
            itemUI.Setup(skin);
            bulletItems.Add(itemUI);
        }
    }
    
    public void UpdateUI()
    {
        UpdateCoinDisplay();
        foreach (var item in characterItems) item.UpdateUI();
        foreach (var item in bulletItems) item.UpdateUI();
    }
    
    public void UpdateCoinDisplay()
    {
        if (coinText != null && SkinManager.Instance != null)
            coinText.text = $"{SkinManager.Instance.GetCoins()}";
    }
    
    public void OpenShop()
    {
        shopPanel.SetActive(true);
        UpdateUI();

        if (closeButton != null) closeButton.gameObject.SetActive(true);
        if (settingButton != null) settingButton.gameObject.SetActive(false);
        if (openShopButton != null) openShopButton.gameObject.SetActive(false);
        if (aboutButton != null) aboutButton.gameObject.SetActive(false);
        
        AudioManager.Instance?.PlaySFX("Click");
    }
    
    public void CloseShop()
    {
        shopPanel.SetActive(false);
        
        if (openShopButton != null) openShopButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(false);
        if (settingButton != null) settingButton.gameObject.SetActive(true);
        if (aboutButton != null) aboutButton.gameObject.SetActive(true);

        AudioManager.Instance?.PlaySFX("Click");
    }

    public void ShowNotEnoughCoins()
    {
        StartCoroutine(FlashNotEnough());
    }

    IEnumerator FlashNotEnough()
    {
        if (notEnoughText != null)
        {
            notEnoughText.SetActive(true);
            yield return new WaitForSecondsRealtime(1.5f);
            notEnoughText.SetActive(false);
        }
    }
}