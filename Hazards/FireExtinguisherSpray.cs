
using UnityEngine;

public class FireExtinguisherSpray : MonoBehaviour
{
    [Header("Spray")]
    [SerializeField] private float amountExtinguishedPerSecond = 1f;
    [SerializeField] private float sprayDistance = 100f;
    [SerializeField] private LayerMask fireLayerMask = ~0;

    [Header("Particles")]
    [SerializeField] private ParticleSystem sprayParticles;

    public bool IsHeld { get; private set; }
    public bool IsSpraying { get; private set; }

    private void Awake()
    {
        // Prevent the spray effect from starting automatically.
        if (sprayParticles != null)
        {
            var main = sprayParticles.main;
            main.playOnAwake = false;

            sprayParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void SetHeld(bool held)
    {
        IsHeld = held;

        if (!IsHeld)
            SetSpraying(false);
    }

    public void SetSpraying(bool spraying)
    {
        IsSpraying = spraying && IsHeld;

        if (sprayParticles == null)
            return;

        if (IsSpraying)
        {
            if (!sprayParticles.isPlaying)
                sprayParticles.Play();
        }
        else
        {
            sprayParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void Update()
    {
        if (!IsHeld || !IsSpraying)
            return;

        if (Physics.Raycast(
                transform.position,
                transform.forward,
                out RaycastHit hit,
                sprayDistance,
                fireLayerMask))
        {
            FireHazard fireHazard =
                hit.collider.GetComponentInParent<FireHazard>();

            if (fireHazard != null && fireHazard.isLit)
            {
                fireHazard.TryExtinguish(
                    amountExtinguishedPerSecond * Time.deltaTime);
            }
        }
    }
}
