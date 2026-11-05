
using System.Collections;
using UnityEngine;

public class FireHazard : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float currentIntensity = 1.0f;
    private float [] startIntensities = new float[0];

    [SerializeField] private ParticleSystem [] fireParticleSystems = new ParticleSystem[0];
    
    float timeLastWasExtinguished = 0f;
    [SerializeField] private float regenDelay = 2.5f;
    [SerializeField] private float regenRate = 0.1f;

    public bool isLit = true;



    private void Start()
    {
        startIntensities = new float[fireParticleSystems.Length];

        for (int i = 0; i < fireParticleSystems.Length; i++)
        {
            startIntensities[i] = fireParticleSystems[i].emission.rateOverTime.constant;
        }
    }



    private void Update()
    {
        if (isLit && currentIntensity < 1f && Time.time - timeLastWasExtinguished >= regenDelay)
        {
            currentIntensity += Time.deltaTime * regenRate;

            ChangeIntensity();
        }
    }


    public bool TryExtinguish(float amount)
    {
        if (!isLit || amount <= 0f)
            return false;

        timeLastWasExtinguished = Time.time;

        currentIntensity = Mathf.Max(0f, currentIntensity - amount);

        ChangeIntensity();

        if (currentIntensity <= 0f)
        {
            isLit = false;
            return true;
        }

        return false;
    }


    private void ChangeIntensity()
    {
       for (int i = 0; i < fireParticleSystems.Length; i++)
        {
            var emission = fireParticleSystems[i].emission;
            emission.rateOverTime = currentIntensity * startIntensities[i];
        }


    }
}
