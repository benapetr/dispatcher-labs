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
		public static bool IsRunning
		{
			get
			{
				return Running;
			}
		}
		private static bool Running = true;
		public static Dictionary<string, Feed> DB = new Dictionary<string, Feed>();

		public static void DebugLog(string text, int Verbosity = 1)
		{
			if (Verbosity <= Configuration.System.Verbosity)
			{
				Log ("DEBUG: " + text);
			}
		}

		public static void Save()
		{
			Core.DebugLog("Storing db");
			XmlDocument file = new XmlDocument();
			XmlNode n = file.CreateElement("data");

			List<Feed> f = new List<Feed>();

			lock (DB)
			{
				f.AddRange(DB.Values);
			}
				foreach (Feed x in f)
				{
					XmlNode feed = file.CreateElement("feed");
					feed.InnerText = x.Name;
					XmlAttribute tx = file.CreateAttribute("token");
					tx.Value = x.token;
					feed.Attributes.Append(tx);
					lock (x.Items)
					{
						foreach (FeedItem f2 in x.Items)
						{
							XmlNode f3 = file.CreateElement("item");
							f3.InnerText = f2.wiki;
							XmlAttribute page = file.CreateAttribute("page");
							page.Value = f2.PageName;
							XmlAttribute pr = file.CreateAttribute("pagerx");
							pr.Value = f2.IsRegex.ToString ();
							XmlAttribute user = file.CreateAttribute("user");
							user.Value = f2.Username;
							f3.Attributes.Append(page);
							f3.Attributes.Append(pr);
							f3.Attributes.Append(user);
							XmlAttribute us = file.CreateAttribute("ux");
							us.Value = f2.UsernameIsRegex.ToString ();
							f3.Attributes.Append(us);
							feed.AppendChild(f3);
						}
					}
					n.AppendChild(feed);
				}
			if (File.Exists(Configuration.DB))
			{
				File.Copy(Configuration.DB, Configuration.DB + "~", true);
			}
			file.Save (Configuration.DB);
			if (File.Exists(Configuration.DB + "~"))
			{
				File.Delete(Configuration.DB + "~");
			}
		}

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
				if (file.ChildNodes.Count > 0)
				{
					foreach (XmlNode feed in file.ChildNodes)
					{
						Feed x = new Feed(feed.InnerText);
						x.token = feed.Attributes["token"].Value;
						if (feed.ChildNodes != null)
						{
							foreach (XmlNode item in feed.ChildNodes)
							{
								FeedItem fx = new FeedItem();
								fx.wiki = item.InnerText;
								fx.PageName = item.Attributes["page"].Value;
								fx.IsRegex = bool.Parse(item.Attributes["pagerx"].Value);
								fx.UsernameIsRegex = bool.Parse(item.Attributes["ux"].Value);
								fx.Username = item.Attributes["user"].Value;
								x.Items.Add(fx);
							}
						}
						DB.Add(feed.InnerText, x);
					}
				}
			}
		}

		public static void Log (string text)
		{
			Console.WriteLine(DateTime.Now.ToString() + ": " + text);
		}

		public static void Main (string[] args)
		{
			if (Parser.Parse(args))
			{
				Log("Dispatcher daemon");
				Load();
				Log ("Initializing terminal");
				Terminal.Init();
				Log("Connecting to feed");
			}
		}
	}
}
