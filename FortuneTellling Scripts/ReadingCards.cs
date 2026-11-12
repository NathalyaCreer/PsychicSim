using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class ReadingCards : MonoBehaviour
{
    #region Parameters

    [Header("Reading")]
    [SerializeField] private Customer.ReadingType deckReadingType;
    [SerializeField] private List<CardData> deckCards = new List<CardData>();

    [Header("Reading Type UI")]
    [SerializeField] private GameObject readingTypePanel;
    [SerializeField] private Button loveButton;
    [SerializeField] private Button wealthButton;
    [SerializeField] private Button healthButton;

    [Header("3D Card Prefab")]
    [SerializeField] private Card3D cardPrefab;

    [Header("Card Layout")]
    [SerializeField] private Transform cardGridAnchor;
    [SerializeField] private Transform selectedRowAnchor;
    [SerializeField] private int columns = 4;
    [SerializeField] private int rows = 3;
    [SerializeField] private float gridSpacingX = 0.8f;
    [SerializeField] private float gridSpacingZ = 1.1f;
    [SerializeField] private float selectedSpacingX = 1.0f;
    [SerializeField] private float moveToSelectedRowDuration = 0.5f;

    [Header("Reading Result")]
    [SerializeField] private TMP_Text combinedMeaningText;
    [SerializeField] private float resetDelay = 10f;

    [Header("Reading Line")]
    [SerializeField] private FortuneTellingLine fortuneTellingLine;

    [Header("Selection")]
    [SerializeField] private int cardsToSelect = 3;
    [SerializeField] private float cardFlipDuration = 0.5f;

    [Header("Customer")]
    [SerializeField] private Customer waitingCustomer;

    public event Action<bool> ReadingCompleted;

    private readonly List<Card3D> dealtCards = new List<Card3D>();
    private readonly List<Card3D> selectedCards = new List<Card3D>();

    private bool readingStarted;
    private bool readingFinished;
    private bool isMovingSelectedCards;

    public Customer.ReadingType DeckReadingType => deckReadingType;
    public bool HasActiveSpread => readingStarted;

    public Customer WaitingCustomer => waitingCustomer;

    public void SetWaitingCustomer(Customer customer)
    {
        waitingCustomer = customer;

        Debug.Log(
            waitingCustomer != null
                ? $"ReadingCards now has waiting customer: {waitingCustomer.name}"
                : "ReadingCards waiting customer has cleared."
        );
    }

    public bool IsReadingCompleteFor(Customer customer)
    {
        return readingFinished && waitingCustomer == customer;
    }

    // The table/deck interaction should open the reading-type selection UI.
    // It should NOT deal cards by itself.
    #endregion


    private void Update()
    {
        // Don't replace the customer while a reading is active.
        if (readingStarted || waitingCustomer != null)
            return;

        if (fortuneTellingLine == null)
            return;

        Customer frontCustomer = fortuneTellingLine.FrontCustomer;

        // Assign the customer only when they reach the front.
        if (frontCustomer != null &&
            fortuneTellingLine.IsReadyForReading(frontCustomer))
        {
            SetWaitingCustomer(frontCustomer);

            Debug.Log(
                $"ReadingCards: {frontCustomer.name} is ready for a reading."
            );
        }
    }

    public void Interact()
    {
        if (waitingCustomer == null)
        {
            Debug.Log("ReadingCards: No customer is waiting. Cannot start a reading.");
            readingTypePanel.SetActive(true);
            return;
        }

        Debug.Log("ReadingCards: Customer is waiting. Choose a reading type.");
        
        if(readingTypePanel == null)
        {
            Debug.LogWarning("ReadingCards: Reading type panel is not assigned");
            return;
        }

        readingTypePanel.SetActive(true);
    }

    // Call this from the Love/Wealth/Health selection buttons.
    public void StartReading(Customer.ReadingType chosenType)
    {
        if (waitingCustomer == null)
        {
            Debug.LogWarning("ReadingCards: Cannot deal cards without a waiting customer.");
            readingTypePanel.SetActive(true);
            return;
        }

        // Choosing the already-active type should not create another spread.
        if (readingStarted && deckReadingType == chosenType)
        {
            Debug.Log("ReadingCards: That reading type is already active.");
            return;
        }

        // If another type/spread is active, remove its cards first.
        if (readingStarted || dealtCards.Count > 0)
        {
            ClearSpread();
        }

        deckReadingType = chosenType;
        BeginReading();
    }

    private void BeginReading()
    {
        if (waitingCustomer == null)
        {
            Debug.LogWarning("ReadingCards: No customer is waiting.");
            return;
        }

        if (readingStarted)
            return;

        if (cardPrefab == null || cardGridAnchor == null || selectedRowAnchor == null)
        {
            Debug.LogError("ReadingCards: Assign the card prefab, grid anchor, and selected-row anchor.");
            return;
        }

        int cardsNeeded = columns * rows;

        if (deckCards == null || deckCards.Count < cardsNeeded)
        {
            Debug.LogError($"ReadingCards: The deck needs at least {cardsNeeded} CardData entries.");
            return;
        }

        readingStarted = true;
        readingFinished = false;
        isMovingSelectedCards = false;

        selectedCards.Clear();

        if (combinedMeaningText != null)
            combinedMeaningText.text = "";

        DealCards();
    }

    private void DealCards()
    {
        int cardsNeeded = columns * rows;

        List<CardData> shuffledDeck = new List<CardData>(deckCards);
        Shuffle(shuffledDeck);

        for (int i = 0; i < cardsNeeded; i++)
        {
            int row = i / columns;
            int column = i % columns;

            Vector3 localOffset = new Vector3(
                column * gridSpacingX,
                0f,
                -row * gridSpacingZ
            );

            Vector3 worldPosition = cardGridAnchor.TransformPoint(localOffset);

            Card3D card = Instantiate(
                cardPrefab,
                worldPosition,
                cardGridAnchor.rotation,
                cardGridAnchor
            );

            card.Setup(shuffledDeck[i], this, deckReadingType);
            card.SetSelected(false);
            card.SetFaceUp(false);

            dealtCards.Add(card);
        }

        Debug.Log($"ReadingCards: Dealt one {deckReadingType} spread.");
    }

    public void SelectCard(Card3D card)
    {
        // Require both an active spread and a customer.
        if (waitingCustomer == null)
        {
            Debug.LogWarning("ReadingCards: Card selection blocked—no customer is waiting.");
            return;
        }

        if (!readingStarted || readingFinished || isMovingSelectedCards)
        {
            Debug.LogWarning("ReadingCards: Card selection blocked by reading state.");
            return;
        }

        if (card == null || selectedCards.Contains(card))
            return;

        if (!dealtCards.Contains(card))
        {
            Debug.LogWarning("ReadingCards: That card does not belong to the active spread.");
            return;
        }

        if (selectedCards.Count >= cardsToSelect)
            return;

        selectedCards.Add(card);
        card.SetSelected(true);

        Debug.Log($"Card selected: {selectedCards.Count}/{cardsToSelect}");

        if (selectedCards.Count == cardsToSelect)
            StartCoroutine(FinishCardSelection());
    }

    private IEnumerator FinishCardSelection()
    {
        isMovingSelectedCards = true;

        for (int i = 0; i < selectedCards.Count; i++)
        {
            Card3D card = selectedCards[i];

            Vector3 localOffset = new Vector3(
                (i - (selectedCards.Count - 1) / 2f) * selectedSpacingX,
                0f,
                0f
            );

            Vector3 targetPosition = selectedRowAnchor.TransformPoint(localOffset);
            Quaternion targetRotation = selectedRowAnchor.rotation;

            yield return StartCoroutine(
                MoveCard(
                    card.transform,
                    targetPosition,
                    targetRotation,
                    moveToSelectedRowDuration
                )
            );

            card.transform.SetParent(selectedRowAnchor, true);

            yield return StartCoroutine(card.RevealFlip(cardFlipDuration));
        }

        ShowCombinedMeaning();
        ResolveReading();

        isMovingSelectedCards = false;

        // Keep the completed reading visible.
        yield return new WaitForSeconds(resetDelay);

        // Save the customer before clearing the spread.
        Customer completedCustomer = waitingCustomer;

        ClearSpread();

        // Remove the completed customer from the line.
        // The next customer will move to the front.
        if (completedCustomer != null && fortuneTellingLine != null)
        {
            fortuneTellingLine.LeaveLine(completedCustomer);
        }

        // Clear the active customer so ReadingCards can assign the next one.
        SetWaitingCustomer(null);
    }

    #region MoveCard()
    private IEnumerator MoveCard(
        Transform cardTransform,
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration)
    {
        Vector3 startPosition = cardTransform.position;
        Quaternion startRotation = cardTransform.rotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            cardTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            cardTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        cardTransform.position = targetPosition;
        cardTransform.rotation = targetRotation;
    }
    #endregion
    private void ShowCombinedMeaning()
    {
        string combinedMeaning = "";

        foreach (Card3D card in selectedCards)
        {
            if (card == null || card.Data == null)
                continue;

            string meaning = card.Data.GetMeaning(deckReadingType);

            if (string.IsNullOrWhiteSpace(meaning))
                continue;

            if (!string.IsNullOrEmpty(combinedMeaning))
                combinedMeaning += " ";

            combinedMeaning += meaning.Trim();
        }

        if (combinedMeaningText != null)
            combinedMeaningText.text = combinedMeaning;

        Debug.Log($"Tarot reading meaning: {combinedMeaning}");
    }


    private void ResolveReading()
    {
        if (waitingCustomer == null)
        {
            Debug.LogWarning("ReadingCards: Customer is no longer waiting.");

            readingFinished = true;
            ReadingCompleted?.Invoke(false);

            return;
        }

        bool successfulReading =
            deckReadingType == waitingCustomer.PurchaseReading;

        readingFinished = true;

        Debug.Log(successfulReading
            ? "Tarot reading successful!"
            : "Tarot reading failed.");

        // Notify any systems listening for the reading result.
        ReadingCompleted?.Invoke(successfulReading);

        // Update store reputation.
        if (ReputationSystem.Instance != null)
        {
            ReputationSystem.Instance.ReportCardReading(successfulReading);
        }
        else
        {
            Debug.LogWarning(
                "ReadingCards: ReputationSystem.Instance is missing."
            );
        }
    }


    // Removes only the spawned card spread, not the table/deck object.
    private void ClearSpread()
    {
        StopAllCoroutines();

        foreach (Card3D card in dealtCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        dealtCards.Clear();
        selectedCards.Clear();

        if (combinedMeaningText != null)
            combinedMeaningText.text = "";

        readingStarted = false;
        readingFinished = false;
        isMovingSelectedCards = false;
    }

    // Call this when you want to clear the current spread manually.
    public void ResetReading()
    {
        ClearSpread();
    }

    private void Shuffle(List<CardData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            CardData temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void ChooseLoveReading()
    {
        Debug.Log("Player chose Love reading.");
        readingTypePanel.SetActive(false);
        StartReading(Customer.ReadingType.Love);
    }

    public void ChooseWealthReading()
    {
        Debug.Log("Player chose Wealth reading.");
        readingTypePanel.SetActive(false);
        StartReading(Customer.ReadingType.Wealth);
    }

    public void ChooseHealthReading()
    {
        Debug.Log("Player chose Health reading.");
        readingTypePanel.SetActive(false);
        StartReading(Customer.ReadingType.Health);
    }
    public void CancelReadingFor(Customer customer)
    {
        if (customer == null || waitingCustomer != customer)
            return;

        // Remove any cards and stop the current reading coroutine.
        ClearSpread();

        SetWaitingCustomer(null);

        if (readingTypePanel != null)
            readingTypePanel.SetActive(false);
    }




}