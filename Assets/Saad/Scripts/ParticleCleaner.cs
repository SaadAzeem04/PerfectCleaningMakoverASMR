/*using UnityEngine;
using System.Collections.Generic; // List<> ke liye yeh line bohot zaroori hai

public class ParticleCleaner : MonoBehaviour
{
    private MaskEraser eraser;

    ParticleSystem ps;

    List<ParticleCollisionEvent> collisions =
    new List<ParticleCollisionEvent>();

    float timer = 0;

    void Start()
    {
        eraser =
        FindFirstObjectByType<MaskEraser>();

        ps =
        GetComponent<ParticleSystem>();
    }

    void OnParticleCollision(GameObject other)
    {
        timer += Time.deltaTime;

        if (timer < 0.1f)
            return;

        timer = 0;

        int count =
        ps.GetCollisionEvents(
        other,
        collisions);

        if (count > 0)
        {
            Vector3 hit =
            collisions[0].intersection;

            eraser.EraseAtWorldPosition(hit);
        }
    }
}*/