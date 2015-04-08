namespace ZeroMQ.SimpleTests
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;

    internal class HelloWorld : ITest
    {
        public string TestName
        {
            get { return "Hello World"; }
        }

        public void RunTest()
        {
            Console.WriteLine("ZeroMQ version: ");

            var client = new Thread(ClientThread);
            var server = new Thread(ServerThread);

            server.Start();
            client.Start();

            server.Join();
            client.Join();
        }

        private static void ClientThread()
        {
            Thread.Sleep(10);

            using (var context = ZContext.Create())
            using (var socket = ZSocket.Create(context, ZSocketType.REQ))
            {
                socket.Connect("tcp://localhost:8989");

                socket.SendFrame(new ZFrame(Encoding.UTF8.GetBytes("Hello")));

                var buffer = new byte[100];
                socket.ReceiveBytes(buffer, 0, buffer.Length);

                using (var stream = new MemoryStream(buffer, 0, buffer.Length))
                {
                    Console.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
                }
            }
        }

        private static void ServerThread()
        {
            using (var context = ZContext.Create())
            using (var socket = ZSocket.Create(context, ZSocketType.REP))
            {
                socket.Bind("tcp://*:8989");

                ZFrame request = socket.ReceiveFrame();
                Console.WriteLine(request.ReadString(Encoding.UTF8));

                socket.SendFrame(new ZFrame(Encoding.UTF8.GetBytes("World")));
            }
        }
    }
}
