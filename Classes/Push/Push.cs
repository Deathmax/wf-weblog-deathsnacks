using System;
using System.Collections.Generic;
using log4net;
using MySql.Data.MySqlClient;
using PushSharp;
using PushSharp.Android;
using PushSharp.Core;
using Warframe_WebLog.Helpers;

namespace Warframe_WebLog.Classes.Push
{
    /// <summary>
    /// Class for dealing with push notifications for GCM.
    /// </summary>
    public static class Push
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Push));
        private static MySqlConnection _connection;

        /// <summary>
        /// Pushes a GCM data payload to a list of <paramref name="ids"/>.
        /// </summary>
        /// <param name="ids">List of GCM ids</param>
        /// <param name="topic">Unused topic parameter</param>
        /// <param name="ttl">Time to live</param>
        /// <param name="data">Data payload to send</param>
        /// <param name="collapse">Collapse key to use</param>
        public static void PushDataNotification(List<String> ids, string topic, int ttl, Dictionary<string, string> data,
            string collapse = "alerts")
        {
            Log.InfoFormat("Starting push. No. of ids: {0}.", ids.Count);
            var push = new PushBroker();
            push.OnNotificationSent += NotificationSent;
            push.OnChannelException += ChannelException;
            push.OnServiceException += ServiceException;
            push.OnNotificationFailed += NotificationFailed;
            push.OnDeviceSubscriptionExpired += DeviceSubscriptionExpired;
            push.OnDeviceSubscriptionChanged += DeviceSubscriptionChanged;

            push.RegisterGcmService(new GcmPushChannelSettings(Program.Config.GcmSenderId, Program.Config.GcmKey,
                Program.Config.GcmPackage));
            //push.QueueNotification(new GcmNotification().ForDeviceRegistrationId("/topics/" + topic).WithCollapseKey(collapse).WithData(data));
            for (var i = 0; i < Math.Ceiling((double) ids.Count/999); i++)
            {
                push.QueueNotification(new GcmNotification().ForDeviceRegistrationId(ids.Page(999, i))
                    .WithCollapseKey(collapse)
                    /*.WithTimeToLive(ttl)*/
                    .WithData(data));
            }
            push.StopAllServices();
            Log.InfoFormat("Queues have drained.");
        }

        /// <summary>
        /// Push data payload to a <paramref name="topic"/>.
        /// </summary>
        /// <param name="push"></param>
        /// <param name="topic">Topic to send to</param>
        /// <param name="data">Data payload to send</param>
        /// <param name="collapse">Collapse key to use</param>
        private static void PushTopics(PushBroker push, string topic, Dictionary<string, string> data, string collapse)
        {
            foreach (var pair in data)
            {
                push.QueueNotification(
                    new GcmNotification().ForDeviceRegistrationId("/topics/" + topic)
                        .WithCollapseKey(collapse)
                        .WithData(new Dictionary<string, string> {{pair.Key, pair.Value}}));
            }
        }

        /// <summary>
        /// Pushes a tickle notification to a list of <paramref name="ids"/> that requests
        /// devices to wake up and pull data from the server.
        /// </summary>
        /// <param name="ids">List of GCM ids</param>
        public static void PushTickleNotification(List<String> ids)
        {
            Log.InfoFormat("Starting tickle push. No. of ids: {0}.", ids.Count);
            var push = new PushBroker();
            push.OnNotificationSent += NotificationSent;
            push.OnChannelException += ChannelException;
            push.OnServiceException += ServiceException;
            push.OnNotificationFailed += NotificationFailed;
            push.OnDeviceSubscriptionExpired += DeviceSubscriptionExpired;
            push.OnDeviceSubscriptionChanged += DeviceSubscriptionChanged;

            push.RegisterGcmService(new GcmPushChannelSettings(Program.Config.GcmSenderId, Program.Config.GcmKey,
                Program.Config.GcmPackage));
            for (var i = 0; i < Math.Ceiling((double) ids.Count/999); i++)
            {
                push.QueueNotification(new GcmNotification().ForDeviceRegistrationId(ids.Page(999, i))
                    .WithCollapseKey("tickle")
                    /*.WithTimeToLive(ttl)*/
                    .WithJson("{\"tickle\":true}"));
            }
            push.StopAllServices();
            Log.InfoFormat("Queues have drained.");
        }

        /// <summary>
        /// Event handler for when a device subscription has changed and needs and update to the DB.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="oldsubscriptionid"></param>
        /// <param name="newsubscriptionid"></param>
        /// <param name="notification"></param>
        private static void DeviceSubscriptionChanged(object sender, string oldsubscriptionid, string newsubscriptionid,
            INotification notification)
        {
            Log.WarnFormat("Device id changed: {0} -> {1}", oldsubscriptionid, newsubscriptionid);
            var connectionstr =
                $"SERVER={Program.Config.DbAddress};DATABASE={Program.Config.DbTable};UID={Program.Config.DbUsername};PASSWORD={Program.Config.DbPassword};";
            _connection = new MySqlConnection(connectionstr);
            try
            {
                _connection.Open();
                var cmd = _connection.CreateCommand();
                cmd.CommandText = $"DELETE FROM gcm_ids WHERE id = '{oldsubscriptionid}'";
                cmd.ExecuteNonQuery();
                cmd.Dispose();
                var insertcmd = _connection.CreateCommand();
                insertcmd.CommandText = $"INSERT INTO gcm_ids (id) VALUES('{newsubscriptionid}')";
                insertcmd.ExecuteNonQuery();
                insertcmd.Dispose();
                _connection.Close();
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Error with MySQL.");
                Log.Error(e);
            }
        }

        /// <summary>
        /// Event handler for when a device subscription has expired and needs to be removed from the DB.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="expiredsubscriptionid"></param>
        /// <param name="expirationdateutc"></param>
        /// <param name="notification"></param>
        private static void DeviceSubscriptionExpired(object sender, string expiredsubscriptionid,
            DateTime expirationdateutc, INotification notification)
        {
            Log.WarnFormat("Device id expired, removing: {0}", expiredsubscriptionid);
            var connectionstr =
                $"SERVER={Program.Config.DbAddress};DATABASE={Program.Config.DbTable};UID={Program.Config.DbUsername};PASSWORD={Program.Config.DbPassword};";
            _connection = new MySqlConnection(connectionstr);
            try
            {
                _connection.Open();
                var cmd = _connection.CreateCommand();
                cmd.CommandText = $"DELETE FROM gcm_ids WHERE id = '{expiredsubscriptionid}'";
                cmd.ExecuteNonQuery();
                cmd.Dispose();
                _connection.Close();
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Error with MySQL.");
                Log.Error(e);
            }
        }

        /// <summary>
        /// Event handler for when a notification has failed to send.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="notification"></param>
        /// <param name="error"></param>
        private static void NotificationFailed(object sender, INotification notification, Exception error)
        {
            Log.ErrorFormat("Notification failure: {0} -> {1}", sender, error.Message);
            if (error.Message == "InvalidRegistration")
            {
                try
                {
                    var gcmNotification = (GcmNotification) notification;
                    var ids = gcmNotification.RegistrationIds;
                    if (ids.Count == 1)
                    {
                        var connectionstr =
                            $"SERVER={Program.Config.DbAddress};DATABASE={Program.Config.DbTable};UID={Program.Config.DbUsername};PASSWORD={Program.Config.DbPassword};";
                        Log.WarnFormat("Invalid device id, removing: {0}", ids[0]);
                        _connection = new MySqlConnection(connectionstr);
                        try
                        {
                            _connection.Open();
                            var cmd = _connection.CreateCommand();
                            cmd.CommandText = $"DELETE FROM gcm_ids WHERE id = '{ids[0]}'";
                            cmd.ExecuteNonQuery();
                            cmd.Dispose();
                            _connection.Close();
                        }
                        catch (Exception e)
                        {
                            Log.ErrorFormat("Error with MySQL.");
                            Log.Error(e);
                        }
                    }
                    else
                    {
                        Log.WarnFormat("We having too many ids that are marked invalid, bailing out.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
        }

        /// <summary>
        /// Event handler for when there is an error with the push service.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="error"></param>
        private static void ServiceException(object sender, Exception error)
        {
            Log.ErrorFormat("Service exception: {0} -> {1}", sender, error);
        }

        /// <summary>
        /// Event handler for when there is an error with the push channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="pushchannel"></param>
        /// <param name="error"></param>
        private static void ChannelException(object sender, IPushChannel pushchannel, Exception error)
        {
            Log.ErrorFormat("Channel exception: {0} -> {1}", sender, error);
        }

        /// <summary>
        /// Event handler for when a notification is successfully sent.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="notification"></param>
        private static void NotificationSent(object sender, INotification notification)
        {
            //Log.DebugFormat("Notification sent: {0}", sender);
        }

        /// <summary>
        /// Pushes a GCM data payload to a list of <paramref name="ids"/> specifically for PS4 data.
        /// </summary>
        /// <param name="ids">List of GCM ids</param>
        /// <param name="ttl">Time to live</param>
        public static void PushDataNotificationPs4(List<string> ids, int ttl, Dictionary<string, string> dictionary)
        {
            Log.InfoFormat("Starting PS4 push.");
            dictionary.Add("ps4", "1");
            PushDataNotification(ids, "ps4alerts", ttl, dictionary, "ps4");
        }

        /// <summary>
        /// Pushes a GCM data payload to a list of <paramref name="ids"/> specifically for Xbox data.
        /// </summary>
        /// <param name="ids">List of GCM ids</param>
        /// <param name="ttl">Time to live</param>
        public static void PushDataNotificationXbox(List<string> ids, int ttl, Dictionary<string, string> dictionary)
        {
            Log.InfoFormat("Starting Xbox push.");
            dictionary.Add("xbox", "1");
            PushDataNotification(ids, "xboxalerts", ttl, dictionary, "xbox");
        }
    }
}