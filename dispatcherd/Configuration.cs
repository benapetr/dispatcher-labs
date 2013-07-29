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
using System.Xml;

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

