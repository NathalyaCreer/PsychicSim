using System.Collections.Generic;
using UnityEngine;

public class SkillTreeManager : MonoBehaviour
{
    [Header("Skill Tree")]
    [SerializeField] private List<SkillTreeNode> nodes = new List<SkillTreeNode>();
    [SerializeField] private Transform nodeContainer;
    [SerializeField] private SkillTreeNodeButton nodeButtonPrefab;

    [Header("Gameplay Upgrade Targets")]
    [SerializeField] private PlayerHotbar playerHotbar;

    private readonly List<SkillTreeNodeButton> spawnedButtons =
        new List<SkillTreeNodeButton>();

    private const string UnlockedKeyPrefix = "SkillTree_Unlocked_";

    public event System.Action<SkillTreeNode> OnSkillUnlocked;

    private void OnEnable()
    {
        // Reapply purchased upgrades from saved skill-node state.
        ApplyAllUnlockedUpgrades();

        BuildTree();

        if (ReputationSystem.Instance != null)
            ReputationSystem.Instance.OnReputationPointsChanged += HandlePointsChanged;
    }

    private void OnDisable()
    {
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

        for (int i = nodeContainer.childCount - 1; i >= 0; i--)
            Destroy(nodeContainer.GetChild(i).gameObject);

        spawnedButtons.Clear();

        foreach (SkillTreeNode node in nodes)
        {
            if (node == null)
                continue;

            SkillTreeNodeButton nodeButton =
                Instantiate(nodeButtonPrefab, nodeContainer);

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

        // Apply any other unlocked upgrade types here as you add them.
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
}