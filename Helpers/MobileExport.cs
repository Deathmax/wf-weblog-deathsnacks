using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;

namespace Warframe_WebLog.Helpers
{
    public class MobileExport
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(MobileExport));

        private const string MobileExportUrl = "http://content.warframe.com/MobileExport/Manifest/";
        private static readonly string[] JsonFiles = {
            "ExportUpgrades.json",
            "ExportWeapons.json",
            "ExportWarframes.json",
            "ExportSentinels.json",
            "ExportEnemies.json",
            "ExportResources.json",
            "ExportDrones.json",
            "ExportCustoms.json",
            "ExportFlavour.json",
            "ExportKeys.json",
            "ExportGear.json",
            "ExportRegions.json"
        };
        private const string MobileRetrieveRecipesUrl = "https://api.warframe.com/API/PHP/mobileRetrieveRecipes.php";

        private const string CommunityLanguageDataUrl =
                "https://raw.githubusercontent.com/Warframe-Community-Developers/warframe-worldstate-data/master/data/languages.json"
            ;

        private static Dictionary<string, string> _lookupDictionary;
        private static Dictionary<string, JsonValue> _newLookupDictionary;
        private static Dictionary<string, dynamic> _regionDictionary;

        private static DateTime _lastFetch = DateTime.MinValue;

        private static string GetRecipeResult(string rawBlueprint)
        {
            try
            {
                using (var wc = new WebClient {Proxy = null})
                {
                    var values = new NameValueCollection
                    {
                        {"mobile", "true"},
                        {
                            "recipes", JsonConvert.SerializeObject(new List<Dictionary<string, dynamic>> { new Dictionary<string, dynamic>()
                            {
                                {"ItemCount", 1},
                                {"ItemType", rawBlueprint}
                            }})
                        }
                    };
                    var response = wc.UploadValues(MobileRetrieveRecipesUrl, values);
                    dynamic responseJson = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(response));
                    return (string)responseJson[0].ResultType;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to get recipe for " + rawBlueprint);
            }
            return null;
        }

        private static string ConvertRawToSafeString(string rawJson)
        {
            return rawJson.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\\", "/");
        }

        private static void LoadFiles()
        {
            Log.Info("Loading MobileExport files.");
            _lookupDictionary = new Dictionary<string, string>();
            _regionDictionary = new Dictionary<string, dynamic>();
            foreach (var file in JsonFiles)
            {
                dynamic loadJson = JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine("mobileexport", file)));
                if (file == "ExportRegions.json")
                {
                    foreach (var entry in loadJson[file.Replace(".json", "")])
                    {
                        try
                        {
                            _regionDictionary.Add((string) entry.uniqueName, entry);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    continue;
                }
                foreach (var entry in loadJson[file.Replace(".json", "")])
                {
                    if (entry.name == ((string) entry.name).ToUpper())
                    {
                        entry.name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(((string)entry.name).ToLower());
                    }
                    try
                    {
                        _lookupDictionary.Add((string) entry.uniqueName, (string) entry.name);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        private static void FetchFiles()
        {
            if (_lookupDictionary != null && (DateTime.UtcNow - _lastFetch).TotalMinutes < 10)
            {
                //Log.Info("MobileExport files still fresh, skipping fetch.");
                //avoid fetching too often
                return;
            }
            _lastFetch = DateTime.UtcNow;
            Log.Info("Fetching MobileExport files.");
            if (!Directory.Exists("mobileexport"))
            {
                Directory.CreateDirectory("mobileexport");
            }
            Parallel.ForEach(JsonFiles, file =>
            {
                using (var wc = new WebClient { Proxy = null })
                {
                    try
                    {
                        File.WriteAllText(Path.Combine("mobileexport", file),
                            ConvertRawToSafeString(wc.DownloadString(MobileExportUrl + file)));
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Failed to download " + file, ex);
                    }
                }
            });
            Log.Info("Fetching CommunityDeveloper files.");
            using (var wc = new WebClient {Proxy = null})
            {
                try
                {
                    // Download the community maintained language file from
                    // https://github.com/Warframe-Community-Developers/warframe-worldstate-data
                    var rawJson = wc.DownloadString(CommunityLanguageDataUrl);
                    _newLookupDictionary = JsonConvert.DeserializeObject<Dictionary<string, JsonValue>>(rawJson);
                    Log.Info("Loaded new community languages.json.");
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to download community languages.json", ex);
                }
            }
            LoadFiles();
        }

        public static void Initialize()
        {
            FetchFiles();
        }

        public static string GetPlanetName(string node)
        {
            return _regionDictionary.ContainsKey(node) ? (string) _regionDictionary[node].name : null;
        }

        public static string GetPlanetNameWithRegion(string node)
        {
            if (!_regionDictionary.ContainsKey(node)) return null;
            var data = _regionDictionary[node];
            return $"{(string) data.name} ({(string) data.systemName})";
        }

        public static string GetRegion(string node)
        {
            return _regionDictionary.ContainsKey(node) ? (string) _regionDictionary[node].systemName : null;
        }

        public static string GetNodeMission(string node)
        {
            return _regionDictionary.ContainsKey(node)
                ? Names.GetMissionType((int) _regionDictionary[node].missionIndex)
                : null;
        }

        public static string GetItemName(string raw)
        {
            FetchFiles();
            if (_lookupDictionary.ContainsKey(raw))
            {
                return _lookupDictionary[raw];
            }

            var removeStoreItems = raw.Replace("/StoreItems", "");
            if (_lookupDictionary.ContainsKey(removeStoreItems))
            {
                return _lookupDictionary[removeStoreItems];
            }

            if (removeStoreItems.Contains("Recipe") || removeStoreItems.Contains("Blueprint"))
            {
                var getRecipeResult = GetRecipeResult(removeStoreItems);
                if (getRecipeResult != null)
                {
                    if (_lookupDictionary.ContainsKey(getRecipeResult))
                    {
                        if (removeStoreItems.ToLower().Contains("althelmet"))
                        {
                            var splitParts = _lookupDictionary[getRecipeResult].Split(new []{' '}, 3);
                            if (splitParts.Length == 3)
                            {
                                return splitParts[1] + " " + splitParts[0] + " " + splitParts[2] + " Blueprint";
                            }
                        }
                        return _lookupDictionary[getRecipeResult] + " Blueprint";
                    }
                }
            }

            return null;
        }

        public static string GetNewItemName(string raw)
        {
            raw = raw.ToLower();
            return _newLookupDictionary.ContainsKey(raw) ? _newLookupDictionary[raw].Value : null;
        }

        private class JsonValue
        {
            [JsonProperty("value")]
            public string Value;
        }
    }
}
