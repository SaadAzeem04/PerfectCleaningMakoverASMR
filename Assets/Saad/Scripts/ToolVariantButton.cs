using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToolVariantButton : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text priceText;
    public GameObject coinIconObject; // Coin image/icon reference
    public Button selectButton;
    public GameObject equippedCheckmark;

    [Header("Background Settings")]
    public GameObject defaultBGObject;
    public GameObject selectedBGObject;

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

        if (defaultBGObject != null)
        {
            if (defaultBGObject == this.gameObject)
            {
                Image parentImage = defaultBGObject.GetComponent<Image>();
                if (parentImage != null)
                {
                    parentImage.enabled = !isEquipped;
                }
            }
            else
            {
                defaultBGObject.SetActive(!isEquipped);
            }
        }

        if (selectedBGObject != null)
        {
            selectedBGObject.SetActive(isEquipped);
        }

        if (equippedCheckmark != null)
        {
            equippedCheckmark.SetActive(isEquipped);
        }

        // ==================== PRICE & COIN ICON LOGIC ====================
        if (isEquipped)
        {
            priceText.text = "Equipped";
            selectButton.interactable = false;

            // Equipped par Coin Image GAYAB ho jayegi
            if (coinIconObject != null) coinIconObject.SetActive(false);
        }
        else if (isUnlocked)
        {
            priceText.text = "Free";
            selectButton.interactable = true;

            // Free par Coin Image GAYAB ho jayegi
            if (coinIconObject != null) coinIconObject.SetActive(false);
        }
        else
        {
            priceText.text = currentVariant.coinPrice.ToString();
            selectButton.interactable = true;

            // Coins hone par Coin Image NAZAR aayegi
            if (coinIconObject != null) coinIconObject.SetActive(true);
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

                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayBuySFX();
                    }

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

                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayBuySFX();
                    }

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