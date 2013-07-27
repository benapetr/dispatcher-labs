//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.

using System;

namespace dispatcherd
{
	public class Parser
	{
		private static void Help()
		{
			Console.WriteLine("Bot dispatcher daemon\n\n" +
			                  "Parameters:\n" +
			                  "    -v | --verbose: Increase verbosity\n" +
			                  "    --help: Display this help\n\n" +
			                  "This project is open source licensed under GNU GPLv3");
		}

		public static bool Parse (string[] d)
		{
			foreach (string parameter in d)
			{
				if (parameter.StartsWith("--"))
				{
					switch(parameter)
					{
					case "--verbose":
						Configuration.System.Verbosity++;
						break;
					case "--help":
						Help();
						return false;
					}
					continue;
				}
				if (parameter.StartsWith("-"))
				{
					foreach (char x in parameter)
					{
						switch (x)
						{
						case 'v':
							Configuration.System.Verbosity++;
							break;
						}
					}
				}
			}
			return true;
		}
	}
}

