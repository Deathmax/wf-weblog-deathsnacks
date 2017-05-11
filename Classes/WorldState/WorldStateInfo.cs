using System;
using System.Collections.Generic;

namespace Warframe_WebLog.Classes.WorldState
{
    public class WorldStateInfo
    {
        public List<Event> Events { get; set; }
        public List<object> Goals { get; set; }
        public List<Alert> Alerts { get; set; }
        public List<GlobalUpgrade> GlobalUpgrades { get; set; }
        public List<FlashSale> FlashSales { get; set; }
        public List<Invasion> Invasions { get; set; }
        public List<NodeOverride> NodeOverrides { get; set; }
        public List<BadlandNode> BadlandNodes { get; set; }
        public int Time { get; set; }
        public string BuildLabel { get; set; }
        public int Version { get; set; }
        public List<VoidTrader> VoidTraders { get; set; }
        public PrimeAccessAvailability PrimeAccessAvailability { get; set; }
        public List<DailyDeal> DailyDeals { get; set; }
        public object LibraryInfo { get; set; }
        public List<dynamic> Sorties { get; set; }
        public List<dynamic> PersistentEnemies { get; set; }
        public List<dynamic> ActiveMissions { get; set; }
    }

    public class DailyDeal
    {
        public Id _id { get; set; }
        public string StoreItem { get; set; }
        public Activation Activation { get; set; }
        public Expiry Expiry { get; set; }
        public int Discount { get; set; }
        public int OriginalPrice { get; set; }
        public int SalePrice { get; set; }
        public int AmountTotal { get; set; }
        public int AmountSold { get; set; }
    }

    public class CurrentTarget
    {
        public Activation StartTime { get; set; }
        public string TargetType { get; set; }
        public string EnemyType { get; set; }
        public int PersonalScansRequired { get; set; }
        public double ProgressPercent { get; set; }
    }

    public class LibraryInfo
    {
        public CurrentTarget CurrentTarget { get; set; }
    }

    public class VoidTrader
    {
        public Id _id { get; set; }
        public Config Config { get; set; }
        public Activation Activation { get; set; }
        public Expiry Expiry { get; set; }
        public NextRotation NextRotation { get; set; }
        public int ManifestIndex { get; set; }
        public int NodeIndex { get; set; }
        public string Character { get; set; }
        public string Node { get; set; }
        public List<Manifest> Manifest { get; set; }
    }

    public class Config
    {
        public string Character { get; set; }
        public int HoursAvailable { get; set; }
        public int FrequencyInDays { get; set; }
        public List<string> Nodes { get; set; }
        public List<List<Manifest>> Manifests { get; set; }
    }

    public class NextRotation
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class Manifest
    {
        public string ItemType { get; set; }
        public int PrimePrice { get; set; }
        public int RegularPrice { get; set; }
    }

    public class PrimeAccessAvailability
    {
        public string State { get; set; }
    }

    public class DeploymentActivationTime
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class LastHealTime
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class MissionRewardBadlands
    {
        public int credits { get; set; }
        public int xp { get; set; }
        public List<string> items { get; set; }
        public List<CountedItem> countedItems { get; set; }
        public string randomizedItems { get; set; }
        public bool hidden { get; set; }
    }

    public class BadlandSpectre
    {
        public string Suits { get; set; }
        public string LongGuns { get; set; }
        public string Pistols { get; set; }
        public string Melee { get; set; }
        public string Name { get; set; }
    }

    public class MissionInfoBadlands
    {
        public string missionType { get; set; }
        public string faction { get; set; }
        public double difficulty { get; set; }
        public string levelOverride { get; set; }
        public string enemySpec { get; set; }
        public List<string> badlandMemberNames { get; set; }
        public List<string> badlandMemberSuits { get; set; }
        public string badlandWarlordName { get; set; }
        public string badlandWarlordSuit { get; set; }
        public List<object> customAllySpectres { get; set; }
        public List<object> customEnemySpectres { get; set; }
        public string gameRules { get; set; }
        public List<BadlandSpectre> badlandSpectres { get; set; }
    }

