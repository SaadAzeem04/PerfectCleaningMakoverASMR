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
            // FIX: CoinManager ke DeductCoins system se integrate kiya hai
            if (CoinManager.Instance != null)
            {
                if (CoinManager.Instance.HasEnoughCoins(currentVariant.coinPrice))
                {
                    // Direct Quiet Deduct (Bina kisi Flying Swoop Animation ke)
                    CoinManager.Instance.DeductCoins(currentVariant.coinPrice);

                    // Unlock Save
                    PlayerPrefs.SetInt(parentTool.name + "_" + currentVariant.variantName, 1);
                    PlayerPrefs.Save();

                    EquipThisSkin();
                }
                else
                {
                    Debug.Log("Not enough coins!");
                }
            }
            else
            {
                // Fallback: Agar CoinManager missing ho tab bhi coins cut ho jayein
                int currentCoins = PlayerPrefs.GetInt("TotalCoins", 100);
                if (currentCoins >= currentVariant.coinPrice)
                {
                    currentCoins -= currentVariant.coinPrice;
                    PlayerPrefs.SetInt("TotalCoins", currentCoins);
                    PlayerPrefs.SetInt(parentTool.name + "_" + currentVariant.variantName, 1);
                    PlayerPrefs.Save();

                    EquipThisSkin();
                }
                else
                {
                    Debug.Log("Not enough coins!");
                }
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