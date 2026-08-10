using UnityEngine;

public class DynamicToolSorting : MonoBehaviour
{
    [Header("Default Settings")]
    [Tooltip("Tool ka normal sorting order jab wo kisi object ke peeche na ho")]
    public int defaultFrontOrder = 25;

    private SpriteRenderer[] toolRenderers;

    private void Awake()
    {
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        toolRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    // Is method se tool ka order (Target Prefab Order - 1) ho jaye ga
    public void SetOrderBehindTarget(GameObject targetObject)
    {
        if (targetObject == null) return;

        SpriteRenderer[] targetRenderers = targetObject.GetComponentsInChildren<SpriteRenderer>(true);
        if (targetRenderers == null || targetRenderers.Length == 0) return;

        int highestTargetOrder = int.MinValue;
        foreach (SpriteRenderer sr in targetRenderers)
        {
            if (sr != null && sr.sortingOrder > highestTargetOrder)
            {
                highestTargetOrder = sr.sortingOrder;
            }
        }

        if (highestTargetOrder != int.MinValue)
        {
            ApplyOrderToTool(highestTargetOrder - 1);
        }
    }

    // Cleaning khatam hone par tool ko dobara normal layer par laane ke liye
    public void ResetToDefaultOrder()
    {
        ApplyOrderToTool(defaultFrontOrder);
    }

    private void ApplyOrderToTool(int order)
    {
        if (toolRenderers == null || toolRenderers.Length == 0)
        {
            CacheRenderers();
        }

        foreach (SpriteRenderer sr in toolRenderers)
        {
            if (sr != null)
            {
                sr.sortingOrder = order;
            }
        }
    }
}