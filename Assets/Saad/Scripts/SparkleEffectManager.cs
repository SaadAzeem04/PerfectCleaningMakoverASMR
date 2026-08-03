/*using UnityEngine;

public class SparkleEffectManager : MonoBehaviour
{
    public static SparkleEffectManager Instance;

    [Header("Sparkle Particle Reference")]
    public ParticleSystem sparkleParticleSystem;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Level Finish Hone Par Burst Play Karein (Full Dynamic Setup)
    /// </summary>
    public void PlaySparklesAtPosition(Vector3 position)
    {
        if (sparkleParticleSystem == null)
        {
            Debug.LogError(" Sparkle Particle System assign nahi hai Inspector mein!");
            return;
        }

        // 1. Position Purse ke Center par move karein
        sparkleParticleSystem.transform.position = position;

        //  2. MAIN MODULE: Max Particles aur Lifetime Force Karein
        var main = sparkleParticleSystem.main;
        main.maxParticles = 1000; // 1 particle issue fix
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f); // Outward Movement
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);

        // 3. SHAPE MODULE: Purse ke Area jitna Box banaayein (Center Issue Fix)
        var shape = sparkleParticleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3.5f, 2.5f, 1.0f); // Purse ke charo taraf stars phelenge

        //  4. EMISSION MODULE: 30 Stars ka Instant Burst Force Karein
        var emission = sparkleParticleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0; // Continuous stream off

        // Dynamic Burst Set (30 Stars ek saath)
        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[1];
        bursts[0] = new ParticleSystem.Burst(0.0f, 30);
        emission.SetBursts(bursts);

        // 5. Old Stars clear karke Fresh Play karein
        sparkleParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        sparkleParticleSystem.Play();

        Debug.Log(" Sparkle Effect Triggered at: " + position);
    }
}*/