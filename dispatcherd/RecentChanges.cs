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
					Core.WD.Add(parts[2], wiki);
				}
			}
		}

		private static void Traffic(string data)
		{
			if (Configuration.Network.TrafficDump)
			{
				File.AppendAllText ("traffic.dump", data + "\n");
			}
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

			}
		}
	}
}

