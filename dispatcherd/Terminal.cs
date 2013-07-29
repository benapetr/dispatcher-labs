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
using System.Threading;

namespace dispatcherd
{
    /// <summary>
    /// Listener for TCP
    /// </summary>
    public class Terminal
    {
        /// <summary>
        /// Number of currently connected sessions
        /// </summary>
        public static uint Connections = 0;
        /// <summary>
        /// Thread in which this thing run
        /// </summary>
        private static Thread thread = null;

        /// <summary>
        /// Start it
        /// </summary>
        public static void Init()
        {
            thread = new Thread(exec);
            thread.Name = "terminal";
            thread.Start();
            Core.DebugLog("Started tr");
        }

        /// <summary>
        /// Handle a new connection
        /// </summary>
        /// <param name="data"></param>
        private static void Client(object data)
        {
            Connections++;
            try
            {
                Connection connection = new Connection((System.Net.Sockets.TcpClient)data);
                connection.Exec();
            }
            catch (Exception fail)
            {
                Console.WriteLine(fail.ToString());
            }
            Connections--;
        }

        /// <summary>
        /// Run a listener
        /// </summary>
        public static void exec()
        {
            System.Net.Sockets.TcpListener server = new System.Net.Sockets.TcpListener(IPAddress.Any, Configuration.Network.Port);
            server.Start();
            while (Core.IsRunning)
            {
                System.Net.Sockets.TcpClient connection = server.AcceptTcpClient();
                Thread _client = new Thread(Client);
                _client.Start(connection);
            }
        }
    }
}

