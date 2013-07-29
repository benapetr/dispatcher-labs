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
using System.Text;
using System.Xml;
using System.Net;
using Newtonsoft.Json;
using System.Threading;

namespace dispatcherd
{
    /// <summary>
    /// This is a definition in subscription, each definition can match some pattern
    /// </summary>
    public class FeedItem
    {
        public string wiki = null;
        public string PageName = "";
        public bool IsRegex = false;
        public int Namespace = 0;
        public string Username = "";
        public bool UsernameIsRegex = false;
    }

    public class Subscription
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name = null;
        /// <summary>
        /// Token
        /// </summary>
        public string token = null;
        /// <summary>
        /// Format of how the user wants to receive the data (in redis and such)
        /// </summary>
        public Format format = Format.Pipe;
        /// <summary>
        /// List of definitions of what to watch
        /// </summary>
        public List<FeedItem> Items = new List<FeedItem>();

        /// <summary>
        /// Generate a new private token
        /// </summary>
        public void GenerateToken()
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < 40; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            token = builder.ToString();
        }

        /// <summary>
        /// Convert an XML input to list of items
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<FeedItem> String2List(string list)
        {
            try
            {
                List<FeedItem> items = new List<FeedItem>();
                if (!list.Contains("<items>"))
                {
                    list = "<items>" + list + "</items>";
                }

                XmlDocument d = new XmlDocument();
                d.LoadXml(list);

                foreach (XmlNode node in d.ChildNodes[0])
                {
                    if (node.Name == "item")
                    {
                        FeedItem i = new FeedItem();
                        i.wiki = node.InnerText;
                        if (!Core.WD.ContainsKey(i.wiki))
                        {
                            continue;
                        }
                        foreach (XmlAttribute attribute in node.Attributes)
                        {
                            if (attribute.Name == "page_rx")
                            {
                                i.IsRegex = true;
                                i.PageName = attribute.Value;
                            }
                            if (attribute.Name == "page")
                            {
                                i.IsRegex = false;
                                i.PageName = attribute.Value;
                            }
                            if (attribute.Name == "user_rx")
                            {
                                i.Username = attribute.Value;
                                i.UsernameIsRegex = true;
                            }
                            if (attribute.Name == "user")
                            {
                                i.Username = attribute.Value;
                            }
                        }
                        items.Add(i);
                    }
                }

                return items;
            }
            catch (Exception fail)
            {
                Core.DebugLog("XML exception while parsing " + list);
                Core.DebugLog(fail.ToString());
            }
            return null;
        }

        /// <summary>
        /// Create a new empty subscription with a specific name
        /// </summary>
        /// <param name="name"></param>
        public Subscription(string name)
        {
            Name = name;
        }

        public Subscription(string name, List<FeedItem> items)
        {
            Items.AddRange(items);
            Name = name;
        }

        /// <summary>
        /// This will convert indentical instance of FeedItem to a corresponding FeedItem in a database, it is necessary
        /// in order to delete it from db
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public FeedItem RetrieveItem(FeedItem item)
        {
            lock (Items)
            {
                foreach (FeedItem xx in Items)
                {
                    if (xx.IsRegex == item.IsRegex && xx.wiki == item.wiki &&
                        xx.PageName == item.PageName && xx.Username == item.Username)
                    {
                        return xx;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Insert a list of feed items to db
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public int Insert(List<FeedItem> list)
        {
            int result = 0;
            lock (Items)
            {
                foreach (FeedItem xx in list)
                {
                    if (RetrieveItem(xx) == null)
                    {
                        result++;
                        Items.Add(xx);
                    }
                }
            }
            Core.SaveNeeded = true;
            return result;
        }

        /// <summary>
        /// Delete a list of feed item instance from a db
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public int Delete(List<FeedItem> list)
        {
            int result = 0;
            lock (Items)
            {
                foreach (FeedItem xx in list)
                {
                    FeedItem original = RetrieveItem(xx);
                    if (original != null)
                    {
                        result++;
                        Items.Remove(original);
                    }
                }
            }
            Core.SaveNeeded = true;
            return result;
        }

        /// <summary>
        /// Authenticate
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="token">Token</param>
        /// <returns></returns>
        public static Subscription login(string name, string token)
        {
            lock (Core.DB)
            {
                if (Core.DB.ContainsKey(name))
                {
                    if (Core.DB[name].token == token)
                    {
                        return Core.DB[name];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// fix me
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<FeedItem> JSON2List(string list)
        {
            try
            {
                List<FeedItem> result = JsonConvert.DeserializeObject<List<FeedItem>>(list);
                return result;
            }
            catch (Exception fail)
            {
                Core.DebugLog("JSON exception while parsing " + list);
                Core.DebugLog(fail.ToString());
            }
            return null;
        }

        public enum Format
        {
            Pipe,
            XML,
            JSON
        }
    }
}

