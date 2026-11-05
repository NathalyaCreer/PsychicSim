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
    [field: SerializeField] public string LoveMeaning { get; private set; }
    [field: SerializeField] public string WealthMeaning { get; private set; }
    [field: SerializeField] public string HealthMeaning { get; private set; }

    public string GetMeaning(Customer.ReadingType readingType)
    {
        switch (readingType)
        {
            case Customer.ReadingType.Love:
                return LoveMeaning;

            case Customer.ReadingType.Wealth:
                return WealthMeaning;

            case Customer.ReadingType.Health:
                return HealthMeaning;

            default:
                return "";
        }
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


