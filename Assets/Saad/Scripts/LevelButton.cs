using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    [Header("Assign New Level Data Asset")]
    public LevelData levelData; // Ab direct CleaningObjectData ke bajaye LevelData aayega

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(LoadLevel);
        }
    }

    public void LoadLevel()
    {
        if (levelData != null && LevelManager.Instance != null)
        {
            // LevelManager through FootballScene switch hoga
            LevelManager.Instance.LoadLevel(levelData);
        }
        else
        {
            Debug.LogWarning("LevelData ya LevelManager missing hai!");
        }
    }
}