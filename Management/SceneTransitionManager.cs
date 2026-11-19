
using UnityEngine;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Environment Roots")]
    [SerializeField] private GameObject storeEnvironment;
    [SerializeField] private GameObject apartmentEnvironment;

    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Spawn Points")]
    [SerializeField] private Transform storeSpawnPoint;
    [SerializeField] private Transform apartmentSpawnPoint;

    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    public void LoadApartment(string unusedSceneName = "")
    {
        TeleportPlayer(
            apartmentEnvironment,
            apartmentSpawnPoint
        );
    }

    public void LoadStore(string unusedSceneName = "")
    {
        TeleportPlayer(
            storeEnvironment,
            storeSpawnPoint
        );
    }

    private void TeleportPlayer(
        GameObject destinationEnvironment,
        Transform spawnPoint)
    {
        if (isTransitioning)
            return;

        if (player == null)
        {
            Debug.LogError("SceneTransitionManager: Player is missing.");
            return;
        }

        if (destinationEnvironment == null || spawnPoint == null)
        {
            Debug.LogError("SceneTransitionManager: Destination or spawn point is missing.");
            return;
        }

        isTransitioning = true;

        // Activate destination and deactivate the other environment.
        storeEnvironment.SetActive(destinationEnvironment == storeEnvironment);
        apartmentEnvironment.SetActive(destinationEnvironment == apartmentEnvironment);

        // Temporarily deactivate the player.
        player.gameObject.SetActive(false);

        // Disable movement components if necessary.
        CharacterController controller =
            player.GetComponent<CharacterController>();

        UnityEngine.AI.NavMeshAgent agent =
            player.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (agent != null)
        {
            agent.enabled = false;
        }

        // Teleport while inactive.
        player.SetPositionAndRotation(
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Synchronize physics transforms.
        Physics.SyncTransforms();

        // Reactivate the player.
        player.gameObject.SetActive(true);

        // Restore movement components.
        if (controller != null)
            controller.enabled = true;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.ResetPath();
        }

        Debug.Log($"Player teleported to {player.position}");

        isTransitioning = false;
    }
}
