using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour
{
    [Header("Object Data")]
    public CleaningObjectData objectData; // Isme ScriptableObject data file aayegi

    public void LoadLevel()
    {
        Debug.Log("Football Button Clicked");

        if (objectData != null)
        {
            LevelManager.SelectedObject = objectData;
            SceneManager.LoadScene("FootballScene");
        }
        else
        {
            Debug.LogError("Object Data Missing");
        }
    }
    void OnMouseDown()
    {
        Debug.Log("Mouse Down");
    }
}