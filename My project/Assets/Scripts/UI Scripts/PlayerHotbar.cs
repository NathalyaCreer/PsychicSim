using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHotbar : MonoBehaviour
{
    [Header("Hotbar")]
    [SerializeField, Min(2)] private int slotCount = 5;
    [SerializeField, Min(2)] private int startingUnlockedSlots = 2;
    [SerializeField] private Transform objectGrabPoint;
    [SerializeField] private Image[] slotBackgrounds;
    [SerializeField] private TMP_Text[] slotLabels;

    [Header("Selection Colors")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = new Color(0.65f, 0.65f, 0.65f);
    [SerializeField] private Color lockedColor = new Color(0.25f, 0.25f, 0.25f);

    private ObjectGrabbable[] slots;
    private int selectedSlot;
    private int unlockedSlotCount;

    private const string UnlockedSlotsKey = "PlayerHotbar_UnlockedSlots";

    public int SelectedSlot => selectedSlot;
    public int UnlockedSlotCount => unlockedSlotCount;
    public int TotalSlotCount => slots != null ? slots.Length : slotCount;

    public ObjectGrabbable HeldObject =>
        slots != null && selectedSlot >= 0 && selectedSlot < slots.Length
            ? slots[selectedSlot]
            : null;

    private void Awake()
    {
        slotCount = Mathf.Max(2, slotCount);
        startingUnlockedSlots = Mathf.Clamp(startingUnlockedSlots, 2, slotCount);

        slots = new ObjectGrabbable[slotCount];

        // New save starts with two slots; existing save loads its unlocked count.
        unlockedSlotCount = PlayerPrefs.GetInt(
            UnlockedSlotsKey,
            startingUnlockedSlots
        );

        unlockedSlotCount = Mathf.Clamp(unlockedSlotCount, startingUnlockedSlots, slotCount);
        selectedSlot = 0;

        RefreshUI();
    }

    private void Update()
    {
        // Number keys 1–5 select slots, but locked slots cannot be selected.
        for (int i = 0; i < slotCount && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SelectSlot(i);
        }
    }

    public bool IsSlotUnlocked(int index)
    {
        return index >= 0 && index < unlockedSlotCount;
    }

    public bool TryPickup(ObjectGrabbable item)
    {
        if (item == null || !IsSlotUnlocked(selectedSlot) || HeldObject != null)
            return false;

        slots[selectedSlot] = item;
        EquipSelectedItem();
        RefreshUI();

        return true;
    }

    public void SelectSlot(int index)
    {
        if (!IsSlotUnlocked(index) || index == selectedSlot)
            return;

        StowSelectedItem();

        selectedSlot = index;
        EquipSelectedItem();
        RefreshUI();
    }

    public void DropSelectedItem()
    {
        ObjectGrabbable item = HeldObject;

        if (item == null)
            return;

        slots[selectedSlot] = null;

        item.gameObject.SetActive(true);
        item.Drop();

        RefreshUI();
    }

    public void ForgetItem(ObjectGrabbable item)
    {
        if (item == null || slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == item)
                slots[i] = null;
        }

        RefreshUI();
    }

    /// <summary>
    /// Unlocks the next hotbar slot.
    /// Skill-tree slot numbers are 1-based: slot 3 means index 2.
    /// </summary>
    public bool UnlockSlot(int slotNumber)
    {
        int requestedCount = slotNumber;

        // Only unlock the next slot in sequence.
        if (requestedCount != unlockedSlotCount + 1)
        {
            Debug.LogWarning(
                $"Cannot unlock hotbar slot {slotNumber}. " +
                $"The next slot to unlock is {unlockedSlotCount + 1}.",
                this
            );
            return false;
        }

        if (requestedCount > slotCount)
        {
            Debug.LogWarning($"Hotbar only has {slotCount} configured slots.", this);
            return false;
        }

        unlockedSlotCount = requestedCount;

        PlayerPrefs.SetInt(UnlockedSlotsKey, unlockedSlotCount);
        PlayerPrefs.Save();

        RefreshUI();

        Debug.Log($"Hotbar slot {slotNumber} unlocked!");
        return true;
    }

    private void StowSelectedItem()
    {
        ObjectGrabbable item = HeldObject;

        if (item == null)
            return;

        // Keep it in its inventory slot, but hide it while unequipped.
        item.transform.SetParent(null, true);
        item.gameObject.SetActive(false);
    }

    private void EquipSelectedItem()
    {
        ObjectGrabbable item = HeldObject;

        if (item == null)
            return;

        item.gameObject.SetActive(true);

        if (objectGrabPoint != null)
            item.Grab(objectGrabPoint);
        else
            Debug.LogError("PlayerHotbar: Object Grab Point is not assigned.", this);
    }

    private void RefreshUI()
    {
        for (int i = 0; i < slotCount; i++)
        {
            bool unlocked = IsSlotUnlocked(i);

            if (slotBackgrounds != null &&
                i < slotBackgrounds.Length &&
                slotBackgrounds[i] != null)
            {
                if (!unlocked)
                    slotBackgrounds[i].color = lockedColor;
                else
                    slotBackgrounds[i].color =
                        i == selectedSlot ? selectedColor : unselectedColor;
            }

            if (slotLabels != null &&
                i < slotLabels.Length &&
                slotLabels[i] != null)
            {
                if (!unlocked)
                {
                    slotLabels[i].text = $"{i + 1}: LOCKED";
                }
                else
                {
                    string itemName = slots[i] != null
                        ? slots[i].name
                        : "Empty";

                    slotLabels[i].text = $"{i + 1}: {itemName}";
                }
            }
        }
    }
}