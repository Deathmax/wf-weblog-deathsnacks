using System;
using log4net;
using Quartz;
using Warframe_WebLog.Helpers;

namespace Warframe_WebLog.Classes.Jobs
{
    public class MinuteStatusJob : IJob
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(MinuteStatusJob));
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                Status.Status.UpdateStatus();
            }
            catch (Exception e)
            {
                Log.Error("Exception thrown when checking server status.");
                Log.Error(e.ToString());
            }
        }
    }
}