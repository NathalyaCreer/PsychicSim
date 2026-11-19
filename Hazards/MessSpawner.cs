
using System.Collections.Generic;
using UnityEngine;

public class MessSpawner : MonoBehaviour
{
    public static MessSpawner Instance { get; private set; }

    [Header("Mess Prefabs")]
    [SerializeField] private StoreMess trashPrefab;
    [SerializeField] private StoreMess spillPrefab;

    [Header("Spawn Settings")]
    [SerializeField, Min(1)] private int maximumMessCount = 20;
    [SerializeField] private float spawnRadius = 1.5f;
    [SerializeField] private float floorRayHeight = 2f;
    [SerializeField] private float floorRayDistance = 6f;
    [SerializeField] private LayerMask floorLayer;
    [SerializeField] private LayerMask obstructionLayer;

    [Header("Customer Mess Chances")]
    [Range(0f, 1f)]
    [SerializeField] private float trashChance = 0.15f;

    [Range(0f, 1f)]
    [SerializeField] private float spillChance = 0.08f;

    private readonly List<StoreMess> activeMesses =
        new List<StoreMess>();

    public IReadOnlyList<StoreMess> ActiveMesses => activeMesses;

    public int ActiveMessCount
    {
        get
        {
            activeMesses.RemoveAll(mess => mess == null);
            return activeMesses.Count;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // --------------------------------------------------
    // CUSTOMER MESS GENERATION
    // --------------------------------------------------

    public bool TrySpawnRandomMess(Vector3 customerPosition)
    {
        if (ActiveMessCount >= maximumMessCount)
            return false;

        float roll = Random.value;

        StoreMess prefab = null;

        if (roll < trashChance)
        {
            prefab = trashPrefab;
        }
        else if (roll < trashChance + spillChance)
        {
            prefab = spillPrefab;
        }

        if (prefab == null)
            return false;

        return TrySpawnMess(prefab, customerPosition);
    }

    // --------------------------------------------------
    // SPAWNING
    // --------------------------------------------------

    private bool TrySpawnMess(
        StoreMess prefab,
        Vector3 nearPosition)
    {
        if (prefab == null)
            return false;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 randomOffset =
                Random.insideUnitCircle * spawnRadius;

            Vector3 candidate = nearPosition +
                new Vector3(randomOffset.x, floorRayHeight, randomOffset.y);

            if (!Physics.Raycast(
                candidate,
                Vector3.down,
                out RaycastHit hit,
                floorRayDistance,
                floorLayer,
                QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            Vector3 spawnPosition = hit.point;

            // Avoid spawning inside furniture, characters, or other messes.
            if (Physics.CheckSphere(
                spawnPosition + Vector3.up * 0.1f,
                0.25f,
                obstructionLayer,
                QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            StoreMess mess = Instantiate(
                prefab,
                spawnPosition,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            );

            activeMesses.Add(mess);

            Debug.Log(
                $"Customer created a {mess.Type} mess.",
                mess
            );

            return true;
        }

        return false;
    }

    public void RegisterMess(StoreMess mess)
    {
        if (mess != null && !activeMesses.Contains(mess))
            activeMesses.Add(mess);
    }

    public void UnregisterMess(StoreMess mess)
    {
        activeMesses.Remove(mess);
    }
}
