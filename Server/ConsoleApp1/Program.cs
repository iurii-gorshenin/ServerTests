// Do we really need all of these references?
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading.Tasks;

namespace Server
{
    //Always worth to put <summary> comments here
    class Program
    {
        static int port = 8888;
        static string ipAddress = "127.0.0.1"; 
        static List<Socket> clients = new List<Socket>();
        static List<string> chatMsg = new List<string>();
        // why do we need this?
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        private delegate bool EventHandler();
        static EventHandler _handler;
        
        //summary
        static void Main(string[] args)
        {
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
    
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listenSocket.Bind(ipPoint);

                //слушаем...
                listenSocket.Listen(100);
                Console.WriteLine("Waiting for clients...");

                int count = 0;
                while (true)
                {
                    Socket handler = listenSocket.Accept();
                    Console.WriteLine("client "+(count+=1)+" entered chatroom (one chat session statistics)");

                    var connectionData = new ConnectionData(handler, clients, chatMsg);
                    //вариант с созданием треда, а не tpl
                    //Thread userCtrl = new Thread(ClientThread);
                    //userCtrl.Start( );

                    Task.Run(()=> ClientThread(connectionData));
                    clients.Add(handler);
                    
                    string history = "";
                    if (chatMsg.Count != 0)
                    {
                        foreach (string str in chatMsg)
                        {
                            history += str + Environment.NewLine;
                        }                       
                        handler.Send(Encoding.Unicode.GetBytes(history));
                    }                        
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        
        //Summary
        static void ClientThread(object StateInfo)
        {
            // Sometimes vars are used, sometimes - type declaration
            // Try to be consistent in such things, use the same approach
            // (And in most cases it will be vars)
            var connectionData = (ConnectionData)StateInfo;
            var handler = connectionData.Handler;
            int bytes = 0;
            byte[] data = new byte[256];
            string userName = "";
            while (SocketConnected(handler))
            {
                StringBuilder builder = new StringBuilder();
                try
                {                 
                    do
                    {
                        bytes = handler.Receive(data);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (handler.Available > 0); //Can anything go wrong here?
                    
                    if(builder.ToString()!="")
                    {
                        string chatMessage = "";
                        if (userName == "")
                        {
                            userName = builder.ToString();
                            chatMessage = DateTime.Now.ToShortTimeString() + " " + userName + " connected";
                            Console.WriteLine(DateTime.Now.ToShortTimeString() + " " + userName + " connected");
                        }
                        else
                        {
                            chatMessage = DateTime.Now.ToShortTimeString() + " " + userName + ": " + builder.ToString();
                            Console.WriteLine(chatMessage);
                        }

                        Task.Run(() =>
                        {
                            foreach (var client in connectionData.Clients)
                            {
                                client.Send(Encoding.Unicode.GetBytes(chatMessage));
                            }
                        });
                        
                        lock (ConnectionData.lockerObj)
                        {
                            chatMsg.Add(chatMessage);
                        }
                    }                                     
                }
                catch
                {
                    break;
                }

            }
            Console.WriteLine("client "+ userName + " was disconnected");
            lock (ConnectionData.lockerObjCli)
            {
                connectionData.Clients.Remove(handler);
            }
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
        
        //Summary comments
        static bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }
        
        //Summary comments
        private static bool Handler()
        {
            Stop();
            return false;
        }
        
        //Summary comments
        public static void Stop()
        {
            foreach(var cli in clients)
            {
                cli.Send(Encoding.Unicode.GetBytes("Server close all connections, goodbye!"));
                cli.Shutdown(SocketShutdown.Both);
                cli.Close();
            }           
        }
    }
    
    //Extract class into separate file, put summary comments over properties and class itself
    class ConnectionData
    {
        public Socket Handler { get; set; }
        public List<Socket> Clients { get; set; }
        public List<string> ChatMsg { get; set; }
        public static object lockerObj = new object();
        public static object lockerObjCli = new object();
        public ConnectionData(Socket socket, List<Socket> sockets, List<string> chatMsg)
        {
            Handler = socket;
            Clients = sockets;
            ChatMsg = chatMsg;
        }
    }
}