    public class RailId
    {
        public string id { get; set; }
    }

    public class ArmyExpiration
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class BadlandInfo
    {
        public double? BattlePayReserve { get; set; }
        public string BattlePaySetBy { get; set; }
        public string BattlePaySetByClan { get; set; }
        public double? CreditsTaxRate { get; set; }
        public int DamagePerMission { get; set; }
        public string DeployerName { get; set; }
        public DeploymentActivationTime DeploymentActivationTime { get; set; }
        public double? HealCost { get; set; }
        public double? HealRate { get; set; }
        public Id Id { get; set; }
        public bool IsAlliance { get; set; }
        public double? ItemsTaxRate { get; set; }
        public LastHealTime LastHealTime { get; set; }
        public string MOTD { get; set; }
        public string MOTDAuthor { get; set; }
        public double? MaxStrength { get; set; }
        public double? MemberCreditsTaxRate { get; set; }
        public double? MemberItemsTaxRate { get; set; }
        public double? MissionBattlePay { get; set; }
        public MissionInfo MissionInfo { get; set; }
        public string Name { get; set; }
        public double? RailHealReserve { get; set; }
        public string RailType { get; set; }
        public double? StrengthRemaining { get; set; }
        public TaxChangeAllowedTime TaxChangeAllowedTime { get; set; }
        public string TaxLastChangedBy { get; set; }
        public string TaxLastChangedByClan { get; set; }
        public string DeployerClan { get; set; }
        public RailId RailId { get; set; }
    }

    public class TaxChangeAllowedTime
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class DefId
    {
        public string id { get; set; }
    }

    public class AttId
    {
        public string id { get; set; }
    }

    public class WinId
    {
        public string id { get; set; }
    }

    public class Start
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class End
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class History
    {
        public string Def { get; set; }
        public DefId DefId { get; set; }
        public bool DefAli { get; set; }
        public string Att { get; set; }
        public AttId AttId { get; set; }
        public bool AttAli { get; set; }
        public WinId WinId { get; set; }
        public Start Start { get; set; }
        public End End { get; set; }
    }

    public class ConflictExpiration
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class LastNameCacheTime
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class PostConflictCooldown
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class BadlandNode
    {
        public BadlandInfo DefenderInfo { get; set; }
        public string Node { get; set; }
        public Id _id { get; set; }
        public BadlandInfo AttackerInfo { get; set; }
        public List<History> History { get; set; }
        public ConflictExpiration ConflictExpiration { get; set; }
        public string NodeDisplayName { get; set; }
        public string NodeRegionName { get; set; }
        public string NodeGameType { get; set; }
        public LastNameCacheTime LastNameCacheTime { get; set; }
        public PostConflictCooldown PostConflictCooldown { get; set; }
    }

    public class FlashSale
    {
        public Id _id { get; set; }
        public string TypeName { get; set; }
        public StartDate StartDate { get; set; }
        public EndDate EndDate { get; set; }
        public int Discount { get; set; }
        public int RegularOverride { get; set; }
        public int PremiumOverride { get; set; }
    }

    public class StartDate
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class EndDate
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class AttackerMissionInfo
    {
        public string missionType { get; set; }
        public string faction { get; set; }
        public int seed { get; set; }
        public double difficulty { get; set; }
        //public MissionReward missionReward { get; set; }
        public dynamic missionReward;
        public string levelOverride { get; set; }
        public string enemySpec { get; set; }
        public int minEnemyLevel { get; set; }
        public int maxEnemyLevel { get; set; }
    }

    public class CountedItem
    {
        public string ItemType { get; set; }
        public int ItemCount { get; set; }
    }

    public class AttackerReward
    {
        public List<CountedItem> countedItems { get; set; }
        public int? credits { get; set; }
    }

    public class DefenderMissionInfo
    {
        public string missionType { get; set; }
        public string faction { get; set; }
        public int seed { get; set; }
        public double difficulty { get; set; }
        //public MissionReward missionReward { get; set; }
        public dynamic missionReward { get; set; }
        public string levelOverride { get; set; }
        public string enemySpec { get; set; }
        public int minEnemyLevel { get; set; }
        public int maxEnemyLevel { get; set; }
    }

