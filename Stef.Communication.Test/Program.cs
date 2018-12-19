using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Stef.Communication.ByteImpl;
using Stef.Communication.DuplexImpl;
using Stef.Communication.EventImpl;
using Stef.Communication.FileImpl;

namespace Stef.Communication.Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //InitByteServer();
            //InitEventServer();
            //InitDuplexServer();
            InitFileServer();
        }

        public static void InitByteServer()
        {
            var server = new ByteServer();

            server.Connected += (s, a) =>
            {
                Console.WriteLine("SERVER: Connected");
            };
            server.Disconnected += (s, a) =>
            {
                Console.WriteLine("SERVER: Disconnected");
            };
            server.DataReceived += (s, a) =>
            {
                Console.WriteLine(string.Concat("CLIENT: ", Encoding.UTF8.GetString(a.Data)));
            };

            var client = new ByteClient();
            client.Connected += (s, a) =>
            {
                Console.WriteLine("CLIENT: Connected");
            };
            client.Disconnected += (s, a) =>
            {
                Console.WriteLine("CLIENT: Disconnected");
            };
            client.DataReceived += (s, a) =>
            {
                Console.WriteLine(string.Concat("CLIENT: ", Encoding.UTF8.GetString(a.Data)));
            };

            while (true)
            {
                var val = Console.ReadLine();

                if (val == null || val.Length < 3)
                    continue;

                var key = val.Substring(0, 2);
                val = val.Substring(2);

                switch (key)
                {
                    case "s:":
                        {
                            switch (val)
                            {
                                case "start":
                                    server.Start();
                                    break;
                                case "stop":
                                    server.Stop();
                                    break;
                                default:
                                    server.SendData(Encoding.UTF8.GetBytes(val));
                                    break;
                            }
                        }
                        break;
                    case "c:":
                        switch (val)
                        {
                            case "start":
                                client.Connect(autoReconnectOnError: true);
                                break;
                            case "stop":
                                client.Disconnect();
                                break;
                            default:
                                client.SendData(Encoding.UTF8.GetBytes(val));
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        public static void InitEventServer()
        {
            var eventServer = new EventServer();
            eventServer.Start();

            var eventClient1 = new EventClient();
            eventClient1.Connect();
            eventClient1.PublishEvent += (s, a) =>
            {
                Console.WriteLine("CLIENT1: " + a.Arguments.ToString());
            };

            var eventClient2 = new EventClient();
            eventClient2.Connect();
            eventClient2.PublishEvent += (s, a) =>
            {
                Console.WriteLine("CLIENT2: " + a.Arguments.ToString());
            };

            while (true)
            {
                eventClient1.SendEvent(new CustomEventData()
                {
                    FirstName = "A",
                    LastName = "B"
                });
                eventClient1.SendEvent(new CustomEventData()
                {
                    FirstName = "C",
                    LastName = "D"
                });

                Console.WriteLine("Message sent");
                Console.ReadLine();
            }
        }
        public static void InitDuplexServer()
        {
            var server = new DuplexServer();
            server.RegisterMessageType<CustomEventData, object>(e =>
            {
                Console.WriteLine(string.Concat("Server: ", e.FirstName));
                return "OK from Server";
            });
            server.RegisterMessageType<string, object>(e =>
            {
                Console.WriteLine(string.Concat("Server: ", e));
                return "OK from Server";
            });
            server.Start();

            var client = new DuplexClient();
            client.RegisterMessageType<CustomEventData, object>(e =>
            {
                Console.WriteLine(string.Concat("Client: ", e.FirstName));
                return "OK from Client";
            });
            client.RegisterMessageType<string, object>(e =>
            {
                Console.WriteLine(string.Concat("Client: ", e));
                return "OK from Client";
            });
            client.Connect();

            while (true)
            {
                var watch = new Stopwatch();
                watch.Start();

                var r1 = client.Send<CustomEventData, object>(new CustomEventData()
                {
                    FirstName = "Stefan",
                    LastName = "Heim",
                    Data = File.ReadAllBytes(@"c:\temp\Aktueller Mandant.pdf")
                });
                Console.WriteLine(string.Concat("Server response: ", r1));

                var r3 = client.Send<string, object>("Das ist ein Test");
                Console.WriteLine(string.Concat("Server response: ", r3));

                var r2 = server.Send<CustomEventData, object>(new CustomEventData()
                {
                    FirstName = "Stefan",
                    LastName = "Heim",
                    Data = File.ReadAllBytes(@"c:\temp\Aktueller Mandant.pdf")
                });
                Console.WriteLine(string.Concat("Client response: ", r2));

                var r4 = server.Send<string, object>("Das ist ein Test");
                Console.WriteLine(string.Concat("Server response: ", r4));

                watch.Stop();
                Console.WriteLine(string.Concat(watch.ElapsedMilliseconds, "ms"));

                Console.ReadLine();
            }
        }

        public static void InitFileServer()
        {
            var server = new FileServer();
            server.EvalFile += (s, a) =>
            {
                if (!File.Exists(a.Key))
                    return;

                a.Data = File.ReadAllBytes(a.Key);
            };
            server.SaveFile += (s, a) =>
            {
                File.WriteAllBytes(a.Key, a.Data);
            };
            server.Start();

            ExecuteMultipleActions();
            Console.ReadLine();
        }
        private static void ExecuteMultipleActions()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var taskList = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                taskList.Add(Task.Factory.StartNew(() =>
                {
                    var client = new FileClient();
                    client.Connect();

                    for (int x = 0; x < 50; x++)
                    {
                        RequestFile(client);
                        SaveFile(client);
                    }
                }));
            }

            Task.WaitAll(taskList.ToArray());
            stopwatch.Stop();

            Console.WriteLine(string.Concat("Total ", stopwatch.ElapsedMilliseconds, "ms"));
        }
        private static void RequestFile(FileClient client)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var fileName = @"c:\temp\Aktueller Mandant.pdf";
            var data = client.GetFile(fileName);
            //var fileData = client.GetFile(@"c:\temp\test.bmp");

            stopwatch.Stop();
            Console.WriteLine(string.Concat(stopwatch.ElapsedMilliseconds, "ms"));

            if (data != null)
            {
                File.WriteAllBytes(
                    @"c:\temp\c\" + Guid.NewGuid().ToString() + Path.GetExtension(fileName),
                    data);

                Console.WriteLine(string.Concat(data.Length, " bytes returned"));
            }
            else
            {
                Console.WriteLine("no file returned");
            }
            
        }
        private static void SaveFile(FileClient client)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var fileName = @"c:\temp\Aktueller Mandant.pdf";

            client.SaveFile(
                @"c:\temp\c\" + Guid.NewGuid().ToString() + Path.GetExtension(fileName),
                File.ReadAllBytes(fileName));

            stopwatch.Stop();
            Console.WriteLine(string.Concat(stopwatch.ElapsedMilliseconds, "ms"));
        }
    }
}
