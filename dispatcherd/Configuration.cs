using System;

namespace dispatcherd
{
	public class Configuration
	{
		public class Network
		{
			public static int Port = 29438;
			public static bool TrafficDump = false;
		}

		public class System
		{
			public static int Verbosity = 0;
		}

		public const string DB = "database.xml";
		public static string Version = "1.0.0.0";
	}
}

