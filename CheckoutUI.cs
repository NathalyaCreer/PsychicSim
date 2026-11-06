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
    //[SerializeField] private TMP_Text statusText;
 

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

    // Session earnings; this is not yet saved between game sessions.
    private int moneyEarned;

    private void Awake()
    {
        checkoutPanel.SetActive(false);

        cardButton.onClick.AddListener(SelectCard);
        cashButton.onClick.AddListener(SelectCash);

        loveButton.onClick.AddListener(() =>
            SelectReading(Customer.ReadingType.Love));

        wealthButton.onClick.AddListener(() =>
            SelectReading(Customer.ReadingType.Wealth));

        healthButton.onClick.AddListener(() =>
            SelectReading(Customer.ReadingType.Health));

        printTicketButton.onClick.AddListener(PrintTicket);
        checkoutButton.onClick.AddListener(Checkout);
    }

    public void BeginCheckout(Customer customer)
    {
        if (customer == null)
            return;

        currentCustomer = customer;
       
        if (customer == null || customer.IsDead)
            return;

        // Tell the bag holder which customer is currently checking out.
        if (bagSpawn != null)
            bagSpawn.SetCheckoutCustomer(customer);
        else
            Debug.LogWarning("CheckoutUI is missing its BagSpawn reference.", this);

        selectedCardPayment = null;
        selectedReading = null;
        ticketPrinted = false;

        itemsText.text = customer.PurchasedItemNames.Count > 0
            ? string.Join("\n", customer.PurchasedItemNames)
            : "No products";

        if (customer.WantsReading)
        {
            readingText.text =
                $"Customer Requests: {customer.PurchaseReading} reading (${customer.ReadingFee})";
        }
        else
        {
            readingText.text = "No reading requested.";
        }

        totalText.text = $"Total: ${customer.ShoppingTotal}";

        // Reading options are available only if the customer wants and can afford one.
        bool readingAvailable = customer.WantsReading && customer.CanAffordReading;

        loveButton.interactable = readingAvailable;
        wealthButton.interactable = readingAvailable;
        healthButton.interactable = readingAvailable;
        printTicketButton.interactable = readingAvailable;

        UpdateTotal();

        checkoutPanel.SetActive(true);
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
            readingText.text = "This customer doesn't want a reading.";
            return;
        }

        if (!currentCustomer.CanAffordReading)
        {
            readingText.text = "The customer can't afford the reading fee.";
            return;
        }

        selectedReading = readingType;
        ticketPrinted = false;


        readingText.text = $"Selected reading: {readingType} (${currentCustomer.ReadingFee})";

        UpdateTotal();
    }

    public void PrintTicket()
    {
        if (currentCustomer == null || !selectedReading.HasValue)
        {
            readingText.text = "Select Love, Wealth, or Health first.";
            return;
        }

        if (currentCustomer.GiveReadingTicket(selectedReading.Value))
        {
            ticketPrinted = true;
            readingText.text = $"{selectedReading.Value} ticket given to customer.";
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
        if (currentCustomer == null)
            return;

        // Require a payment method.
        if (!selectedCardPayment.HasValue)
        {
            readingText.text = "Select Cash or Card first.";
            return;
        }

        // Require the reading ticket if a reading was selected.
        bool includeReading = selectedReading.HasValue;

        if (includeReading && !ticketPrinted)
        {
            readingText.text = "Print the reading ticket before checkout.";
            return;
        }

        Customer customerBeingCheckedOut = currentCustomer;

        // Copy products before TryCompleteCheckout clears purchase data.
        List<ProductData> productsBeingSold =
            new List<ProductData>(customerBeingCheckedOut.PurchasedProducts);

        bool paymentSucceeded = customerBeingCheckedOut.TryCompleteCheckout(selectedCardPayment.Value,includeReading,selectedReading.GetValueOrDefault(),out int total, out string message);
        
        Debug.Log($"Checkout result: success={paymentSucceeded}, total=${total}, message={message}",this);

        if (!paymentSucceeded)
        {
            readingText.text = message;
            return;
        }

        // Payment succeeded.
        moneyEarned += total;
        Debug.Log($"Reached wallet deposit. Sale total: ${total}", this);
        Debug.Log($"MoneyManager.Instance exists: {MoneyManager.Instance != null}", this);

        // Deposit this completed sale into the player's persistent wallet.
        if (MoneyManager.Instance != null)
        {
            Debug.Log($"Wallet before sale: ${MoneyManager.Instance.Balance}", this);            
            MoneyManager.Instance.AddMoney(total);            

            Debug.Log($"Wallet after sale: ${MoneyManager.Instance.Balance}", this);
        }
        else
        {
            Debug.LogError("CheckoutUI: MoneyManager.Instance is NULL.", this);

        }

        totalText.text = $"Sale complete: ${total}";
        readingText.text = message;

        // Apply reputation change only once per transaction.
        if (ReputationSystem.Instance != null && productsBeingSold.Count > 0)
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
                float averageQuality = (float)totalQuality / validProductCount;
                bool highQuality = averageQuality >= 70f;

                ReputationSystem.Instance.ReportProductQuality(highQuality);

                Debug.Log(
                    $"Checkout complete. Average product quality: {averageQuality:F1}. " +
                    $"Reputation {(highQuality ? "increased" : "decreased")} once."
                );
            }
        }

        // Handle an incorrect reading ticket.
        if (customerBeingCheckedOut.Unsatisfied)
        {
            readingText.text = "Wrong reading ticket! Customer is unhappy.";
        }

        checkoutPanel.SetActive(false);
        currentCustomer = null;
    }

    public void CancelCheckout(Customer customer)
    {
        // Never cancel another customer's checkout by mistake.
        if (customer == null || currentCustomer != customer)
            return;

        Debug.Log(
            $"Checkout canceled because {customer.name} was possessed.",
            this
        );

        // Release the checkout UI.
        checkoutPanel.SetActive(false);
        currentCustomer = null;

        // Reset selections so the next customer starts fresh.
        selectedCardPayment = null;
        selectedReading = null;
        ticketPrinted = false;

        // Clear the active customer assigned to the bag spawner.
        if (bagSpawn != null)
            bagSpawn.SetCheckoutCustomer(null);

        // Clear old customer information from the UI.
        itemsText.text = "";
        readingText.text = "";
        totalText.text = "";

        // Reset button states.
        loveButton.interactable = false;
        wealthButton.interactable = false;
        healthButton.interactable = false;
        printTicketButton.interactable = false;

        Debug.Log("Checkout released for the next customer.", this);
    }
    private void UpdateTotal()
    {
        if (currentCustomer == null)
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