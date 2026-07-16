/*using UnityEngine;
using UnityEngine.EventSystems;

public class PauseButtonHelper : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Jab mouse pause button ke upar aayega, toh tool tracking block ho jayegi
        PauseManager.IsGamePaused = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Jab mouse baahir jayega (agar click nahi kiya), toh tracking wapas shuru ho jayegi
        // Lekin agar Pause Menu khul gaya hai, toh PauseManager khud isay true rakhega
        if (PauseManager.Instance != null && !PauseManager.Instance.pauseMenuPanel.activeSelf)
        {
            PauseManager.IsGamePaused = false;
        }
    }
}*/