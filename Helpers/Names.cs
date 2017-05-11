namespace Warframe_WebLog.Helpers
{
    public class Names
    {
        public static string GetMissionType(string raw)
        {
            switch (raw.ToLower())
            {
                case "mt_defense":
                    return "Defense";
                case "mt_assassination":
                    return "Assassination";
                case "mt_extermination":
                    return "Extermination";
                case "mt_survival":
                    return "Survival";
                case "mt_intel":
                    return "Spy";
                case "mt_capture":
                    return "Capture";
                case "mt_sabotage":
                    return "Sabotage";
                case "mt_counter_intel":
                    return "Deception";
                case "mt_rescue":
                    return "Rescue";
                case "mt_mobile_defense":
                    return "Mobile Defense";
                case "mt_territory":
                    return "Interception";
                case "mt_retrieval":
                    return "Retrieval";
                case "mt_hive":
                    return "Hive Sabotage";
                case "mt_excavate":
                    return "Excavation";
                default:
                    return raw;
            }
        }

        public static string GetMissionType(int id)
        {
            switch (id)
            {
                case 0: //
                    return "Assassination";
                case 1: //
                    return "Extermination";
                case 2: //
                    return "Survival";
                case 3: //
                    return "Rescue";
                case 4: //
                    return "Sabotage";
                case 5: //
                    return "Capture";
                case 6: //
                    return "Deception";
                case 7: //
                    return "Spy";
                case 8: //
                    return "Defense";
                case 9: //
                    return "Mobile Defense";
                case 10: //
                    return "Relay/Conclave";
                // no 11/12
                case 13: //
                    return "Interception";
                case 14: //
                    return "Hijack";
                // no 15
                case 16: //
                    return "Hive";
                case 18:
                    return "Excavation";
                case 21:
                    return "Region Shortcut";
                case 22:
                    return "Infested Salvage";
                case 23:
                    return "Arena";
                case 24:
                    return "Junction";
                case 25:
                    return "Pursuit";
                case 26:
                    return "Rush";
                case 27:
                    return "Assault";
                default:
                    return $"Unknown: {id}";
            }
        }

        public static string GetInvasionMissionType(string raw)
        {
            switch (raw.ToLower())
            {
                case "spy":
                case "capture":
                case "sabotage":
                case "deception":
                case "rescue":
                    return "Random";
                default:
                    return raw;
            }
        }

        public static string GetFaction(string raw)
        {
            switch (raw.ToLower())
            {
                case "fc_orokin":
                    return "Corrupted";
                case "fc_grineer":
                    return "Grineer";
                case "fc_infestation":
                    return "Infestation";
                case "fc_corpus":
                    return "Corpus";
                default:
                    return raw;
            }
        }

        public static string GetEnemySpec(string raw)
        {
            if (raw == null)
            {
                return "?";
            }
            return raw.Replace("/Lotus/Types/Game/EnemySpecs/", "").Replace("/Lotus/Types/Game/", "");
        }

        public static string GetPlanet(string tweettext)
        {
            return tweettext.Split(':')[0];
        }

        public static string GetRewards(string tweettext)
        {
            var parts = tweettext.Split(new[] {'-'}, 3);
            return parts[2].Replace(" -", ",");
        }
    }
}