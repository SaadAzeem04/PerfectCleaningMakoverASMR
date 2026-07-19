using UnityEngine;

public class MudChunk : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Jaise hi scraper ka edge is chunk se takrayega
        if (collision.CompareTag("ScraperEdge"))
        {
            // MaskEraser script ko dhoond kar bache hue chunks ki counting kam karayein
            MaskEraser eraser = FindObjectOfType<MaskEraser>();
            if (eraser != null)
            {
                eraser.ScraperChunkDestroyed();
            }

            // Chunk ko foran screen se gayab (inactive) kar dein
            gameObject.SetActive(false);
        }
    }
}