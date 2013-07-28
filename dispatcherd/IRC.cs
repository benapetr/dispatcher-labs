//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.

using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace dispatcherd
{
	public class IRC
	{
		public string Host;
		public int Port;

		public string Nick;
		public string Username;
		public string Ident;
		private Thread ping;
		public bool Connected = false;

		public NetworkStream ns;
		private StreamReader streamReader;
		private StreamWriter streamWriter;

		public IRC (string nick, int port, string host)
		{
			Host = host;
			Port = port;
			Nick = nick;
			Username = nick;
			Ident = nick;
		}

		public void Join(string channel)
		{
			SendData("JOIN " + channel);
		}

		public string ReadLine()
		{
			if (!Connected)
			{
				return null;
			}
			return streamReader.ReadLine();
		}

		public void Connect()
		{
			Connected = true;
			ping = new Thread(Ping);
			ns = new System.Net.Sockets.TcpClient(Host, Port).GetStream();
			streamReader = new StreamReader(ns);
			streamWriter = new StreamWriter(ns);

			SendData("USER " + Username + " 8 * :" + Ident);
			SendData("NICK " + Nick);

			ping.Start();
		}

		public void SendData(string data)
		{
			streamWriter.WriteLine(data);
			streamWriter.Flush();
		}

		private void Ping()
		{
			try
			{
				while (Connected)
				{
					SendData("PING :" + Host);
					Thread.Sleep(20000);
				}
			}
			catch (ThreadAbortException)
			{
				return;
			}
			catch (Exception fail)
			{
				Console.WriteLine(fail);
			}
		}
	}
}

