using System;
using log4net;
using Quartz;
using Warframe_WebLog.Helpers;

namespace Warframe_WebLog.Classes.Jobs
{
    public class MinutePcChinaWorldStateJob : IJob
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(MinutePcChinaWorldStateJob));
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                //WorldState.WorldState.Update(false);
                Program.ParserDictionary[Platform.PcChina].Update(false);
            }
            catch (Exception e)
            {
                Log.Error("Exception thrown when updating China WorldState.");
                Log.Error(e.ToString());
            }
        }
    }
}