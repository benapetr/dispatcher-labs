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
		public string Page = null;
		public string User = null;
		public string ChangeID = null;
		public action Action = action.Unknown;
		public string DiffID = null;
		public string ChangeSize = null;
		public string Summary;
		public bool Minor = false;
		public string oldid = null;
		public bool New = false;
		public bool Bot = false;

		public enum action
		{
			New,
			Change,
			Delete,
			Protect,
			Unknown
		}
	}
	
	public class RecentChanges
	{
		public static bool Connected = true;
		private static Thread thread;

		public static void Init()
		{
			Load ();
			thread = new Thread(Exec);
			thread.Start();
			Core.DebugLog("Initialized RC");
		}

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
			
			change.Page = text.Substring(text.IndexOf(variables.color + "14[[") + 5);
			change.Page = change.Page.Substring(3);
			if (!change.Page.Contains(variables.color + "14]]"))
			{
				Core.DebugLog("Parser error #2", 6);
				return null;
			}
			
			change.Page = change.Page.Substring(0, change.Page.IndexOf(variables.color + "14"));
			
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

		private static void Load()
		{
			lock (Core.WD)
			{
				Core.WD.Clear();
				string[] db = File.ReadAllLines ("sites");
				foreach (string line in db)
				{
					string[] parts = line.Split('|');
					Wiki wiki = new Wiki();
					wiki.Channel = parts[0];
					wiki.Url = parts[1];
					wiki.Name = parts[2];
					if (Core.WD.ContainsKey (parts[2]))
					{
						throw new Exception("There is already: " + parts[2]);
					}
					Core.WD.Add(parts[2], wiki);
				}
			}
		}

		private static void Traffic(string data)
		{
			if (Configuration.Network.TrafficDump)
			{
				Writer.InsertLine ("traffic.dump", data + "\n", true);
			}
		}

		private static bool Matches(FeedItem item, ChangeItem diff)
		{
			if (!item.IsRegex && item.PageName != "" && item.PageName != diff.Page)
			{
				return false;
			}
			if (item.IsRegex)
			{
				return false;
			}
			if (!item.UsernameIsRegex && item.Username != "" && item.Username != diff.User)
			{
				return false;
			}
			return true;
		}

		public static string Format2Redis(ChangeItem diff, Subscription.Format format, Wiki wiki)
		{
			if (format == Subscription.Format.XML)
			{
				XmlDocument d = new XmlDocument();
				XmlNode node = d.CreateElement("rc");
				XmlAttribute name = d.CreateAttribute("wiki");
				name.Value = wiki.Name;
				XmlAttribute pagename = d.CreateAttribute("pagename");
				pagename.Value = diff.Page;
				node.Attributes.Append(name);
				node.Attributes.Append(pagename);
				return node.InnerXml;
			}
			return wiki.Name + "|" + diff.Page + "|" + diff.User + "|" + diff.Action.ToString()
			+  "|" + diff.DiffID + "|" + diff.ChangeID + "|" + diff.ChangeSize + "|"
					+ diff.Summary;
		}

		public static void RedisSend(ChangeItem diff, Subscription subscription, Wiki wiki)
		{
			Core.redis.RightPush(subscription.Name, Format2Redis(diff, subscription.format, wiki));
		}

		public static Dictionary<string, Wiki> Cache = new Dictionary<string, Wiki>();

		public static Wiki getWiki(string channel)
		{
			lock (Cache)
			{
				if (Cache.ContainsKey(channel))
				{
					return Cache[channel];
				}
				foreach(Wiki wiki in Core.WD)
				{
					if (wiki.Channel == channel)
					{
						Cache.Add(wiki);
						return wiki;
					}
				}
			}
			return null;
		}

		private static void Exec()
		{
			string nick = "bd_" + DateTime.Now.ToBinary ().ToString().Substring (8);
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
			while(Core.IsRunning && irc.Connected)
			{
				string line = irc.ReadLine();
				if (line == null)
				{
					break;
				}
				Traffic(line);

				if (line.StartsWith(":PRIVMSG"))
				{
					string channel = line.Substring (9);
					if (!channel.Contains(" "))
					{
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
									if (Matches(item, c))
									{
										RedisSend(c, subscription, wiki);
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

