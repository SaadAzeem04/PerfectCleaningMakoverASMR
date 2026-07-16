using UnityEngine;

public class ToolManager : MonoBehaviour
{
    public MaskEraser maskEraser;

    public ToolData currentTool;

    public void SelectTool(ToolData tool)
    {
        Debug.Log("Tool Selected : " + tool.toolName);

        if (tool == null)
            return;

        currentTool = tool;

        maskEraser.SelectTool(tool);
    }
}