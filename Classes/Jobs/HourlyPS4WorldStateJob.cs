using System;
using log4net;
using Quartz;
using Warframe_WebLog.Helpers;

namespace Warframe_WebLog.Classes.Jobs
{
    public class HourlyPS4WorldStateJob : IJob
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(HourlyPS4WorldStateJob));
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                //WorldState.WorldStatePS4.Update(true);
                Program.ParserDictionary[Platform.PS4].Update(false);
            }
            catch (Exception e)
            {
                Log.Error("Exception thrown when updating PS4 WorldState.");
                Log.Error(e.ToString());
            }
        }
    }
}