using System.Collections.Generic;
using UnityEngine;

public class FurnitureFireTargetManager : MonoBehaviour
{
    public static FurnitureFireTargetManager Instance { get; private set; }

    private FurnitureFireTarget[] targets;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Discover all furniture targets in the loaded scene.
        targets = FindObjectsOfType<FurnitureFireTarget>();
    }

    public FurnitureFireTarget GetRandomAvailableTarget()
    {
        List<FurnitureFireTarget> availableTargets =
            new List<FurnitureFireTarget>();

        foreach (FurnitureFireTarget target in targets)
        {
            if (target != null && !target.HasBeenBurned)
                availableTargets.Add(target);
        }

        if (availableTargets.Count == 0)
            return null;

        int index = Random.Range(0, availableTargets.Count);
        return availableTargets[index];
    }

    public void RefreshTargets()
    {
        targets = FindObjectsOfType<FurnitureFireTarget>();
    }
}
