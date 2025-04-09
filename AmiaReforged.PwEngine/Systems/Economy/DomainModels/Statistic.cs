namespace AmiaReforged.PwEngine.Systems.Economy.DomainModels;

public enum Statistic
{
    // Individual statistics

    /// <summary>
    ///  Affects overall contribution to defense actions in the simulation
    /// </summary>
    Defense,

    /// <summary>
    /// Affects overall contribution to attack actions in the simulation
    /// </summary>
    Attack,

    /// <summary>
    /// How intimidating someone is. Affects the likelihood of certain actions succeeding, but also decreases the likelihood of others. Also increases influence gain.
    /// </summary>
    Dread,

    /// <summary>
    /// How respected someone is. Affects the likelihood of certain actions succeeding, but also decreases the likelihood of others. Also increases influence gain.
    /// </summary>
    Gravitas,

    /// <summary>
    /// A resource spent to perform actions. Influence is gained through various means, such as completing quests, winning battles, or making deals. It can be spent to perform actions, such as recruiting troops, building structures, or conducting diplomacy.
    /// </summary>
    Influence,

    /// <summary>
    /// A measure of piety and devotion. It is gained through various means, such as religious activities, completing quests, or making sacrifices.
    /// </summary>
    Fervor, // Impacts resistance to manipulation (coercion, blackmail) and corruption 

    /// <summary>
    /// How famous someone is. Impacts how many people are willing to follow them, and how many people are willing to listen to them. Renown is lost due to inactivity. Maximum renown is impacted by charisma, persuasion, and other factors.
    /// </summary>
    Renown,

    // Settlement Statistics

    /// <summary>
    /// A percentage value representing how much money is gained from taxes. High tax rates cause unrest to grow over time, while low tax rates cause unrest to decrease over time. Tax rates can be changed at any time, but changing them too often can cause guilds to become risk-averse, lowering trade and prosperity.
    /// </summary>
    TaxRate,

    /// <summary>
    /// An aggregate of the wealth of a settlement. It is gained through various means, such as trade, production, and conquest. It can be spent to perform actions, such as recruiting troops, building structures, or conducting diplomacy.
    /// </summary>
    Prosperity,

    /// <summary>
    /// How effective the guards are at keeping the peace. High security means protection from espionage, sabotage, and other actions that would harm the settlement. However, this can increase unrest and decrease activity in the settlement.
    /// Low security means that activities in the settlement are not as tightly regulated, but makes the settlement more vulnerable to espionage.
    /// Settlement leadership should aim to balance this statistic to prevent the settlement from becoming too oppressive or too chaotic.
    /// </summary>
    Security,

    /// <summary>
    /// How much unrest a settlement is experiencing. Low unrest means the settlement is stable and happy, while high unrest means the settlement is unhappy. High unrest makes citizens easier to manipulate, but also makes them more likely to rebel if security is low enough.
    /// </summary>
    Unrest,

    /// <summary>
    /// How many troops are available to be recruited per month.
    /// </summary>
    Levies,

    /// <summary>
    /// The maximum number of troops that can be stationed in a province.
    /// </summary>
    Garrison,
}