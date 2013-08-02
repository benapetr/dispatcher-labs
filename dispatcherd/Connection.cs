//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.

using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace dispatcherd
{
    public class Connection
    {
        /// <summary>
        /// Network stream
        /// </summary>
        public NetworkStream ns;
        /// <summary>
        /// Stream reader
        /// </summary>
        private StreamReader streamReader;
        private StreamWriter streamWriter;
        public bool IsConnected = true;
        /// <summary>
        /// If current user is authenticated to subscription
        /// </summary>
        public bool IsAuthenticated
        {
            get
            {
                if (subscription != null)
                {
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// This is a subscription they are authenticated to :)
        /// </summary>
        public Subscription subscription = null;

        public Connection(TcpClient client)
        {
            ns = client.GetStream();
            streamWriter = new StreamWriter(ns);
            streamReader = new StreamReader(ns);
        }

        public void Send(string text)
        {
            streamWriter.WriteLine(text);
            streamWriter.Flush();
        }

        private bool Subscribe(string line, List<string> parameters)
        {
            // check if name is correct
            if (parameters.Count < 1)
            {
                Send("E001: Invalid name");
                return false;
            }
            Subscription feed;
            if (Core.DB.ContainsKey(parameters[0]))
            {
                Send("E002: This subscription exist");
                return false;
            }
            feed = new Subscription(parameters[0]);
            feed.GenerateToken();
            lock (Core.DB)
            {
                if (Core.DB.ContainsKey(parameters[0]))
                {
                    Send("E002: This subscription exist");
                    return false;
                }
                Core.DB.Add(parameters[0], feed);
                Send("TOKEN: " + feed.token);
                Core.Save();
            }
            return true;
        }

        private bool Unsubscribe(string line, List<string> parameters)
        {
            if (parameters.Count < 2)
            {
                if (IsAuthenticated)
                {
                    lock (Core.DB)
                    {
                        if (!Core.DB.ContainsKey(subscription.Name))
                        {
                            Send("E007: No such feed");
                            return false;
                        }
                        Core.DB.Remove(subscription.Name);
                    }
                    subscription = null;
                    Send("OK");
                    return true;
                }
            }
            else
            {
                Subscription f2 = Subscription.login(parameters[0], parameters[1]);
                if (f2 != null)
                {
                    lock (Core.DB)
                    {
                        if (!Core.DB.ContainsKey(f2.Name))
                        {
                            Send("E007: No such feed");
                            return false;
                        }
                        Core.DB.Remove(f2.Name);
                    }
                    if (f2 == subscription)
                    {
                        subscription = null;
                    }
                    Send("OK");
                    return true;
                }
            }
            Send("E005: Invalid token or name");
            return false;
        }

        private bool Insert(string line, List<string> parameters)
        {
            if (!IsAuthenticated)
            {
                Send("E020: Authentication was not completed");
                return false;
            }
            if (parameters.Count < 1)
            {
                Send("E008: Missing parameter for insert");
                return false;
            }
            List<FeedItem> InsertData;
            StringBuilder sb = new StringBuilder("");
            switch (parameters[0])
            {
                case "xml":
                    while (!line.Contains("</items>"))
                    {
                        line = streamReader.ReadLine();
                        sb.Append(line + "\n");
                    }
                    InsertData = Subscription.String2List(sb.ToString());
                    if (InsertData == null)
                    {
                        Send("E060: Invalid xml");
                        return false;
                    }
                    Send(subscription.Insert(InsertData).ToString());
                    return true;
                case "json":
                    InsertData = Subscription.JSON2List(sb.ToString());
                    if (InsertData == null)
                    {
                        Send("E062: Invalid JSON");
                        return false;
                    }
                    Send(subscription.Insert(InsertData).ToString());
                    return true;
            }
            Send("E010: Unknown format of data");
            return false;
        }

        private bool Remove(string line, List<string> parameters)
        {
            if (!IsAuthenticated)
            {
                Send("E020: Authentication was not completed");
                return false;
            }
            if (parameters.Count < 1)
            {
                Send("E008: Missing parameter for delete");
                return false;
            }
            List<FeedItem> RemoveData;
            StringBuilder sb02 = new StringBuilder("");
            switch (parameters[0])
            {
                case "xml":
                    while (!line.Contains("</items>"))
                    {
                        sb02.Append(line + "\n");
                    }
                    RemoveData = Subscription.String2List(sb02.ToString());
                    if (RemoveData == null)
                    {
                        Send("E060: Invalid xml");
                        return false;
                    }
                    Send(subscription.Delete(RemoveData).ToString());
                    return true;
                case "json":
                    RemoveData = Subscription.JSON2List(sb02.ToString());
                    if (RemoveData == null)
                    {
                        Send("E062: Invalid json");
                        return false;
                    }
                    Send(subscription.Delete(RemoveData).ToString());
                    return true;
            }
            Send("E010-Unknown format of data");
            return false;
        }

        public void Exec()
        {
            while (IsConnected && !streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                string command = line;
                List<string> parameters = new List<string>();
                if (command.Contains(" "))
                {
                    command = command.Substring(0, command.IndexOf(" "));
                    parameters.AddRange(line.Substring(line.IndexOf(" ") + 1).Split(' '));
                }

                switch (command.ToLower())
                {
                    case "quit":
                        Send("Good bye :o");
                        ns.Close();
                        return;
                    case "subscribe":
                        Subscribe(line, parameters);
                        continue;
                    case "unsubscribe":
                        Unsubscribe(line, parameters);
                        continue;
                    case "auth":
                        if (parameters.Count == 2)
                        {
                            Subscription f1 = Subscription.login(parameters[0], parameters[1]);
                            if (f1 != null)
                            {
                                Send("OK");
                                subscription = f1;
                                continue;
                            }
                        }
                        Send("E005: Invalid token or name");
                        continue;
                    case "insert":
                        Insert(line, parameters);
                        continue;
                    case "remove":
                        Remove(line, parameters);
                        continue;
                    case "list":
                        if (!IsAuthenticated)
                        {
                            Send("E020: Authentication was not completed");
                            continue;
                        }
                        break;
                    case "format":
                        if (!IsAuthenticated)
                        {
                            Send("E020: Authentication was not completed");
                            continue;
                        }
                        if (parameters.Count > 0)
                        {
                            switch (parameters[0])
                            {
                                case "pipe":
                                    subscription.format = Subscription.Format.Pipe;
                                    Send("OK");
                                    continue;
                                case "xml":
                                    subscription.format = Subscription.Format.XML;
                                    Send("OK");
                                    continue;
                                case "json":
                                    subscription.format = Subscription.Format.JSON;
                                    Send("OK");
                                    continue;
                            }
                        }
                        Send("E012: Invalid format");
                        continue;
                    case "info":
                        Send("I have " + Terminal.Connections.ToString() + " connections and " + Core.DB.Count.ToString() + " subscriptions, I am now watching " + Core.WD.Count.ToString() + " wikis");
                        continue;
                    default:
                        Send("E006: Command not understood");
                        continue;
                }
            }
        }
    }
}

