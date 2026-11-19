using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine;

public class SkillTreeManager : MonoBehaviour
{
    [Header("Skill Tree")]
    [SerializeField] private List<SkillTreeNode> nodes = new List<SkillTreeNode>();
    [SerializeField] private Transform nodeContainer;
    [SerializeField] private SkillTreeNodeButton nodeButtonPrefab;

    [Header("Tree Layout")]
    [SerializeField] private Vector2 nodeSpacing = new Vector2(320f, 220f);
    [SerializeField] private Vector2 treePadding = new Vector2(150f, 120f);
    [SerializeField] private Image connectorPrefab;
    [SerializeField] private Color connectorColor = new Color(0.45f, 0.45f, 0.5f, 0.8f);
    [SerializeField] private float connectorThickness = 5f;

    [Header("Gameplay Upgrade Targets")]
    [SerializeField] private PlayerHotbar playerHotbar;

    private readonly List<SkillTreeNodeButton> spawnedButtons =
        new List<SkillTreeNodeButton>();

    private const string UnlockedKeyPrefix = "SkillTree_Unlocked_";

    public event System.Action<SkillTreeNode> OnSkillUnlocked;


    private void OnEnable()
    {
        PlayerHotbar.OnHotbarRegistered += RegisterPlayerHotbar;

        // The player may already exist when this manager becomes active.
        if (PlayerHotbar.Instance != null)
        {
            RegisterPlayerHotbar(PlayerHotbar.Instance);
        }

        BuildTree();

        if (ReputationSystem.Instance != null)
            ReputationSystem.Instance.OnReputationPointsChanged += HandlePointsChanged;
    }


    private void OnDisable()
    {
        PlayerHotbar.OnHotbarRegistered -= RegisterPlayerHotbar;
        if (ReputationSystem.Instance != null)
            ReputationSystem.Instance.OnReputationPointsChanged -= HandlePointsChanged;
    }

    public void BuildTree()
    {
        if (nodeContainer == null || nodeButtonPrefab == null)
        {
            Debug.LogError(
                "SkillTreeManager: Assign Node Container and Node Button Prefab.",
                this
            );
            return;
        }

        RectTransform contentRect = nodeContainer as RectTransform;

        if (contentRect == null)
        {
            Debug.LogError("Node Container must have a RectTransform.", this);
            return;
        }

        // Remove previously generated nodes and connectors.
        for (int i = nodeContainer.childCount - 1; i >= 0; i--)
            Destroy(nodeContainer.GetChild(i).gameObject);

        spawnedButtons.Clear();

        Dictionary<SkillTreeNode, Vector2> nodePositions =
            new Dictionary<SkillTreeNode, Vector2>();

        int maxColumn = 0;
        int maxRow = 0;

        foreach (SkillTreeNode node in nodes)
        {
            if (node == null)
                continue;

            maxColumn = Mathf.Max(maxColumn, node.Column);
            maxRow = Mathf.Max(maxRow, node.Row);

            Vector2 position = new Vector2(
                treePadding.x + node.Column * nodeSpacing.x,
                -(treePadding.y + node.Row * nodeSpacing.y)
            );

            nodePositions[node] = position;
        }

        // Set the Scroll View content size.
        float contentWidth =
            treePadding.x * 2f + maxColumn * nodeSpacing.x + nodeSpacing.x;

        float contentHeight =
            treePadding.y * 2f + maxRow * nodeSpacing.y + nodeSpacing.y;

        contentRect.sizeDelta = new Vector2(
            contentWidth,
            contentHeight
        );

        // Draw prerequisite connectors first so they appear behind nodes.
        if (connectorPrefab != null)
        {
            foreach (SkillTreeNode node in nodes)
            {
                if (node == null || node.Prerequisites == null)
                    continue;

                foreach (SkillTreeNode prerequisite in node.Prerequisites)
                {
                    if (prerequisite == null ||
                        !nodePositions.ContainsKey(prerequisite))
                        continue;

                    CreateConnector(
                        nodePositions[prerequisite],
                        nodePositions[node]
                    );
                }
            }
        }

        // Spawn skill buttons at their assigned positions.
        foreach (SkillTreeNode node in nodes)
        {
            if (node == null)
                continue;

            SkillTreeNodeButton nodeButton =
                Instantiate(nodeButtonPrefab, nodeContainer);

            RectTransform buttonRect =
                nodeButton.GetComponent<RectTransform>();

            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = nodePositions[node];

            nodeButton.Setup(node, this);
            spawnedButtons.Add(nodeButton);
        }
    }

    public bool IsUnlocked(SkillTreeNode node)
    {
        if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
            return false;

        return PlayerPrefs.GetInt(UnlockedKeyPrefix + node.NodeId, 0) == 1;
    }

