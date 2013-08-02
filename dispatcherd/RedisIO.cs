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
using Newtonsoft.Json;

namespace dispatcherd
{
    /// <summary>
    /// Helper class to sort out redis stuff
    /// </summary>
    class RedisIO
    {
        /// <summary>
        /// This function will take the diff and convert it to format that user specified in their subscription
        /// </summary>
        /// <param name="diff">Diff that is converted</param>
        /// <param name="format">Format of data</param>
        /// <param name="wiki">Site</param>
        /// <returns></returns>
        public static string Format2Redis(ChangeItem diff, Subscription.Format format, Wiki wiki)
        {
            Core.DebugLog("User: " + diff.User);
            if (format == Subscription.Format.XML)
            {
                XmlDocument d = new XmlDocument();
                XmlNode node = d.CreateElement("rc");

                XmlAttribute name = d.CreateAttribute("wiki");
                name.Value = wiki.Name;
                XmlAttribute pagename = d.CreateAttribute("title");
                pagename.Value = diff.Title;
                node.Attributes.Append(name);
                node.Attributes.Append(pagename);

                XmlAttribute FullTitle = d.CreateAttribute("fulltitle");
                FullTitle.Value = diff.Title;
                node.Attributes.Append(FullTitle);

                if (diff.Namespace != null)
                {
                    XmlAttribute _namespace = d.CreateAttribute("namespace");
                    _namespace.Value = diff.Namespace;
                    node.Attributes.Append(_namespace);
                }

                XmlAttribute ts = d.CreateAttribute("timestamp");
                ts.Value = diff.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
                node.Attributes.Append(ts);

                return node.InnerXml;
            }

            if (format == Subscription.Format.JSON)
            {
                return JsonConvert.SerializeObject(diff).Replace("\n", "");
            }

            return wiki.Name + "|" + diff.Title + "|" + diff.User + "|" + diff.Action.ToString()
            + "|" + diff.DiffID + "|" + diff.ChangeID + "|" + diff.ChangeSize + "|"
                    + diff.Summary;
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// This function will send stuff to redis queue
        /// </summary>
        /// <param name="diff"></param>
        /// <param name="subscription"></param>
        /// <param name="wiki"></param>
        public static void RedisSend(ChangeItem diff, Subscription subscription, Wiki wiki)
        {
            string data = Format2Redis(diff, subscription.format, wiki);
            Core.DebugLog("Sending to redis: Q " + subscription.Name + ": " + data, 6);
            Core.redis.LPush(subscription.Name, GetBytes(data));
        }
    }
}
