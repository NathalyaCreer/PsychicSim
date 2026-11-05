using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTreeNodeButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;

    private SkillTreeManager controller;
    private SkillTreeNode node;

    public void Setup(
        SkillTreeNode skillNode,
        SkillTreeManager skillTreeManager)
    {
        node = skillNode;
        controller = skillTreeManager;

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(TryUnlock);
        }

        if (titleText != null)
            titleText.text = node.DisplayName;

        Refresh();
    }

    public void Refresh()
    {
        if (node == null || controller == null)
            return;

        bool unlocked = controller.IsUnlocked(node);
        bool prerequisitesMet = controller.ArePrerequisitesMet(node);
        bool canAfford = controller.CanAfford(node);

        if (statusText != null)
        {
            if (unlocked)
                statusText.text = "UNLOCKED";
            else if (!prerequisitesMet)
                statusText.text = $"Requires previous upgrade • {node.ReputationPointCost} RP";
            else
                statusText.text = $"{node.ReputationPointCost} RP";
        }

        if (button != null)
            button.interactable = !unlocked && prerequisitesMet && canAfford;
    }

    private void TryUnlock()
    {
        if (controller != null && node != null)
            controller.TryUnlock(node);
    }
}