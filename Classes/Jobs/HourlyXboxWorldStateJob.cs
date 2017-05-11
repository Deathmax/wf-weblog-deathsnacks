using System;
using log4net;
using Quartz;
using Warframe_WebLog.Helpers;

namespace Warframe_WebLog.Classes.Jobs
{
    public class HourlyXboxWorldStateJob : IJob
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(HourlyXboxWorldStateJob));
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                //WorldState.WorldStateXbox.Update(true);
                Program.ParserDictionary[Platform.Xbox].Update(true);
            }
            catch (Exception e)
            {
                Log.Error("Exception thrown when updating Xbox WorldState.");
                Log.Error(e.ToString());
            }
        }
    }
}