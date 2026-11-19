
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CheckoutUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject checkoutPanel;
    [SerializeField] private MoneyManager moneyManager;

    [Header("Labels")]
    [SerializeField] private TMP_Text itemsText;
    [SerializeField] private TMP_Text readingText;
    [SerializeField] private TMP_Text totalText;
    [SerializeField] private BagSpawn bagSpawn;

    [Header("Buttons")]
    [SerializeField] private Button cardButton;
    [SerializeField] private Button cashButton;
    [SerializeField] private Button loveButton;
    [SerializeField] private Button wealthButton;
    [SerializeField] private Button healthButton;
    [SerializeField] private Button printTicketButton;
    [SerializeField] private Button checkoutButton;

    private Customer currentCustomer;

    private bool? selectedCardPayment;
    private Customer.ReadingType? selectedReading;
    private bool ticketPrinted;

    // Session earnings; not yet saved between game sessions.
    private int moneyEarned;

    public int MoneyEarned => moneyEarned;

    private void Awake()
    {
        if (checkoutPanel == null)
        {
            Debug.LogError("CheckoutUI: Checkout Panel is missing!", this);
            return;
        }

        checkoutPanel.SetActive(false);

        if (cardButton != null)
            cardButton.onClick.AddListener(SelectCard);

        if (cashButton != null)
            cashButton.onClick.AddListener(SelectCash);

        if (loveButton != null)
            loveButton.onClick.AddListener(
                () => SelectReading(Customer.ReadingType.Love));

        if (wealthButton != null)
            wealthButton.onClick.AddListener(
                () => SelectReading(Customer.ReadingType.Wealth));

        if (healthButton != null)
            healthButton.onClick.AddListener(
                () => SelectReading(Customer.ReadingType.Health));

        if (printTicketButton != null)
            printTicketButton.onClick.AddListener(PrintTicket);

        if (checkoutButton != null)
            checkoutButton.onClick.AddListener(Checkout);

        Debug.Log("CheckoutUI: Awake completed successfully.", this);
    }

    // --------------------------------------------------
    // PLAYER CHECKOUT
    // --------------------------------------------------

    public void BeginCheckout(Customer customer)
    {
        Debug.Log("CheckoutUI: BeginCheckout() was called.", this);

        if (customer == null || customer.IsDead)
        {
            Debug.LogWarning("CheckoutUI: Invalid customer.", this);
            return;
        }

        if (checkoutPanel == null)
            return;

        if (currentCustomer != null)
        {
            Debug.LogWarning(
                $"CheckoutUI: Replacing existing customer {currentCustomer.name}.",
                this
            );
        }

        currentCustomer = customer;

        if (bagSpawn != null)
            bagSpawn.SetCheckoutCustomer(customer);
        else
            Debug.LogWarning("CheckoutUI: BagSpawn is not assigned.", this);

        selectedCardPayment = null;
        selectedReading = null;
        ticketPrinted = false;

        if (itemsText != null)
        {
            itemsText.text = customer.PurchasedItemNames.Count > 0
                ? string.Join("\n", customer.PurchasedItemNames)
                : "No products";
        }

        if (readingText != null)
        {
            readingText.text = customer.WantsReading
                ? $"Customer Requests: {customer.PurchaseReading} reading (${customer.ReadingFee})"
                : "No reading requested.";
        }

        if (totalText != null)
            totalText.text = $"Total: ${customer.ShoppingTotal}";

        bool readingAvailable =
            customer.WantsReading && customer.CanAffordReading;

        if (loveButton != null)
            loveButton.interactable = readingAvailable;

        if (wealthButton != null)
            wealthButton.interactable = readingAvailable;

        if (healthButton != null)
            healthButton.interactable = readingAvailable;

        if (printTicketButton != null)
            printTicketButton.interactable = readingAvailable;

        UpdateTotal();

        checkoutPanel.SetActive(true);

        Debug.Log(
            $"CheckoutUI: Checkout activated for {customer.name}.",
            this
        );
    }

    public void SelectCard()
    {
        if (currentCustomer == null)
            return;

        selectedCardPayment = true;
        UpdateTotal();
    }

    public void SelectCash()
    {
        if (currentCustomer == null)
            return;

        selectedCardPayment = false;
        UpdateTotal();
    }

    public void SelectReading(Customer.ReadingType readingType)
    {
        if (currentCustomer == null)
            return;

        if (!currentCustomer.WantsReading)
        {
            if (readingText != null)
                readingText.text = "This customer doesn't want a reading.";

            return;
        }

        if (!currentCustomer.CanAffordReading)
        {
            if (readingText != null)
                readingText.text = "The customer can't afford the reading fee.";

            return;
        }

        selectedReading = readingType;
        ticketPrinted = false;

        if (readingText != null)
        {
            readingText.text =
                $"Selected reading: {readingType} (${currentCustomer.ReadingFee})";
        }

        UpdateTotal();
    }

    public void PrintTicket()
    {
        if (currentCustomer == null || !selectedReading.HasValue)
        {
            if (readingText != null)
                readingText.text = "Select Love, Wealth, or Health first.";

            return;
        }

        if (currentCustomer.GiveReadingTicket(selectedReading.Value))
        {
            ticketPrinted = true;

            if (readingText != null)
            {
                readingText.text =
                    $"{selectedReading.Value} ticket given to customer.";
            }
        }
    }

    public void Checkout()
    {
        if (currentCustomer == null || currentCustomer.IsDead)
        {
            if (currentCustomer != null)
                CancelCheckout(currentCustomer);

            return;
        }

        if (!selectedCardPayment.HasValue)
        {
            if (readingText != null)
                readingText.text = "Select Cash or Card first.";

            return;
        }

        bool includeReading = selectedReading.HasValue;

        if (includeReading && !ticketPrinted)
        {
            if (readingText != null)
                readingText.text = "Print the reading ticket before checkout.";

            return;
        }

        Customer customerBeingCheckedOut = currentCustomer;

        List<ProductData> productsBeingSold =
            new List<ProductData>(customerBeingCheckedOut.PurchasedProducts);

        bool paymentSucceeded = customerBeingCheckedOut.TryCompleteCheckout(
            selectedCardPayment.Value,
            includeReading,
            selectedReading.GetValueOrDefault(),
            out int total,
            out string message
        );

        Debug.Log(
            $"Checkout result: success={paymentSucceeded}, total=${total}, message={message}",
            this
        );

        if (!paymentSucceeded)
        {
            if (readingText != null)
                readingText.text = message;

            return;
        }

        CompleteSale(customerBeingCheckedOut, productsBeingSold, total);

        if (totalText != null)
            totalText.text = $"Sale complete: ${total}";

        if (readingText != null)
        {
            readingText.text = customerBeingCheckedOut.Unsatisfied
                ? "Wrong reading ticket! Customer is unhappy."
                : message;
        }

        checkoutPanel.SetActive(false);
        currentCustomer = null;
    }

    // --------------------------------------------------
    // AUTOMATED CASHIER CHECKOUT
    // --------------------------------------------------

    public bool ProcessCashierCheckout(Customer customer)
    {
        if (customer == null || customer.IsDead)
            return false;

        if (customer.HasPaid)
            return true;

        if (bagSpawn == null)
        {
            Debug.LogError(
                "Cashier checkout failed: BagSpawn is not assigned.",
                this
            );

            return false;
        }

        Debug.Log(
            $"Cashier is processing checkout for {customer.name}.",
            this
        );

        // Assign the customer and spawn their checkout bag.
        if (!bagSpawn.SpawnBagForCashier(customer))
        {
            Debug.LogWarning(
                $"Cashier could not spawn a bag for {customer.name}.",
                this
            );

            return false;
        }

        // Automatically put every purchased product into the bag.
        if (!customer.BagAllCheckoutItems())
        {
            Debug.LogWarning(
                $"Cashier could not bag all products for {customer.name}.",
                this
            );

            return false;
        }

        // Cashiers automatically issue the customer's requested
        // reading ticket using the correct reading type.
        bool includeReading =
            customer.WantsReading && customer.CanAffordReading;

        if (includeReading)
        {
            if (!customer.GiveReadingTicket(customer.PurchaseReading))
            {
                Debug.LogWarning(
                    $"Cashier could not issue a reading ticket to {customer.name}.",
                    this
                );

                return false;
            }
        }

        // Select the customer's actual payment method.
        bool payingByCard = customer.PaysByCard;

        List<ProductData> productsBeingSold =
            new List<ProductData>(customer.PurchasedProducts);

        bool paymentSucceeded = customer.TryCompleteCheckout(
            payingByCard,
            includeReading,
            customer.PurchaseReading,
            out int total,
            out string message
        );

        Debug.Log(
            $"Cashier checkout: Customer={customer.name}, " +
            $"Payment={(payingByCard ? "Card" : "Cash")}, " +
            $"Success={paymentSucceeded}, Total=${total}, Message={message}",
            this
        );

        if (!paymentSucceeded)
            return false;

        CompleteSale(customer, productsBeingSold, total);

        Debug.Log(
            $"Cashier successfully completed checkout for {customer.name}.",
            this
        );

        return true;
    }

    // --------------------------------------------------
    // SHARED SALE PROCESSING
    // --------------------------------------------------

    private void CompleteSale(
        Customer customer,
        List<ProductData> productsBeingSold,
        int total)
    {
        if (customer == null)
            return;

        // Add earnings once for this completed transaction.
        moneyEarned += total;

        MoneyManager manager =
            moneyManager != null ? moneyManager : MoneyManager.Instance;

        if (manager != null)
        {
            manager.AddMoney(total);

            Debug.Log(
                $"Sale deposited: ${total}. Wallet balance: ${manager.Balance}",
                this
            );
        }
        else
        {
            Debug.LogError(
                "CheckoutUI: MoneyManager.Instance is NULL.",
                this
            );
        }

        // Apply product reputation once per transaction.
        if (ReputationSystem.Instance != null &&
            productsBeingSold != null &&
            productsBeingSold.Count > 0)
        {
            int totalQuality = 0;
            int validProductCount = 0;

            foreach (ProductData product in productsBeingSold)
            {
                if (product == null)
                    continue;

                totalQuality += product.quality;
                validProductCount++;
            }

            if (validProductCount > 0)
            {
                float averageQuality =
                    (float)totalQuality / validProductCount;

                bool highQuality = averageQuality >= 70f;

                ReputationSystem.Instance.ReportProductQuality(highQuality);

                Debug.Log(
                    $"Checkout complete. Average product quality: " +
                    $"{averageQuality:F1}. Reputation " +
                    $"{(highQuality ? "increased" : "decreased")} once.",
                    this
                );
            }
        }

        if (customer.Unsatisfied)
        {
            Debug.LogWarning(
                $"{customer.name} was unhappy with their reading ticket.",
                this
            );
        }
    }

    // --------------------------------------------------
    // CANCEL CHECKOUT
    // --------------------------------------------------

    public void CancelCheckout(Customer customer)
    {
        if (customer == null || currentCustomer != customer)
            return;

        Debug.Log(
            $"Checkout canceled because {customer.name} was possessed.",
            this
        );

        if (checkoutPanel != null)
            checkoutPanel.SetActive(false);

        currentCustomer = null;

        selectedCardPayment = null;
        selectedReading = null;
        ticketPrinted = false;

        if (bagSpawn != null)
            bagSpawn.SetCheckoutCustomer(null);

        if (itemsText != null)
            itemsText.text = "";

        if (readingText != null)
            readingText.text = "";

        if (totalText != null)
            totalText.text = "";

        if (loveButton != null)
            loveButton.interactable = false;

        if (wealthButton != null)
            wealthButton.interactable = false;

        if (healthButton != null)
            healthButton.interactable = false;

        if (printTicketButton != null)
            printTicketButton.interactable = false;

        Debug.Log("Checkout released for the next customer.", this);
    }

    // --------------------------------------------------
    // UI TOTAL
    // --------------------------------------------------

    private void UpdateTotal()
    {
        if (currentCustomer == null || totalText == null)
            return;

        int total = currentCustomer.ShoppingTotal;

        if (selectedReading.HasValue)
            total += currentCustomer.ReadingFee;

        totalText.text = $"TOTAL: ${total}";
    }

    private string GetTicketSymbol(Customer.ReadingType readingType)
    {
        switch (readingType)
        {
            case Customer.ReadingType.Love:
                return "♡ LOVE";

            case Customer.ReadingType.Wealth:
                return "$ WEALTH";

            case Customer.ReadingType.Health:
                return "+ HEALTH";

            default:
                return "";
        }
    }
}
