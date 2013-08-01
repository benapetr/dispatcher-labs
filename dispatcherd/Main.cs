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
using System.Net;
using System.IO;
using System.Threading;
using System.Xml;

namespace dispatcherd
{
    class Core
    {
        /// <summary>
        /// Redis server
        /// </summary>
        public static Redis redis;
        /// <summary>
        /// This is a lock used for Save()
        /// </summary>
        public static object SaveLock = new object();
        /// <summary>
        /// If server is running, in case this is false all threads should be turned off ASAP
        /// </summary>
        public static bool IsRunning
        {
            get
            {
                return Running;
            }
        }
        /// <summary>
        /// Change this to false to tell all threads to exit
        /// </summary>
        private static bool Running = true;
        /// <summary>
        /// Wiki data
        /// </summary>
        public static Dictionary<string, Wiki> WD = new Dictionary<string, Wiki>();
        /// <summary>
        /// Database of subscriptions
        /// </summary>
        public static Dictionary<string, Subscription> DB = new Dictionary<string, Subscription>();
        /// <summary>
        /// Set this to true to make core thread save the db
        /// </summary>
        public static bool SaveNeeded = false;

        public static void DebugLog(string text, int Verbosity = 1)
        {
            if (Verbosity <= Configuration.System.Verbosity)
            {
                Log("DEBUG: " + text);
            }
        }

        /// <summary>
        /// Store all subscription data to disk, try to avoid calling this function too often, because it writes data to disk
        /// and eventually need to lock some resources
        /// </summary>
        public static void Save()
        {
            lock (SaveLock)
            {
                Core.DebugLog("Storing db");
                XmlDocument file = new XmlDocument();
                XmlNode n = file.CreateElement("data");

                List<Subscription> f = new List<Subscription>();

                lock (DB)
                {
                    f.AddRange(DB.Values);
                }
                foreach (Subscription x in f)
                {
                    XmlNode feed = file.CreateElement("feed");
                    XmlAttribute name = file.CreateAttribute("n");
                    name.Value = x.Name;
                    XmlAttribute tx = file.CreateAttribute("token");
                    tx.Value = x.token;
                    feed.Attributes.Append(name);
                    feed.Attributes.Append(tx);
                    lock (x.Items)
                    {
                        foreach (FeedItem f2 in x.Items)
                        {
                            XmlNode f3 = file.CreateElement("item");
                            f3.InnerText = f2.wiki;
                            XmlAttribute page = file.CreateAttribute("page");
                            page.Value = f2.Title;
                            XmlAttribute pr = file.CreateAttribute("pagerx");
                            pr.Value = f2.IsRegex.ToString();
                            XmlAttribute user = file.CreateAttribute("user");
                            user.Value = f2.Username;
                            f3.Attributes.Append(page);
                            f3.Attributes.Append(pr);
                            f3.Attributes.Append(user);
                            XmlAttribute us = file.CreateAttribute("ux");
                            us.Value = f2.UsernameIsRegex.ToString();
                            f3.Attributes.Append(us);
                            XmlAttribute ok = file.CreateAttribute("active");
                            ok.Value = f2.Active.ToString();
                            f3.Attributes.Append(ok);
                            feed.AppendChild(f3);
                        }
                    }
                    n.AppendChild(feed);
                }
                file.AppendChild(n);
                if (File.Exists(Configuration.DB))
                {
                    File.Copy(Configuration.DB, Configuration.DB + "~", true);
                }
                file.Save(Configuration.DB);
                if (File.Exists(Configuration.DB + "~"))
                {
                    File.Delete(Configuration.DB + "~");
                }
            }
        }

        /// <summary>
        /// Load all data from disk
        /// </summary>
        public static void Load()
        {
            Core.DebugLog("Loading db");
            if (!File.Exists(Configuration.DB))
            {
                return;
            }
            XmlDocument file = new XmlDocument();
            lock (DB)
            {
                DB.Clear();
                file.Load(Configuration.DB);
                Core.DebugLog("Loaded db");
                if (file.ChildNodes.Count > 0)
                {
                    Core.DebugLog("Loading child nodes");
                    foreach (XmlNode feed in file.ChildNodes[0])
                    {
                        string token = null;
                        string name = null;
                        foreach (XmlAttribute x2 in feed.Attributes)
                        {
                            switch (x2.Name)
                            {
                                case "n":
                                    name = x2.Value;
                                    break;
                                case "token":
                                    token = x2.Value;
                                    break;
                            }
                        }
                        if (name == null)
                        {
                            throw new Exception("This xml node is missing n value " + feed.InnerXml);
                        }
                        Core.DebugLog("Loading " + name);
                        Subscription x = new Subscription(name); ;
                        
                        if (feed.ChildNodes != null && feed.ChildNodes.Count > 0)
                        {
                            foreach (XmlNode item in feed.ChildNodes)
                            {
                                Core.DebugLog("Loading item " + item.InnerText);
                                FeedItem fx = new FeedItem();
                                fx.wiki = item.InnerText;
                                foreach (XmlAttribute XmlAttribute2 in item.Attributes)
                                {
                                    switch (XmlAttribute2.Name)
                                    {
                                        case "page":
                                            fx.Title = XmlAttribute2.Value;
                                            break;
                                        case "pagerx":
                                            fx.IsRegex = bool.Parse(XmlAttribute2.Value);
                                            break;
                                        case "user":
                                            fx.Username = XmlAttribute2.Value;
                                            break;
                                        case "ux":
                                            fx.UsernameIsRegex = bool.Parse(XmlAttribute2.Value);
                                            break;
                                    }
                                    if (XmlAttribute2.Name == "active")
                                    {
                                        fx.Active = bool.Parse(XmlAttribute2.Value);
                                    }
                                }
                                x.Items.Add(fx);
                            }
                        }
                        DB.Add(name, x);
                    }
                }
            }
        }

        public static void Log(string text)
        {
            Console.WriteLine(DateTime.Now.ToString() + ": " + text);
        }

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            if (Parser.Parse(args))
            {
                Log("Dispatcher daemon");
                DebugLog("Verbosity level: " + Configuration.System.Verbosity.ToString());
                Load();
                Log("Initializing terminal");
                Terminal.Init();
                DebugLog("Loading writer");
                Writer.Init();
                Log("Redis");
                redis = new Redis("tools-redis");
                Log("Connecting to feed");
                RecentChanges.Init();
                while (IsRunning)
                {
                    if (SaveNeeded)
                    {
                        Save();
                        SaveNeeded = false;
                    }
                    Thread.Sleep(100);
                }
            }
        }
    }
}
