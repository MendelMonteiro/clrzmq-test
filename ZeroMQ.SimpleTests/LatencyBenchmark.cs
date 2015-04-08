namespace ZeroMQ.SimpleTests
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    internal class LatencyBenchmark : ITest
    {
        private const int RoundtripCount = 10000;

        private static readonly int[] MessageSizes = { 8, 64, 512, 4096, 8192, 16384, 32768 };

        public string TestName
        {
            get { return "Latency Benchmark"; }
        }

        public void RunTest()
        {
            var client = new Thread(ClientThread);
            var server = new Thread(ServerThread);

            client.Name = "Client";
            server.Name = "Server";

            server.Start();
            client.Start();

            server.Join(5000);
            client.Join(5000);
        }

        private static void ClientThread()
        {
            using (var context = ZContext.Create())
            using (var socket = ZSocket.Create(context, ZSocketType.REQ))
            {
                socket.Connect("tcp://localhost:9000");

                foreach (int messageSize in MessageSizes)
                {
                    var msg = new byte[messageSize];
                    var reply = new byte[messageSize];

                    var watch = new Stopwatch();
                    watch.Start();

                    for (int i = 0; i < RoundtripCount; i++)
                    {
                        ZError error;
                        var sendStatus = socket.SendBytes(msg, 0, msg.Length, ZSocketFlags.None, out error);

                        Debug.Assert(sendStatus, "Message was not indicated as sent.");

                        var bytesReceived = socket.ReceiveBytes(reply, 0, reply.Length, ZSocketFlags.None, out error);

                        Debug.Assert(bytesReceived, "Pong message did not have the expected size.");
                    }

                    watch.Stop();
                    long elapsedTime = watch.ElapsedTicks;

                    Console.WriteLine("Message size: " + messageSize + " [B]");
                    Console.WriteLine("Roundtrips: " + RoundtripCount);

                    double latency = (double)elapsedTime / RoundtripCount / 2 * 1000000 / Stopwatch.Frequency;
                    Console.WriteLine("Your average latency is {0} [us]", latency.ToString("f2"));
                }
            }
        }

        private static void ServerThread()
        {
            using (var context = ZContext.Create())
            using (var socket = ZSocket.Create(context, ZSocketType.REP))
            {
                socket.Bind("tcp://*:9000");

                foreach (int messageSize in MessageSizes)
                {
                    var message = new byte[messageSize];

                    for (int i = 0; i < RoundtripCount; i++)
                    {
                        ZError error;
                        var receivedBytes = socket.ReceiveBytes(message, 0, message.Length, ZSocketFlags.None, out error);

                        Debug.Assert(receivedBytes, "Ping message length did not match expected value.");

                        var sendStatus = socket.Send(message, 0, message.Length, ZSocketFlags.None, out error);

                        Debug.Assert(sendStatus, "Message was not indicated as sent.");
                    }
                }
            }
        }
    }
}
