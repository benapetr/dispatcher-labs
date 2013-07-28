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
using System.Threading;

namespace dispatcherd
{
	public class Writer
	{
		/// <summary>
		/// List of all data to write
		/// </summary>
		private static readonly List<STI> Data = new List<STI>();

		private static Thread thread;
		
		/// <summary>
		/// Whether storage is running
		/// </summary>
		public static bool isRunning = true;

		private static bool Write(STI item)
		{
			try
			{
				System.IO.File.AppendAllText(item.file, item.line + "\n");
				return true;
			}
			catch (Exception crashed)
			{
				Core.Log("Unable to write data into " + item.file + " skipping this");
				Console.WriteLine(crashed.ToString());
				return false;
			}
		}

		public static void Init()
		{
			thread = new Thread(Exec);
			thread.Name = "Writer";
			thread.Start();
		}

		/// <summary>
		/// Insert a line to storage writer
		/// </summary>
		/// <param name="File"></param>
		/// <param name="Text"></param>
		/// <param name="Delayed"></param>
		public static void InsertLine(string File, string Text, bool Delayed = true)
		{
				lock (Data)
				{
					Data.Add(new STI(Text, File, Delayed));
				}
		}
		
		private static void WriteData()
		{
			List<STI> jobs = new List<STI>();
			lock (Data)
			{
				jobs.AddRange(Data);
				Data.Clear();
			}
			foreach (STI item in jobs)
			{
				if (item.DelayedWrite)
				{
					while (!Write(item))
					{
						Core.Log("Unable to write data, delaying write");
						Thread.Sleep(6000);
					}
				}
				else
				{
					Write(item);
				}
			}
		}
		
		/// <summary>
		/// Thread
		/// </summary>
		public static void Exec()
		{
				Core.DebugLog("loaded writer thread");
				while (isRunning)
				{
					try
					{
						Thread.Sleep(2000);
						if (Data.Count > 0)
						{
							WriteData();
						}
					}
					catch (ThreadAbortException)
					{
						isRunning = false;
						break;
					}
				}
				if (Data.Count > 0)
				{
					Core.Log("KERNEL: Writer thread was requested to stop, but there is still some data to write");
					WriteData();
					Core.Log("No remaining data, stopping writer thread");
					return;
				}
				else
				{
					Core.Log("No remaining data, stopping writer thread");
					return;
				}
		}
	}
	
	/// <summary>
	/// Storage Item
	/// </summary>
	public class STI
	{
		/// <summary>
		/// Delayed write
		/// </summary>
		public bool DelayedWrite;
		/// <summary>
		/// Line
		/// </summary>
		public string line;
		/// <summary>
		/// File
		/// </summary>
		public string file;
		
		/// <summary>
		/// Creates a new instance of STI
		/// </summary>
		/// <param name="Line"></param>
		/// <param name="Name"></param>
		/// <param name="delayed"></param>
		public STI(string Line, string Name, bool delayed = true)
		{
			DelayedWrite = delayed;
			file = Name;
			line = Line;
		}
	}
}

