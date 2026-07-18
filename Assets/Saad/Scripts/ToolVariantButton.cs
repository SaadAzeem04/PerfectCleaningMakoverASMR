using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToolVariantButton : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text priceText;
    public Button selectButton;
    public GameObject equippedCheckmark; // Video wala green tick

    private ToolVariant currentVariant;
    private ToolData parentTool;
    private MaskEraser eraserManager;

    public void SetupButton(ToolVariant variant, ToolData tool, MaskEraser manager)
    {
        currentVariant = variant;
        parentTool = tool;
        eraserManager = manager;

        if (iconImage != null && variant.iconSprite != null)
        {
            iconImage.sprite = variant.iconSprite;
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        bool isUnlocked = (currentVariant.coinPrice == 0) || (PlayerPrefs.GetInt(parentTool.name + "_" + currentVariant.variantName, 0) == 1);
        string equippedName = PlayerPrefs.GetString(parentTool.name + "_Equipped", "");

        // Agar pehle se koi save nahi hai, to pehli (0 index wali) skin equipped manenge
        bool isEquipped = (equippedName == currentVariant.variantName) ||
                          (string.IsNullOrEmpty(equippedName) && parentTool.toolVariants.IndexOf(currentVariant) == 0);

        if (equippedCheckmark != null)
        {
            equippedCheckmark.SetActive(isEquipped);
        }

        if (isEquipped)
        {
            priceText.text = "Equipped";
            selectButton.interactable = false; // Already equipped hai
        }
        else if (isUnlocked)
        {
            priceText.text = "Free";
            selectButton.interactable = true;
        }
        else
        {
            priceText.text = currentVariant.coinPrice.ToString();
            selectButton.interactable = true;
        }
    }

    public void OnButtonClick()
    {
        bool isUnlocked = (currentVariant.coinPrice == 0) || (PlayerPrefs.GetInt(parentTool.name + "_" + currentVariant.variantName, 0) == 1);

        if (isUnlocked)
        {
            EquipThisSkin();
        }
        else
        {
            int currentCoins = PlayerPrefs.GetInt("Coins", 0);
            if (currentCoins >= currentVariant.coinPrice)
            {
                currentCoins -= currentVariant.coinPrice;
                PlayerPrefs.SetInt("Coins", currentCoins);
                PlayerPrefs.SetInt(parentTool.name + "_" + currentVariant.variantName, 1); // Unlock save
                PlayerPrefs.Save();

                // =========================================================================
                //  100% EXACT MATCH WITH YOUR COIN MANAGER (From MaskEraser.cs Line 446):
                // =========================================================================
                if (CoinManager.Instance != null)
                {
                    // Minus me value dene se coins katenge aur Swoop animation chalegi!
                    CoinManager.Instance.TriggerCoinSwoopAnimation(-currentVariant.coinPrice);
                }
                else
                {
                    // UNIVERSAL FIX: GameObject standard search lagaya jo har version me safe hai
                    TMP_Text[] allTexts = GameObject.FindObjectsOfType<TMP_Text>();
                    foreach (TMP_Text txt in allTexts)
                    {
                        if (txt != null && txt.gameObject.name.ToLower().Contains("coin"))
                        {
                            txt.text = currentCoins.ToString();
                        }
                    }
                }
                // =========================================================================

                EquipThisSkin();
            }
            else
            {
                Debug.Log("Not enough coins!");
            }
        }
    }

    void EquipThisSkin()
    {
        PlayerPrefs.SetString(parentTool.name + "_Equipped", currentVariant.variantName);
        PlayerPrefs.Save();

        if (eraserManager != null)
        {
            eraserManager.ApplyVariantSkin(parentTool, currentVariant, true);
        }

        //  UNIVERSAL FIX: Buttons ko refresh karne ke liye standard method bina parameters ke
        ToolVariantButton[] allButtons = GameObject.FindObjectsOfType<ToolVariantButton>();
        foreach (ToolVariantButton btn in allButtons)
        {
            if (btn != null && btn.gameObject.activeInHierarchy)
            {
                btn.UpdateUI();
            }
        }
    }
}