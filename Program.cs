using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using log4net;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using Warframe_WebLog.Classes.Jobs;
using Warframe_WebLog.Classes.Twitter;
using Warframe_WebLog.Classes.WorldState;
using Warframe_WebLog.Helpers;
using Config = Warframe_WebLog.Classes.Config;

namespace Warframe_WebLog
{
    internal class Program
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        public static Config Config;
        private static DateTime _lastUpdate;
        private static long _updateCount;
        private static bool _exiting;
        private static IScheduler _scheduler;

        public static Dictionary<Platform, WorldStateParser> ParserDictionary =
            new Dictionary<Platform, WorldStateParser>();

        private static int Main(string[] args)
        {
            if (!File.Exists("config.json"))
            {
                File.WriteAllText("config.json", JsonConvert.SerializeObject(new Config
                {
                    TwitterInfo = new TwitterInfo()
                },
                    Formatting.Indented));
                Log.Error("config.json not found, generated file.");
                return 1;
            }
            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            Twitter.TwitterInfo = Config.TwitterInfo;
            Twitter.Login();
            _lastUpdate = DateTime.UtcNow.AddSeconds(-601);
            FlatFile.Initialize();
            MobileExport.Initialize();

            ParserDictionary.Add(Platform.Pc, new WorldStateParser(Platform.Pc));
            ParserDictionary.Add(Platform.PS4, new WorldStateParser(Platform.PS4));
            ParserDictionary.Add(Platform.Xbox, new WorldStateParser(Platform.Xbox));
            //ParserDictionary.Add(Platform.PcChina, new WorldStateParser(Platform.PcChina));

            //FlatFile.SerializeAlerts(Platform.Pc);
            //FlatFile.SerializeAlerts(Platform.PS4);
            //FlatFile.SerializeInvasion(Platform.Pc);
            //FlatFile.SerializeInvasion(Platform.PS4);
            (new Thread(Update)).Start(args.Length == 0);
            while (true)
            {
                try
                {
                    var word = Console.ReadLine();
                    if (word == null) continue;
                    var parts = word.Split(new[] {' '}, 2);
                    var command = parts[0];
                    switch (command)
                    {
                        case "reload":
                            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
                            break;
                        case "names":
                        {
                            /*FlatFile.PlanetNames =
                                JsonConvert.DeserializeObject<Dictionary<string, string>>(
                                    File.ReadAllText("planetnames.json"));
                            Log.Info("Loaded " + FlatFile.PlanetNames.Count + " planet names from file.");*/
                            FlatFile.PlanetRegionNames =
                                JsonConvert.DeserializeObject<Dictionary<string, string>>(
                                    File.ReadAllText("planetnamesregion.json"));
                            Log.Info("Loaded " + FlatFile.PlanetRegionNames.Count +
                                     " planet names with region from file.");
                            FlatFile.ItemNames =
                                JsonConvert.DeserializeObject<Dictionary<string, string>>(
                                    File.ReadAllText("names.json"));
                            Log.Info("Loaded " + FlatFile.ItemNames.Count + " item names from file.");
                            FlatFile.LanguageStrings =
                                JsonConvert.DeserializeObject<Dictionary<string, string>>(
                                    File.ReadAllText("strings.json"));
                            Log.Info("Loaded " + FlatFile.LanguageStrings.Count + " language strings from file.");
                            break;
                        }
                        case "currenttick":
                        {
                            Log.InfoFormat("Current tick: {0}", _updateCount);
                            break;
                        }
                        case "settick":
                        {
                            long newcount = 0;
                            if (long.TryParse(parts[1], out newcount))
                                _updateCount = newcount;
                            else
                                Log.Warn("Could not parse the param into an int.");
                            break;
                        }
                        case "exit":
                        case "quit":
                            Log.InfoFormat("Current count: {0}", _updateCount);
                            _exiting = true;
                            _scheduler.Shutdown();
                            return 0;
                        case "forcequit":
                        case "forceexit":
                            Log.InfoFormat("Current count: {0}", _updateCount);
                            Environment.Exit(0);
                            return 0;
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Exception thrown when executing command.");
                    Log.Error(e.ToString());
                }
            }
        }

        private static void Update(object now)
        {
            Log.Info("Starting up the scheduler...");
            _scheduler = StdSchedulerFactory.GetDefaultScheduler();
            _scheduler.Start();
            _scheduler.ScheduleJob(JobBuilder.Create<MinutePcWorldStateJob>().Build(),
                TriggerBuilder.Create()
                    .WithCronSchedule("0 1/1 * 1/1 * ? *")
                    .Build());
            _scheduler.ScheduleJob(JobBuilder.Create<MinutePS4WorldStateJob>().Build(),
                TriggerBuilder.Create()
                    .WithCronSchedule("0 1/1 * 1/1 * ? *")
                    .Build());
            _scheduler.ScheduleJob(JobBuilder.Create<MinuteXboxWorldStateJob>().Build(),
                TriggerBuilder.Create()
                    .WithCronSchedule("0 1/1 * 1/1 * ? *")
                    .Build());
            /*_scheduler.ScheduleJob(JobBuilder.Create<MinutePcChinaWorldStateJob>().Build(),
                TriggerBuilder.Create()
                    .WithCronSchedule("0 1/1 * 1/1 * ? *")
                    .Build());*/
            _scheduler.ScheduleJob(JobBuilder.Create<MinuteStatusJob>().Build(),
                TriggerBuilder.Create()
                    .WithCronSchedule("0 0/1 * 1/1 * ? *")
                    .Build());
            _scheduler.ScheduleJob(JobBuilder.Create<HourlyPcWorldStateJob>().Build(),
                TriggerBuilder.Create()
                    .WithCronSchedule("0 0 0/1 1/1 * ? *")
                    .Build());
            _scheduler.ScheduleJob(JobBuilder.Create<HourlyPS4WorldStateJob>().Build(),
                TriggerBuilder.Create()
                    .WithCronSchedule("0 0 0/1 1/1 * ? *")
                    .Build());
            _scheduler.ScheduleJob(JobBuilder.Create<HourlyXboxWorldStateJob>().Build(),
                TriggerBuilder.Create()
                    .WithCronSchedule("0 0 0/1 1/1 * ? *")
                    .Build());
            Log.Info("Starting Update loop.");
            _updateCount = (bool) now ? 359 : 59;
            if (_updateCount == 59)
            {
                if (File.Exists("currenttick.txt"))
                {
                    long tick;
                    if (long.TryParse(File.ReadAllText("currenttick.txt"), out tick))
                    {
                        _updateCount = tick;
                        Log.InfoFormat("Set current tick to {0}.", _updateCount);
                    }
                }
            }
            while (!_exiting)
            {
                Thread.Sleep(100);
                if ((DateTime.UtcNow - _lastUpdate).TotalSeconds < 10)
                    continue;
                _updateCount++;
                File.WriteAllText("currenttick.txt", _updateCount.ToString());
                _lastUpdate = DateTime.UtcNow;
                //try
                //{
                //    //60 seconds
                //    if (_updateCount%3 == 0)
                //        //1 hour
                //        new Thread(() =>
                //        {
                //            try
                //            {
                //                WorldState.Update(_updateCount%360 == 0);
                //            }
                //            catch (Exception e)
                //            {
                //                Log.Error("Exception thrown when updating WorldState.");
                //                Log.Error(e.ToString());
                //            }
                //        }).Start();
                //}
                //catch (Exception ex)
                //{
                //    Log.Error("Exception thrown when updating WorldState.");
                //    Log.Error(ex.ToString());
                //}
                //try
                //{
                //    //60 seconds
                //    if (_updateCount%3 == 0)
                //        //1 hour
                //        new Thread(() =>
                //        {
                //            try
                //            {
                //                WorldStatePS4.Update(_updateCount % 360 == 0);
                //            }
                //            catch (Exception e)
                //            {
                //                Log.Error("Exception thrown when updating PS4 WorldState.");
                //                Log.Error(e.ToString());
                //            }
                //        }).Start();
                //}
                //catch (Exception ex)
                //{
                //    Log.Error("Exception thrown when updating WorldState PS4.");
                //    Log.Error(ex.ToString());
                //}
                //try
                //{
                //    //60 seconds
                //    if (_updateCount%30 == 0)
                //        new Thread(() =>
                //        {
                //            try
                //            {
                //                Status.UpdateStatus();
                //            }
                //            catch (Exception e)
                //            {
                //                Log.Error("Exception thrown when checking server status.");
                //                Log.Error(e.ToString());
                //            }
                //        }).Start();
                //}
                //catch (Exception ex)
                //{
                //    Log.Error("Exception thrown when checking server status.");
                //    Log.Error(ex.ToString());
                //}
                //try
                //{
                //    //10 minutes
                //    if (_updateCount%60 == 0)
                //        new Thread(() =>
                //        {
                //            try
                //            {
                //                WorldState.RenderGraphs();
                //            }
                //            catch (Exception e)
                //            {
                //                Log.Error("Exception thrown when updating graph data.");
                //                Log.Error(e.ToString());
                //            }
                //        }).Start();
                //}
                //catch (Exception ex)
                //{
                //    Log.Error("Exception thrown when updating graph data.");
                //    Log.Error(ex.ToString());
                //}
                //try
                //{
                //    //10 minutes
                //    if (_updateCount%60 == 0)
                //        new Thread(() =>
                //        {
                //            try
                //            {
                //                WorldStatePS4.RenderGraphs();
                //            }
                //            catch (Exception e)
                //            {
                //                Log.Error("Exception thrown when updating graph data PS4.");
                //                Log.Error(e.ToString());
                //            }
                //        }).Start();
                //}
                //catch (Exception ex)
                //{
                //    Log.Error("Exception thrown when updating graph data PS4.");
                //    Log.Error(ex.ToString());
                //}
            }
        }
    }
}