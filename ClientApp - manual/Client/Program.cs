using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static int port = 8888; 
        static string address = "127.0.0.1";
        static string fileName = Guid.NewGuid().ToString() + ".txt";
        static List<string> names = new List<string>() { "Glen", "Max", "Tim", "John", "Andrew" };
        static List<string> answers = new List<string>() 
        { 
            "Hello!", "How are you?", "What's it?", "Fine, thank you!", "Glad to see you.", "I am ok and you?", "Oh, it is a good place",
            "Let's play!"
        };
        static Random rnd = new Random();
        static void Main(string[] args)
        {
            //while(true)
            //{
                bool sendName = false;
                bool needChat = true;
                try
                {
                    IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), port);

                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(ipPoint);

                    //реализация для треда, а не tpl
                    //var writerThread = new Thread(writerActions);
                    //writerThread.Start(socket);
                    Task.Run(()=>writerActions(socket));

                    int times = rnd.Next(1, 5);

                    while (SocketConnected(socket))// && needChat)
                    {
                        string message = "";
                        if (sendName)
                        {
                            Console.Write("Enter your message:"); //закомментить для работы в автоматическом режиме
                            message = Console.ReadLine(); //закомментить для работы в автоматическом режиме
                            //message = answers[rnd.Next(0, answers.Count)];
                            //Thread.Sleep(rnd.Next(0, 5000));
                        }
                        else
                        {
                            Console.Write("Enter your name:"); //закомментить для работы в автоматическом режиме
                            message = Console.ReadLine(); //закомментить для работы в автоматическом режиме
                            //message = names[rnd.Next(0, names.Count)];
                            sendName = true;
                        }
                        if (SocketConnected(socket) && message!="")
                        {
                            byte[] data = Encoding.Unicode.GetBytes(message);
                            socket.Send(data);
                        }

                        //if (times < 1)
                        //{
                        //    needChat = false;
                        //    Console.WriteLine("Client ended his task");
                        //}
                        //times = times - 1;
                    }

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (needChat)
                {
                    Console.WriteLine("Server closed all connections...");
                    //break;
                }
            //}
            Console.Read();
        }
        static void writerActions(object obj)
        {
            var socket = (Socket)obj;
            while(SocketConnected(socket))
            {
                try
                {
                    byte[] data = new byte[256];
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;

                    do
                    {
                        bytes = socket.Receive(data, data.Length, 0);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (socket.Available > 0);

                    using (StreamWriter sw = File.AppendText("C:\\TEMP\\" + fileName))
                    {
                        sw.WriteLine(builder.ToString());
                    }
                }
                catch
                {
                    break;
                }
            }

        }
        static bool SocketConnected(Socket s)
        {
            try
            {
                bool part1 = s.Poll(1000, SelectMode.SelectRead);
                bool part2 = (s.Available == 0);
                if (part1 && part2)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
            
        }

    }
}