    public class DefenderReward
    {
        public int? credits { get; set; }
        public List<CountedItem> countedItems { get; set; }
    }

    public class PrereqAlt
    {
        public string id { get; set; }
    }

    public class Invasion
    {
        public DateTime LastUpdate;
        public Activation Activation { get; set; }
        public AttackerMissionInfo AttackerMissionInfo { get; set; }
        //public AttackerReward AttackerReward { get; set; }
        public dynamic AttackerReward { get; set; }
        public bool Completed { get; set; }
        public int Count { get; set; }
        public DefenderMissionInfo DefenderMissionInfo { get; set; }
        public DefenderReward DefenderReward { get; set; }
        public string Faction { get; set; }
        public int Goal { get; set; }
        public string Node { get; set; }
        public string ReplacementEnemySpec { get; set; }
        public string ReplacementLevel { get; set; }
        public string ReplacementVipAgent { get; set; }
        public Id _id { get; set; }
        public PrereqAlt PrereqAlt { get; set; }
        public string LocTag { get; set; }
    }

    public class NodeOverride
    {
        public Id _id { get; set; }
        public string Node { get; set; }
        public string Faction { get; set; }
        public string LevelOverride { get; set; }
        public string EnemySpec { get; set; }
        public string VipAgent { get; set; }
        public string MissionType { get; set; }
        public bool? OverrideMissionType { get; set; }
        public bool? Hide { get; set; }
    }

    public class Id
    {
        public string id { get; set; }
    }

    public class Date
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class Event
    {
        public Id _id { get; set; }
        public string Msg { get; set; }
        public List<NewsMessage> Messages { get; set; }
        public string Prop { get; set; }
        public Date Date { get; set; }
    }

    public class NewsMessage
    {
        public string LanguageCode { get; set; }
        public string Message { get; set; }
    }

    public class Activation
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class Expiry
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class MissionReward
    {
        public int credits { get; set; }
        public int xp { get; set; }
        public List<string> items { get; set; }
        public List<CountedItem> countedItems { get; set; }
    }

    public class MissionInfo
    {
        public string descText { get; set; }
        public string location { get; set; }
        public string missionType { get; set; }
        public string faction { get; set; }
        public int seed { get; set; }
        public double difficulty { get; set; }
        public MissionReward missionReward { get; set; }
        public string levelOverride { get; set; }
        public string enemySpec { get; set; }
        public string vipAgent { get; set; }
        public int minEnemyLevel { get; set; }
        public int maxEnemyLevel { get; set; }
        public int maxWaveNum { get; set; }
        public bool nightmare { get; set; }
        public string exclusiveWeapon { get; set; }
        public bool archwingRequired { get; set; }
        public bool isSharkwing { get; set; }
    }

    public class Alert
    {
        public Activation Activation { get; set; }
        //public int AllowReplay { get; set; }
        public Expiry Expiry { get; set; }
        public MissionInfo MissionInfo { get; set; }
        //public int Twitter { get; set; }
        public Id _id { get; set; }
        public string id { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as Alert;
            return (other != null) && Equals(_id.id, other._id.id);
        }

        public bool Equals(Alert other)
        {
            return Equals(_id.id, other._id.id);
        }

        public override int GetHashCode()
        {
            return (_id != null ? _id.id.GetHashCode() : 0);
        }
    }

    public class ExpiryDate
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class GlobalUpgrade
    {
        public Id _id { get; set; }
        public Activation Activation { get; set; }
        public ExpiryDate ExpiryDate { get; set; }
        public string UpgradeType { get; set; }
        public string OperationType { get; set; }
        public int Value { get; set; }
        public string LocalizeTag { get; set; }
        public string LocalizeDescTag { get; set; }
        public string IconTexture { get; set; }
        public string ValidType { get; set; }
        public List<string> Nodes { get; set; }
        public string TweetText { get; set; }
        public string FxLayer { get; set; }
    }
}