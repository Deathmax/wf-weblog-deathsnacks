using System;
using log4net;
using Quartz;
using Warframe_WebLog.Helpers;

namespace Warframe_WebLog.Classes.Jobs
{
    public class HourlyPcWorldStateJob : IJob
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(HourlyPcWorldStateJob));
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                //WorldState.WorldState.Update(true);
                Program.ParserDictionary[Platform.Pc].Update(false);
            }
            catch (Exception e)
            {
                Log.Error("Exception thrown when updating WorldState.");
                Log.Error(e.ToString());
            }
        }
    }
}