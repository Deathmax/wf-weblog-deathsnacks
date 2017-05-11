using System;
using log4net;
using Quartz;
using Warframe_WebLog.Helpers;

namespace Warframe_WebLog.Classes.Jobs
{
    public class MinuteXboxWorldStateJob : IJob
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(MinuteXboxWorldStateJob));
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                //WorldState.WorldStateXbox.Update(false);
                Program.ParserDictionary[Platform.Xbox].Update(false);
            }
            catch (Exception e)
            {
                Log.Error("Exception thrown when updating Xbox WorldState.");
                Log.Error(e.ToString());
            }
        }
    }
}