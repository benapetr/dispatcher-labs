//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.

using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace dispatcherd
{
    public class Wiki
    {
        public string Name;
        public string Url;
        public string Channel;
    }

    public class variables
    {
        public static string color = ((char)003).ToString();
        /// <summary>
        /// This string represent a character that changes text to bold
        /// </summary>
        public static string bold = ((char)002).ToString();

    }

    public class ChangeItem
    {
        /// <summary>
        /// Name of page
        /// </summary>
        public string Title = null;
        /// <summary>
        /// User who edited the page
        /// </summary>
        public string User = null;
        /// <summary>
        /// ID of change
        /// </summary>
        public string ChangeID = null;
        /// <summary>
        /// Action that was done
        /// </summary>
        public action Action = action.Unknown;
        /// <summary>
        /// ID of diff
        /// </summary>
        public string DiffID = null;
        /// <summary>
        /// Size of change
        /// </summary>
        public string ChangeSize = null;
        /// <summary>
        /// Summary of edit
        /// </summary>
        public string Summary = null;
        /// <summary>
        /// Whether it was a minor edit
        /// </summary>
        public bool Minor = false;
        /// <summary>
        /// Old ID ?? wtf comment me ??
        /// </summary>
        public string oldid = null;
        /// <summary>
        /// Namespace
        /// </summary>
        public string Namespace = null;
        /// <summary>
        /// If this is a new page
        /// </summary>
        public bool New = false;
        /// <summary>
        /// Bot
        /// </summary>
        public bool Bot = false;
        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime Timestamp;

        /// <summary>
        /// Action
        /// </summary>
        public enum action
        {
            /// <summary>
            /// New page
            /// </summary>
            New,
            /// <summary>
            /// Change of existing
            /// </summary>
            Change,
            /// <summary>
            /// Deletion of a page
            /// </summary>
            Delete,
            /// <summary>
            /// Protection of a page
            /// </summary>
            Protect,
            /// <summary>
            /// Unknown
            /// </summary>
            Unknown
        }
    }

    /// <summary>
    /// Help for IRC feed
    /// </summary>
    public class RecentChanges
    {
        /// <summary>
        /// If we are connected to irc feed
        /// </summary>
        public static bool Connected = true;
        /// <summary>
        /// Thread in which this thing run
        /// </summary>
        private static Thread thread;

        /// <summary>
        /// This will start this subsystem in own thread
        /// </summary>
        public static void Init()
        {
            Load();
            thread = new Thread(Exec);
            thread.Start();
            Core.DebugLog("Initialized RC");
        }

        /// <summary>
        /// Convert IRC line to diff
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static ChangeItem String2Change(string text)
        {
            // get a page
            if (!text.Contains(variables.color + "14[["))
            {
                Core.DebugLog("Parser error #1", 6);
                return null;
            }
            ChangeItem change = new ChangeItem();

            if (text.Contains(variables.color + "4 M"))
            {
                change.Minor = true;
            }

            if (text.Contains(variables.color + "4 B"))
            {
                change.Bot = true;
            }

            if (text.Contains(variables.color + "4 N"))
            {
                change.New = true;
            }

            change.Title = text.Substring(text.IndexOf(variables.color + "14[[") + 5);
            change.Title = change.Title.Substring(3);
            if (!change.Title.Contains(variables.color + "14]]"))
            {
                Core.DebugLog("Parser error #2", 6);
                return null;
            }

            change.Title = change.Title.Substring(0, change.Title.IndexOf(variables.color + "14"));

            text = text.Substring(text.IndexOf(variables.color + "14]]") + 5);

            if (text.Contains("?oldid="))
            {
                change.oldid = text.Substring(text.IndexOf("?oldid=") + 7);

                if (!change.oldid.Contains("&"))
                {
                    Core.DebugLog("Parser error #4", 6);
                    return null;
                }

                change.oldid = change.oldid.Substring(0, change.oldid.IndexOf("&"));
            }

            if (text.Contains("?diff="))
            {
                change.DiffID = text.Substring(text.IndexOf("?diff=") + 6);

                if (!change.DiffID.Contains("&"))
                {
                    Core.DebugLog("Parser error #4", 6);
                    return null;
                }
                change.DiffID = change.DiffID.Substring(0, change.DiffID.IndexOf("&"));
            }


            text = text.Substring(text.IndexOf("?diff=") + 6);

            if (!text.Contains(variables.color + "03"))
            {
                Core.DebugLog("Parser error #5", 6);
                return null;
            }

            change.User = text.Substring(text.IndexOf(variables.color + "03") + 3);

            if (!change.User.Contains(" " + variables.color + "5*"))
            {
                Core.DebugLog("Parser error #6", 6);
                return null;
            }

            change.User = change.User.Substring(0, change.User.IndexOf(" " + variables.color + "5*") + 4);

            if (!text.Contains(variables.color + "5"))
            {
                Core.DebugLog("Parser error #7", 6);
                return null;
            }

            text = text.Substring(text.IndexOf(variables.color + "5"));

            if (!text.Contains("("))
            {
                Core.DebugLog("Parser error #8", 6);
                return null;
            }

            change.ChangeSize = text.Substring(text.IndexOf("(") + 1);

            if (!change.ChangeSize.Contains(")"))
            {
                Core.DebugLog("Parser error #10", 6);
                return null;
            }

            change.ChangeSize = change.ChangeSize.Substring(0, change.ChangeSize.IndexOf(")"));

            if (!text.Contains(variables.color + "10"))
            {
                Core.DebugLog("Parser error #14", 6);
                return null;
            }

            change.Summary = text.Substring(text.IndexOf(variables.color + "10") + 3);

            return change;
        }

        /// <summary>
        /// Load all definitions of wiki's
        /// </summary>
        private static void Load()
        {
            lock (Core.WD)
            {
                Core.WD.Clear();
                string[] db = File.ReadAllLines("sites");
                foreach (string line in db)
                {
                    string[] parts = line.Split('|');
                    Wiki wiki = new Wiki();
                    wiki.Channel = parts[0];
                    wiki.Url = parts[1];
                    wiki.Name = parts[2];
                    if (Core.WD.ContainsKey(parts[2]))
                    {
                        throw new Exception("There is already: " + parts[2]);
                    }
                    Core.WD.Add(parts[2], wiki);
                }
            }
        }

        /// <summary>
        /// This will store a traffic data to a file
        /// </summary>
        /// <param name="data"></param>
        private static void Traffic(string data, bool In)
        {
            if (Configuration.Network.TrafficDump)
            {
                if (In)
                {
                    Writer.InsertLine("traffic.dump", "RC << " + data + "\n", true);
                    return;
                }
                Writer.InsertLine("traffic.dump", "RC >> " + data + "\n", true);
            }
        }

        /// <summary>
        /// Check if a definition matches the diff
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="diff"></param>
        /// <returns></returns>
        private static bool Matches(FeedItem definition, ChangeItem diff)
        {
            try
            {
                if (!definition.IsRegex && definition.Title != "" && definition.Title != diff.Title)
                {
                    return false;
                }
                if (definition.IsRegex)
                {
                    System.Text.RegularExpressions.Regex r1 = new System.Text.RegularExpressions.Regex(definition.Title);
                    if (!r1.IsMatch(diff.Title))
                    {
                        return false;
                    }
                }
                if (!definition.UsernameIsRegex && definition.Username != "" && definition.Username != diff.User)
                {
                    return false;
                }
                if (definition.UsernameIsRegex)
                {
                    System.Text.RegularExpressions.Regex r2 = new System.Text.RegularExpressions.Regex(definition.Username);
                    if (!r2.IsMatch(diff.User))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception fail)
            {
                Core.DebugLog("Exception while parsing definition, disabled it: " + fail.ToString());
                // exception handling is CPU expensive and this looks like invalid regex or something
                definition.Active = false;
                Core.SaveNeeded = true;
                return false;
            }
        }

        /// <summary>
        /// This is a cache for wiki so that we don't need to spend so much CPU time resolving the wiki every time
        /// thanks to it we can put strings with name of wikis in a hashtable with corresponding wiki instances
        /// </summary>
        public static Dictionary<string, Wiki> Cache = new Dictionary<string, Wiki>();

        /// <summary>
        /// This function will resolve the wiki instance from a string, the string is a channel name
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static Wiki getWiki(string channel)
        {
            lock (Cache)
            {
                if (Cache.ContainsKey(channel))
                {
                    return Cache[channel];
                }
                foreach (Wiki wiki in Core.WD.Values)
                {
                    if (wiki.Channel == channel)
                    {
                        Cache.Add(channel, wiki);
                        return wiki;
                    }
                }
            }
            return null;
        }

        private static void Exec()
        {
            string nick = "bd_" + DateTime.Now.ToBinary().ToString().Substring(8);
            IRC irc = new IRC(nick, 6667, "irc.wikimedia.org");
            irc.Connect();
            lock (Core.WD)
            {
                foreach (Wiki xx in Core.WD.Values)
                {
                    irc.Join(xx.Channel);
                    Thread.Sleep(200);
                }
            }
            while (Core.IsRunning && irc.Connected)
            {
                string line = irc.ReadLine();
                if (line == null)
                {
                    break;
                }
                Traffic(line, true);

                if (line.StartsWith(":PRIVMSG"))
                {
                    string channel = line.Substring(9);
                    if (!channel.Contains(" "))
                    {
                        Core.DebugLog("Ignoring line that contains no useful data (1): " + line, 12);
                        continue;
                    }
                    channel = line.Substring(0, line.IndexOf(" "));
                    Wiki wiki = getWiki(channel);
                    if (wiki == null)
                    {
                        Core.DebugLog("Error " + channel);
                        continue;
                    }
                    ChangeItem c = String2Change(line);
                    if (c == null)
                    {
                        Core.DebugLog("Ignoring line that contains no useful data: " + line, 12);
                        continue;
                    }
                    // check all subscriptions
                    List<Subscription> subscriptiondata = new List<Subscription>();
                    lock (Core.DB)
                    {
                        subscriptiondata.AddRange(Core.DB.Values);
                    }
                    foreach (Subscription subscription in subscriptiondata)
                    {
                        lock (subscription.Items)
                        {
                            foreach (FeedItem item in subscription.Items)
                            {
                                if (item.wiki == wiki.Name)
                                {
                                    Core.DebugLog("Checking if filter matches for subscription " + subscription.Name, 8);
                                    if (Matches(item, c))
                                    {
                                        RedisIO.RedisSend(c, subscription, wiki);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

