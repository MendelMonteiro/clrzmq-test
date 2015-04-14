namespace ZeroMQ.SimpleTests
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    internal class ThroughputBenchmark : ITest
    {
        private static readonly int[] MessageSizes = { 8, 64, 256, 1024, 4096 };

        private const int MessageCount = 1000000;

        public string TestName
        {
            get { return "Throughput Benchmark"; }
        }

        public void RunTest()
        {
            var proxyPull = new Thread(ProxyPullThread);
            var proxyPush = new Thread(ProxyPushThread);

            GC.Collect(2);
            var collections = GC.CollectionCount(0);
            proxyPull.Start();
            proxyPush.Start();

            proxyPush.Join();
            proxyPull.Join();

            Console.WriteLine("Collections performed during test {0}", GC.CollectionCount(0) - collections);
        }

        private static void ProxyPullThread()
        {
            using (var context = ZContext.Create())
            using (var socket = ZSocket.Create(context, ZSocketType.PULL))
            {
                socket.Bind("tcp://*:9091");

                foreach (int messageSize in MessageSizes)
                {
                    var message = new byte[messageSize];

                    ZError error;
                    var receivedBytes = socket.ReceiveBytes(message, 0, message.Length, ZSocketFlags.None, out error);
                    Debug.Assert(receivedBytes, "Message length was different from expected size.");
                    Debug.Assert(message[messageSize / 2] == 0x42, "Message did not contain verification data.");

                    var watch = new Stopwatch();
                    watch.Start();

                    for (int i = 1; i < MessageCount; i++)
                    {
                        receivedBytes = socket.ReceiveBytes(message, 0, message.Length, ZSocketFlags.None, out error);
                        Debug.Assert(receivedBytes, "Message length was different from expected size.");
                        Debug.Assert(message[messageSize / 2] == 0x42, "Message did not contain verification data.");
                    }

                    watch.Stop();

                    long elapsedTime = watch.ElapsedTicks;
                    long messageThroughput = MessageCount * Stopwatch.Frequency / elapsedTime;
                    long megabitThroughput = messageThroughput * messageSize * 8 / 1000000;

                    Console.WriteLine("Message size: {0} [B]", messageSize);
                    Console.WriteLine("Average throughput: {0} [msg/s]", messageThroughput);
                    Console.WriteLine("Average throughput: {0} [Mb/s]", megabitThroughput);
                }
            }
        }

        private static void ProxyPushThread()
        {
            using (var context = ZContext.Create())
            using (var socket = ZSocket.Create(context, ZSocketType.PUSH))
            {
                socket.Connect("tcp://127.0.0.1:9091");

                foreach (int messageSize in MessageSizes)
                {
                    var msg = new byte[messageSize];
                    msg[messageSize / 2] = 0x42;

                    for (int i = 0; i < MessageCount; i++)
                    {
                        ZError error;
                        socket.SendBytes(msg, 0, msg.Length, ZSocketFlags.None, out error);
                    }
                }
            }
        }
    }
}
