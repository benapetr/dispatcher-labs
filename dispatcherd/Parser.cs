using System;

namespace dispatcherd
{
	public class Parser
	{
		private static void Help()
		{
			Console.WriteLine("Bot dispatcher daemon\n\n" +
			                  "Parameters:\n" +
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
					case "--help":
						Help();
						return false;
					}
				}
			}
			return true;
		}
	}
}

