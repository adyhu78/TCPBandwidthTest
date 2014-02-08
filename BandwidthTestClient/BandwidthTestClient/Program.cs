using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BandwidthTest
{
    class Program
    {
        enum Mode {SEND, RECEIVE, BOTH, DUPLEX};

        static int mTotalSize;
        static Mode mMode;
        static long mStartTimestamp;

        static AutoResetEvent mReceiveFinished = new AutoResetEvent(false);

        static String GetModeName(Mode pMode)
        {
            switch (pMode)
            {
                case Mode.SEND:
                    return "Send";
                case Mode.RECEIVE:
                    return "Receive";
                case Mode.BOTH:
                    return "Both";
                case Mode.DUPLEX:
                    return "Duplex";
            }

            return "";
        }

        static Mode StringToMode(String pMode)
        {
            switch (pMode)
            {
                case "S":
                    return Mode.SEND;
                case "R":
                    return Mode.RECEIVE;
                case "B":
                    return Mode.BOTH;
                case "D":
                    return Mode.DUPLEX;
            }

            return Mode.BOTH;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Intellio TCP Bandwidth Tester - Client");
            Console.WriteLine("Usage BandwidthTestClient.exe address [Port] [Mode] [Size MB]");
            Console.WriteLine("  Mode: S - Send, R - Receive, B - Both, D - Duplex");
            Console.WriteLine();

            if (args.Length < 1)
            {
                Console.WriteLine("Missing parameter(s)");
                Environment.Exit(-1);
            }

            string address = args[0];
            int port = (args.Length >= 2) ? Int32.Parse(args[1]) : 9000;

            mMode = (args.Length >= 3) ? StringToMode(args[2]) : Mode.BOTH;
            mTotalSize = (args.Length >= 4) ? Int32.Parse(args[3]) : 100;

            Console.WriteLine("Mode: {0}", GetModeName(mMode));
            Console.WriteLine("Data size: {0}", mTotalSize);
            Console.WriteLine();

            Console.WriteLine("Connecting to host: {0}:{1}", address, port);

            TcpClient client = new TcpClient();
            try
            {
                client.Connect(address, port);
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot connect to the remote host");
                Environment.Exit(-1);
            }

            Console.WriteLine("Connection established");

            Thread thread = new Thread(new ParameterizedThreadStart(HandleClientComm));
            thread.IsBackground = true;
            thread.Start(client);

            NetworkStream stream = client.GetStream();

            switch (mMode)
            {
                case Mode.SEND:
                    SendTest(stream);
                    break;
                case Mode.RECEIVE:
                    ReceiveTest(stream);
                    break;
                case Mode.BOTH:
                    SendTest(stream);
                    ReceiveTest(stream);
                    break;
                case Mode.DUPLEX:
                    SendTest(stream);
                    break;
            }
        }

        static void SendTest(NetworkStream pStream)
        {
            int type = CommHeader.TYPE_CLIENT_TO_SERVER;
            if (mMode == Mode.DUPLEX)
                type = CommHeader.TYPE_DUPLEX;

            CommHeader header = new CommHeader(type, mTotalSize);

            try
            {
                header.WriteToStream(pStream);
            }
            catch (Exception)
            {
                Console.WriteLine("IO Exception while sending data");
                Environment.Exit(-1);
            }

            // Create data
            const int buffersize = 1024 * 1024;
            byte[] buffer = new byte[buffersize];

            for (int i = 0; i < buffersize; i++)
                buffer[i] = (byte) i;

            if (mMode != Mode.DUPLEX)
                Console.WriteLine("Transfer started (Send)");
            else
                Console.WriteLine("Transfer started (Duplex)");

            mStartTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            for (int i = 0; i < mTotalSize; i++)
            {
                try
                {
                    pStream.Write(buffer, 0, buffersize);
                }
                catch (Exception)
                {
                    Console.WriteLine("IO Exception while sending data");
                    Environment.Exit(-1);
                }
            }

            if (mMode == Mode.DUPLEX)
                mReceiveFinished.WaitOne();
           
            long f = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            double timeellapsed = (double)(f - mStartTimestamp) / 1000;
            Console.WriteLine("{0}ms, {1:0.00}Mbit/s", f - mStartTimestamp, mTotalSize * 8 / timeellapsed);
        }

        static void ReceiveTest(NetworkStream pStream)
        {
            CommHeader header = new CommHeader(CommHeader.TYPE_SERVER_TO_CLIENT, mTotalSize);

            Console.WriteLine("Transfer started (Receive)");

            mStartTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            try
            {
                header.WriteToStream(pStream);
            }
            catch (Exception)
            {
                Console.WriteLine("IO Exception while sending data");
                Environment.Exit(-1);
            }

            mReceiveFinished.WaitOne();

            long f = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            double timeellapsed = (double)(f - mStartTimestamp) / 1000;
            Console.WriteLine("{0}ms, {1:0.00}Mbit/s", f - mStartTimestamp, mTotalSize * 8 / timeellapsed);
        }

        static void HandleClientComm(object pClient)
        {
            TcpClient client = (TcpClient)pClient;
            NetworkStream stream = client.GetStream();

            const int buffersize = 1024 * 1024;
            byte[] buffer = new byte[buffersize];
            int bytesreceived;

            bytesreceived = 0;
            while (true)
            {
                try
                {
                    int i = stream.Read(buffer, 0, buffersize);

                    if (i == 0)
                        break;

                    bytesreceived += i;

                    if (bytesreceived == mTotalSize*1024*1024)
                    {
                        mReceiveFinished.Set();
                        bytesreceived = 0;
                    }
                }
                catch
                {
                    break;
                }             
            }
        }
    }
}