    public bool ArePrerequisitesMet(SkillTreeNode node)
    {
        if (node == null)
            return false;

        foreach (SkillTreeNode prerequisite in node.Prerequisites)
        {
            if (prerequisite == null || !IsUnlocked(prerequisite))
                return false;
        }

        return true;
    }

    public bool CanAfford(SkillTreeNode node)
    {
        return node != null &&
               ReputationSystem.Instance != null &&
               ReputationSystem.Instance.ReputationPoints >= node.ReputationPointCost;
    }

    public bool TryUnlock(SkillTreeNode node)
    {
        if (node == null || IsUnlocked(node))
            return false;

        if (!ArePrerequisitesMet(node))
        {
            Debug.Log($"Cannot unlock {node.DisplayName}: prerequisites aren't met.");
            return false;
        }

        // Validate the hotbar upgrade before taking reputation.
        if (node.UpgradeType == SkillUpgradeType.UnlockHotbarSlot)
        {
            if (playerHotbar == null)
            {
                Debug.LogError("SkillTreeManager: PlayerHotbar is not assigned.", this);
                return false;
            }

            if (node.HotbarSlotNumber != playerHotbar.UnlockedSlotCount + 1)
            {
                Debug.LogWarning(
                    $"This node targets hotbar slot {node.HotbarSlotNumber}, " +
                    $"but the next slot is {playerHotbar.UnlockedSlotCount + 1}.",
                    this
                );
                return false;
            }
        }

        ReputationSystem reputation = ReputationSystem.Instance;

        if (reputation == null ||
            !reputation.TrySpendReputationPoints(node.ReputationPointCost))
        {
            Debug.Log($"Not enough reputation points to unlock {node.DisplayName}.");
            RefreshButtons();
            return false;
        }

        // Save this node as purchased.
        PlayerPrefs.SetInt(UnlockedKeyPrefix + node.NodeId, 1);
        PlayerPrefs.Save();

        // Apply the gameplay effect immediately.
        ApplyUpgrade(node);
        OnSkillUnlocked?.Invoke(node);

        Debug.Log($"Skill unlocked: {node.DisplayName}");

        RefreshButtons();
        return true;
    }
    

    private void ApplyAllUnlockedUpgrades()
    {
        if (playerHotbar == null)
            return;

        // Restore hotbar upgrades in slot order.
        for (int slotNumber = 3; slotNumber <= 5; slotNumber++)
        {
            foreach (SkillTreeNode node in nodes)
            {
                if (node == null ||
                    !IsUnlocked(node) ||
                    node.UpgradeType != SkillUpgradeType.UnlockHotbarSlot ||
                    node.HotbarSlotNumber != slotNumber)
                {
                    continue;
                }

                ApplyUpgrade(node);
            }
        }

        // Apply other unlocked upgrade types here as needed.
    }


    private void ApplyUpgrade(SkillTreeNode node)
    {
        if (node == null)
            return;

        switch (node.UpgradeType)
        {
            case SkillUpgradeType.UnlockHotbarSlot:
                if (playerHotbar == null)
                {
                    Debug.LogError("SkillTreeManager: PlayerHotbar is not assigned.", this);
                    return;
                }

                // If the slot was already restored from the hotbar save,
                // this can safely be skipped.
                if (playerHotbar.UnlockedSlotCount < node.HotbarSlotNumber)
                    playerHotbar.UnlockSlot(node.HotbarSlotNumber);

                break;

            case SkillUpgradeType.None:

            case SkillUpgradeType.UnlockEmployeeHiring:
                Debug.Log("Employee hiring has been unlocked!");
                break;
            default:
                break;
        }
    }

    private void HandlePointsChanged(int newPoints)
    {
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        foreach (SkillTreeNodeButton nodeButton in spawnedButtons)
        {
            if (nodeButton != null)
                nodeButton.Refresh();
        }
    }

    private void CreateConnector(Vector2 start, Vector2 end)
    {
        Image connector = Instantiate(connectorPrefab, nodeContainer);

        RectTransform rect = connector.rectTransform;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 difference = end - start;

        rect.anchoredPosition = (start + end) * 0.5f;
        rect.sizeDelta = new Vector2(
            difference.magnitude,
            connectorThickness
        );

        float angle = Mathf.Atan2(
            difference.y,
            difference.x
        ) * Mathf.Rad2Deg;

        rect.localRotation = Quaternion.Euler(0f, 0f, angle);

        connector.color = connectorColor;
    }


    private void RegisterPlayerHotbar(PlayerHotbar hotbar)
    {
        if (hotbar == null)
            return;

        playerHotbar = hotbar;

        Debug.Log("SkillTreeManager registered PlayerHotbar.", this);

        // Restore purchased hotbar upgrades on the newly spawned player.
        ApplyAllUnlockedUpgrades();
    }

}