
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LaptopUI : MonoBehaviour
{
    [Header("Laptop Interface")]
    [SerializeField] private GameObject laptopRoot;
    [SerializeField] private GameObject screensaverPanel;

    [Header("Store Panels")]
    [SerializeField] private GameObject skillTreePanel;
    [SerializeField] private GameObject productStorePanel;
    [SerializeField] private GameObject employeeHiringPanel;

    [Header("Store Buttons")]
    [SerializeField] private Button skillTreeButton;
    [SerializeField] private Button productStoreButton;
    [SerializeField] private Button employeeHiringButton;
    [SerializeField] private Button powerButton;

    [Header("Player Information")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text reputationPointsText;

    [Header("Player Controls")]
    [Tooltip("Assign the player's movement and camera-look scripts.")]
    [SerializeField] private Behaviour[] playerControls;

    private MoneyManager subscribedMoneyManager;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (skillTreeButton != null)
            skillTreeButton.onClick.AddListener(ShowSkillTree);

        if (productStoreButton != null)
            productStoreButton.onClick.AddListener(ShowProductStore);

        if (employeeHiringButton != null)
            employeeHiringButton.onClick.AddListener(ShowEmployeeHiring);

        if (powerButton != null)
            powerButton.onClick.AddListener(CloseLaptop);

        if (laptopRoot != null)
            laptopRoot.SetActive(true);

        IsOpen = false;
        SetPlayerControls(true);
    }

    private void Start()
    {
        // Subscribe after scene objects have initialized.
        subscribedMoneyManager = MoneyManager.Instance;

        if (subscribedMoneyManager != null)
        {
            subscribedMoneyManager.OnBalanceChanged += UpdateMoney;
            UpdateMoney(subscribedMoneyManager.Balance);
        }
        else
        {
            Debug.LogWarning(
                "LaptopUI: No MoneyManager.Instance found."
            );
        }
    }

    private void OnDestroy()
    {
        if (subscribedMoneyManager != null)
            subscribedMoneyManager.OnBalanceChanged -= UpdateMoney;
    }

    public void OpenLaptop()
    {
        if (IsOpen)
            return;

        IsOpen = true;

        if (laptopRoot != null)
            laptopRoot.SetActive(true);

        ShowScreensaver();

        // Refresh the money display after opening the laptop.
        RefreshMoneyDisplay();

        SetPlayerControls(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseLaptop()
    {
        if (!IsOpen)
            return;

        IsOpen = false;

        if (laptopRoot != null)
            laptopRoot.SetActive(false);

        SetPlayerControls(true);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void ShowScreensaver()
    {
        SetPanelActive(screensaverPanel);
        RefreshMoneyDisplay();
    }

    public void ShowSkillTree()
    {
        SetPanelActive(skillTreePanel);
        RefreshMoneyDisplay();
    }

    public void ShowProductStore()
    {
        SetPanelActive(productStorePanel);
        RefreshMoneyDisplay();
    }

    public void ShowEmployeeHiring()
    {
        SetPanelActive(employeeHiringPanel);
        RefreshMoneyDisplay();
    }

    private void SetPanelActive(GameObject selectedPanel)
    {
        if (screensaverPanel != null)
            screensaverPanel.SetActive(selectedPanel == screensaverPanel);

        if (skillTreePanel != null)
            skillTreePanel.SetActive(selectedPanel == skillTreePanel);

        if (productStorePanel != null)
            productStorePanel.SetActive(selectedPanel == productStorePanel);

        if (employeeHiringPanel != null)
            employeeHiringPanel.SetActive(selectedPanel == employeeHiringPanel);
    }

    public void UpdateMoney(int amount)
    {
        if (moneyText != null)
        {
            moneyText.text = $"${amount:N0}";
        }
    }

    public void UpdateReputationPoints(int points)
    {
        if (reputationPointsText != null)
            reputationPointsText.text = points.ToString();
    }

    private void SetPlayerControls(bool enabled)
    {
        if (playerControls == null)
            return;

        foreach (Behaviour control in playerControls)
        {
            if (control != null)
                control.enabled = enabled;
        }
    }

    private void OnDisable()
    {
        if (IsOpen)
        {
            IsOpen = false;
            SetPlayerControls(true);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    private void RefreshMoneyDisplay()
    {
        if (moneyText == null)
        {
            Debug.LogWarning("LaptopUI: Money Text is not assigned.", this);
            return;
        }

        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("LaptopUI: MoneyManager.Instance is missing.", this);
            return;
        }

        moneyText.text = $"${MoneyManager.Instance.Balance:N0}";
    }
}
