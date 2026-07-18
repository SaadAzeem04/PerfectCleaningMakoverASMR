using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToolVariantButton : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text priceText;
    public Button selectButton;
    public GameObject equippedCheckmark; // Video wala green tick (Optional)

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
                    // Backup Updater: Agar Instance na mile to direct UI text badal do
                    TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>();
                    foreach (TMP_Text txt in allTexts)
                    {
                        if (txt.gameObject.name.ToLower().Contains("coin"))
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
    }
}