using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;

namespace BandwidthTest
{
    class Program
    {
        static TcpListener mListener;
        static Thread ListenThread;

        static void Main(string[] args)
        {
            Console.WriteLine("Intellio TCP Bandwidth Tester - Server");
            Console.WriteLine("Usage BandwidthTestServer.exe [port]");
            Console.WriteLine();

            int port = (args.Length == 1) ? Int32.Parse(args[0]) : 9000;

            Console.WriteLine("Listening on port {0}", port);

            mListener = new TcpListener(IPAddress.Any, port);
            try
            {
                mListener.Start();
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot listen on port {0}", port);
                Environment.Exit(-1);
            }

            ListenThread = new Thread(new ThreadStart(ListenForClients));
            ListenThread.Start();
        }

        static void ListenForClients()
        {
            while (true)
            {
                try
                {
                    TcpClient client = mListener.AcceptTcpClient();

                    Thread thread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                    thread.IsBackground = true;
                    thread.Start(client);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: {0}", e.Message);
                }
            }
        }

        static void HandleClientComm(object pClient)
        {
            TcpClient client = (TcpClient)pClient;
            NetworkStream stream = client.GetStream();

            string remoteaddr = client.Client.RemoteEndPoint.ToString();

            Console.WriteLine("{0}: Connection established", remoteaddr);

            while (true)
            {
                try
                {
                    CommHeader header = new CommHeader();
                    header.ReadFromStream(stream);
                    switch (header.mType)
                    {
                        case CommHeader.TYPE_SERVER_TO_CLIENT:
                            SendTest(stream, remoteaddr, header.mSize);
                            break;
                        case CommHeader.TYPE_CLIENT_TO_SERVER:
                            ReceiveTest(stream, remoteaddr, header.mSize, false);
                            break;
                        case CommHeader.TYPE_DUPLEX:
                            ReceiveTest(stream, remoteaddr, header.mSize, true);
                            break;
                    }
                }
               catch (Exception e)
                {
                    break;
                }
            }

            Console.WriteLine("{0}: Connection closed", remoteaddr);
        }

        static void SendTest(Stream pStream, String pRemoteAddress, int pSize)
        {
            Console.WriteLine("{0}: Sending {1}MB", pRemoteAddress, pSize);

            const int size = 1024 * 1024;
            byte[] buffer = new byte[size];

            for (int i = 0; i < pSize; i++ )
                pStream.Write(buffer, 0, size);

            Console.WriteLine("{0}: {1}MB sent", pRemoteAddress, pSize);
        }

        static void ReceiveTest(Stream pStream, String pRemoteAddress, int pSize, bool pIsDuplexMode)
        {
            if (!pIsDuplexMode)
                Console.WriteLine("{0}: Receiving {1}MB", pRemoteAddress, pSize);
            else
                Console.WriteLine("{0}: Receiving and sending back {1}MB", pRemoteAddress, pSize);

            const int size = 1024 * 1024;
            byte[] buffer = new byte[size];
            int bytesreceived = 0;

            while (bytesreceived < pSize * 1024 * 1024)
            {
                int s = (pSize * 1024 * 1024) - bytesreceived > size ? size : (pSize * 1024 * 1024) - bytesreceived;
                int i = pStream.Read(buffer, 0, s);

//                Console.WriteLine("{0}, {1}, {2}, {3}", pSize, bytesreceived, i, s);

                if (i == 0)
                    throw new IOException("Error reading stream");

                bytesreceived += i;

                if (pIsDuplexMode)
                    pStream.Write(buffer, 0, i);
            }

            Console.WriteLine("{0}: {1}MB received", pRemoteAddress, pSize);
        }
    }
}
