using UnityEngine;

public class MudChunk : MonoBehaviour
{
    private bool isScraped = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!gameObject.activeInHierarchy || isScraped) return;

        if (collision.CompareTag("ScraperEdge"))
        {
            isScraped = true;

            MaskEraser eraser = FindObjectOfType<MaskEraser>();
            if (eraser != null)
            {
                eraser.ScraperChunkDestroyed();
            }

            gameObject.SetActive(false);
        }
    }
}