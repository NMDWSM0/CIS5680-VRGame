using UnityEngine;

/// <summary>
/// Utility script that automatically destroys the GameObject once its ParticleSystem has finished playing.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class AutoDestroyParticle : MonoBehaviour
{
    private ParticleSystem ps;

    private void Start()
    {
        ps = GetComponent<ParticleSystem>();
        
        // Safety: If for some reason the particle system is set to loop indefinitely,
        // this script won't destroy it. Make sure "Looping" is unchecked in the Inspector.
        if (ps.main.loop)
        {
            Debug.LogWarning($"AutoDestroyParticle: Particle system on {gameObject.name} is set to Loop. It will never be auto-destroyed.");
        }
    }

    private void Update()
    {
        if (ps != null && !ps.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}
