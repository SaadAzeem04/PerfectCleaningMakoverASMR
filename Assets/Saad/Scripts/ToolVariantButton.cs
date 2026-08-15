using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToolVariantButton : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text priceText;
    public Button selectButton;
    public GameObject equippedCheckmark;

    [Header("Background Settings")]
    public GameObject defaultBGObject;   // Root Parent ya Dedicated Child Object
    public GameObject selectedBGObject;  // Selected Overlay Child GameObject (Select_0)

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

    private void OnEnable()
    {
        if (currentVariant != null && parentTool != null)
        {
            UpdateUI();
        }
    }

    public void UpdateUI()
    {
        if (currentVariant == null || parentTool == null) return;

        bool isUnlocked = (currentVariant.coinPrice == 0) || (PlayerPrefs.GetInt(parentTool.name + "_" + currentVariant.variantName, 0) == 1);
        string equippedName = PlayerPrefs.GetString(parentTool.name + "_Equipped", "");

        bool isEquipped = (equippedName == currentVariant.variantName) ||
                          (string.IsNullOrEmpty(equippedName) && parentTool.toolVariants.IndexOf(currentVariant) == 0);

        // SAFE DEFAULT BG HIDE LOGIC
        if (defaultBGObject != null)
        {
            // Agar defaultBGObject Parent khud hai, to SetActive(false) nahi karenge (warna poora button gayab ho jayega!)
            if (defaultBGObject == this.gameObject)
            {
                Image parentImage = defaultBGObject.GetComponent<Image>();
                if (parentImage != null)
                {
                    parentImage.enabled = !isEquipped; // Sirf image skin render disable hogi, button active rahega
                }
            }
            else
            {
                defaultBGObject.SetActive(!isEquipped);
            }
        }

        //  SELECTED OVERLAY SHOW LOGIC
        if (selectedBGObject != null)
        {
            selectedBGObject.SetActive(isEquipped);
        }

        if (equippedCheckmark != null)
        {
            equippedCheckmark.SetActive(isEquipped);
        }

        if (isEquipped)
        {
            priceText.text = "Equipped";
            selectButton.interactable = false;
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
            if (CoinManager.Instance != null)
            {
                if (CoinManager.Instance.HasEnoughCoins(currentVariant.coinPrice))
                {
                    CoinManager.Instance.DeductCoins(currentVariant.coinPrice);

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

        if (transform.parent != null)
        {
            ToolVariantButton[] siblingButtons = transform.parent.GetComponentsInChildren<ToolVariantButton>();
            foreach (var btn in siblingButtons)
            {
                btn.UpdateUI();
            }
        }
    }
}