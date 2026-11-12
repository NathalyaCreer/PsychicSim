using UnityEngine;

public enum SkillUpgradeType
{
    None,
    UnlockHotbarSlot,
    UnlockEmployeeHiring
}

[CreateAssetMenu(
    fileName = "SkillTreeNode",
    menuName = "Store Game/Skill Tree Node"
)]
public class SkillTreeNode : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string nodeId;
    [SerializeField] private string displayName;

    [TextArea]
    [SerializeField] private string description;

    [Header("Unlock Requirements")]
    [Min(0)]
    [SerializeField] private int reputationPointCost = 1;

    [SerializeField] private SkillTreeNode[] prerequisites;

    [Header("Gameplay Upgrade")]
    [SerializeField] private SkillUpgradeType upgradeType = SkillUpgradeType.None;

    [Tooltip("For hotbar upgrades, enter 3, 4, or 5 (the visible slot number).")]
    [SerializeField, Min(1)] private int hotbarSlotNumber = 3;

    public string NodeId => nodeId;
    public string DisplayName => displayName;
    public string Description => description;
    public int ReputationPointCost => reputationPointCost;
    public SkillTreeNode[] Prerequisites => prerequisites;

    public SkillUpgradeType UpgradeType => upgradeType;
    public int HotbarSlotNumber => hotbarSlotNumber;
}