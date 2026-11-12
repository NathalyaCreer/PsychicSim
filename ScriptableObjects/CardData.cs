
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Scriptable Objects/CardScriptableObject")]
public class CardData : ScriptableObject
{
    [Header("Card Information")]
    [field: SerializeField] public CardType Type { get; private set; }
    [field: SerializeField] public string CardName { get; private set; }
    [field: SerializeField] public int CardNumber { get; private set; }

    [Header("Card Artwork")]
    [field: SerializeField] public Sprite Image { get; private set; }

    [Header("Reading Frames")]
    [field: SerializeField] public Sprite LoveFrame { get; private set; }
    [field: SerializeField] public Sprite WealthFrame { get; private set; }
    [field: SerializeField] public Sprite HealthFrame { get; private set; }

    [Header("Card Meanings")]
    [TextArea(2, 3)]
    [SerializeField] private List<string> loveMeanings = new List<string>();

    [TextArea(2, 3)]
    [SerializeField] private List<string> wealthMeanings = new List<string>();

    [TextArea(2, 3)]
    [SerializeField] private List<string> healthMeanings = new List<string>();

    public string GetMeaning(Customer.ReadingType readingType)
    {
        List<string> meanings = null;

        switch (readingType)
        {
            case Customer.ReadingType.Love:
                meanings = loveMeanings;
                break;

            case Customer.ReadingType.Wealth:
                meanings = wealthMeanings;
                break;

            case Customer.ReadingType.Health:
                meanings = healthMeanings;
                break;
        }

        if (meanings == null || meanings.Count == 0)
            return "";

        List<string> validMeanings = meanings.FindAll(
            meaning => !string.IsNullOrWhiteSpace(meaning)
        );

        if (validMeanings.Count == 0)
            return "";

        int randomIndex = Random.Range(0, validMeanings.Count);

        return validMeanings[randomIndex];
    }

    public Sprite GetFrame(Customer.ReadingType readingType)
    {
        switch (readingType)
        {
            case Customer.ReadingType.Love:
                return LoveFrame;

            case Customer.ReadingType.Wealth:
                return WealthFrame;

            case Customer.ReadingType.Health:
                return HealthFrame;

            default:
                return null;
        }
    }
}

public enum CardType
{
    Love,
    Wealth,
    Health
}

public enum LoveCardMeaning
{
    Leave,
    ItsLove,
    SeeAPlay,
    Split,
    GoOnADate,
    Fate,
    Doomed,
    TheyreCheating,
    TheyreFaithful,
    BeBrave,
    TryChocolate,
    BuyFlowers
}

public enum HealthCardMeaning
{
    Healthy,
    Sick,
    RunMore,
    TryWorkingOut,
    EatLess,
    EatMore,
    YourePerfect,
    Weak,
    IronDeficency,
    Strong,
    ItsCancer
}

public enum WealthCardMeaning
{
    YourePoor,
    GetAJob,
    Quit,
    InvestInGunkCoin,
    Rich,
    YoureGettingFired,
    NewJobComing,
    WishHarder,
    Homeless,
    Donate,
    Never,
    Always
}
