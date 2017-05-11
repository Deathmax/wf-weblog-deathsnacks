using System;
using System.Collections.Generic;
using log4net;
using TweetSharp;
using Warframe_WebLog.Helpers;

namespace Warframe_WebLog.Classes.Twitter
{
    /// <summary>
    /// Class for handling Twitter interation
    /// </summary>
    public class Twitter
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Twitter));
        public enum TwitterType
        {
            PCInvasionUpdate,
            PS4InvasionUpdate,
            PCInvasionNew,
            PS4InvasionNew,
            PCAlertFiltered,
            PS4AlertFiltered
        }

        public static TwitterInfo TwitterInfo;
        private static Dictionary<TwitterType, TwitterService> _twitterServices;

        /// <summary>
        /// Login to Twitter accounts.
        /// </summary>
        public static void Login()
        {
            Log.Info("Logging into Twitter.");
            if (string.IsNullOrWhiteSpace(TwitterInfo.ConsumerKey))
            {
                Log.Warn("Twitter info is empty, skipping.");
                return;
            }
            _twitterServices = new Dictionary<TwitterType, TwitterService>();
            foreach (TwitterType type in Enum.GetValues(typeof (TwitterType)))
            {
                _twitterServices[type] = new TwitterService(TwitterInfo.ConsumerKey, TwitterInfo.ConsumerSecret);
            }
            _twitterServices[TwitterType.PCInvasionUpdate].AuthenticateWith(TwitterInfo.InvasionUpdateUserToken,
                TwitterInfo.InvasionUpdateUserSecret);
            _twitterServices[TwitterType.PCInvasionNew].AuthenticateWith(TwitterInfo.InvasionNewUserToken,
                TwitterInfo.InvasionNewUserSecret);
            _twitterServices[TwitterType.PCAlertFiltered].AuthenticateWith(TwitterInfo.FilterUserToken,
                TwitterInfo.FilterUserSecret);
            _twitterServices[TwitterType.PS4InvasionUpdate].AuthenticateWith(TwitterInfo.PS4InvasionUpdateUserToken,
                TwitterInfo.PS4InvasionUpdateUserSecret);
            _twitterServices[TwitterType.PS4InvasionNew].AuthenticateWith(TwitterInfo.PS4InvasionNewUserToken,
                TwitterInfo.PS4InvasionNewUserSecret);
            _twitterServices[TwitterType.PS4AlertFiltered].AuthenticateWith(TwitterInfo.PS4FilterUserToken,
                TwitterInfo.PS4FilterUserSecret);
            Log.Info("Logged into Twitter.");
        }

        /// <summary>
        /// Post tweet
        /// </summary>
        /// <param name="type">Account type to post to</param>
        /// <param name="status">Text to post</param>
        public static void PostStatus(TwitterType type, string status)
        {
            Log.InfoFormat("Sending status to {0}: {1}", type, status);
            if (_twitterServices == null)
            {
                Log.Warn("We didn't log into Twitter, skipping.");
                return;
            }
            var response = _twitterServices[type].SendTweet(new SendTweetOptions {Status = status});
            var ratestatus = _twitterServices[type].Response.RateLimitStatus;
            Log.Info("Sent Twitter status.");
        }
    }
}