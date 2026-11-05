using UnityEngine;
using TMPro;

public class CustomerDialog : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject customerDialogPanel;

    [Header("Labels")]
    [SerializeField] private TMP_Text paymentTypeText;
    [SerializeField] private TMP_Text readingTypeText;
    [SerializeField] private TMP_Text banterText;
    [SerializeField] private TMP_Text endText;

    private Customer currentCustomer;

    private bool? selectedCardPayment;
    private Customer.ReadingType? selectedReading;
    private bool ticketPrinted;

    private readonly string[] banterOptions =
    {
        "Good morning",
        "This is everything",
        "How much?",
        "Hurry, please",
        "Hey"
    };

    private readonly string[] farewellOptions =
    {
        "Bye",
        "Thanks",
        "Have a good one",
        "...",
        "Goodbye"
    };

    private void Awake()
    {
        customerDialogPanel.SetActive(false);
        endText.gameObject.SetActive(false);
    }

    public void AtCheckout(Customer customer)
    {
        currentCustomer = customer;

        banterText.text = GetRandomBanter();

        paymentTypeText.text = customer.PaysByCard
            ? "I'm using card."
            : "I'm using cash.";

        if (customer.WantsReading)
        {
            readingTypeText.text =
                $"I want a {customer.PurchaseReading} reading.";
        }
        else
        {
            readingTypeText.text = "No reading requested.";
        }

        endText.gameObject.SetActive(false);
        customerDialogPanel.SetActive(true);
    }

    public void SelectCard()
    {
        if (currentCustomer == null)
            return;

        selectedCardPayment = true;
        paymentTypeText.text = "I'm using Card.";
    }

    public void SelectCash()
    {
        if (currentCustomer == null)
            return;

        selectedCardPayment = false;
        paymentTypeText.text = "I'm using Cash.";
    }

    public void FinishedCheckout()
    {
        endText.text = GetRandomFarewell();
        endText.gameObject.SetActive(true);

        // Hide the dialog after the checkout/customer departure
        // logic has decided it is finished.
        customerDialogPanel.SetActive(false);
        currentCustomer = null;
    }

    private string GetRandomBanter()
    {
        int index = Random.Range(0, banterOptions.Length);
        return banterOptions[index];
    }

    private string GetRandomFarewell()
    {
        int index = Random.Range(0, farewellOptions.Length);
        return farewellOptions[index];
    }
}
