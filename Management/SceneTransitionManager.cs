
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private string storeSpawnPoint = "StoreSpawnPoint";
    [SerializeField] private string apartmentSpawnPoint = "ApartmentSpawnPoint";

    private string destinationSpawnPoint;
    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadApartment(string sceneName)
    {
        LoadScene(sceneName, apartmentSpawnPoint);
    }

    public void LoadStore(string sceneName)
    {
        LoadScene(sceneName, storeSpawnPoint);
    }

    private void LoadScene(string sceneName, string spawnPoint)
    {
        if (isTransitioning)
            return;

        if (playerPrefab == null)
        {
            Debug.LogError("SceneTransitionManager: Player prefab is missing.");
            return;
        }

        destinationSpawnPoint = spawnPoint;
        isTransitioning = true;

        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");

        if (currentPlayer != null)
            Destroy(currentPlayer);

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError($"Could not load scene: {sceneName}");
            isTransitioning = false;
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        Transform spawnPoint = FindSpawnPoint(destinationSpawnPoint);

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"Could not find spawn point: {destinationSpawnPoint}"
            );

            isTransitioning = false;
            yield break;
        }

        Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        isTransitioning = false;
    }

    private Transform FindSpawnPoint(string pointName)
    {
        GameObject point = GameObject.Find(pointName);

        return point != null ? point.transform : null;
    }
}
