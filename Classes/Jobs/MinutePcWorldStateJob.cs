using System;
using log4net;
using Quartz;
using Warframe_WebLog.Helpers;

namespace Warframe_WebLog.Classes.Jobs
{
    public class MinutePcWorldStateJob : IJob
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(MinutePcWorldStateJob));
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                //WorldState.WorldState.Update(false);
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