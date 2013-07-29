﻿//  This program is free software; you can redistribute it and/or modify
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
                ts.Value = diff.Time.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
                node.Attributes.Append(ts);



                return node.InnerXml;
            }
            return wiki.Name + "|" + diff.Title + "|" + diff.User + "|" + diff.Action.ToString()
            + "|" + diff.DiffID + "|" + diff.ChangeID + "|" + diff.ChangeSize + "|"
                    + diff.Summary;
        }

        /// <summary>
        /// This function will send stuff to redis queue
        /// </summary>
        /// <param name="diff"></param>
        /// <param name="subscription"></param>
        /// <param name="wiki"></param>
        public static void RedisSend(ChangeItem diff, Subscription subscription, Wiki wiki)
        {
            Core.redis.RightPush(subscription.Name, Format2Redis(diff, subscription.format, wiki));
        }
    }
}