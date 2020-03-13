using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ReadFileCustom {
    class Server {
        private static List<Socket> clientSockets;
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        private static Thread _socketThread;
        private static Socket handler;
        private static Socket listener;
        private static StateObject state = new StateObject();
        private static int socket_port = 0;

        public static void Start(int port) {
            socket_port = port;
            _socketThread = new Thread(SocketThreadFunc);
            _socketThread.Start();
            clientSockets = new List<Socket>();
            Console.WriteLine("Server started, waiting for a connection");
        }

        private static void SocketThreadFunc(object state) {
            try {
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ipAddress = IPAddress.Any;
                listener.Bind(new IPEndPoint(ipAddress, socket_port));
                
                listener.Listen(100);

                while (true) {
                    allDone.Reset();
                    listener.BeginAccept(AcceptCallback, listener);
                    allDone.WaitOne();
                }
            }
            catch (Exception ex) {
                Util.Log(ex.ToString());
            }
        }

        private static void AcceptCallback(IAsyncResult ar) {
            try {
                allDone.Set();
                listener = (Socket)ar.AsyncState;
                handler = listener.EndAccept(ar);
                state.workSocket = handler;
                clientSockets.Add(handler);
                Console.WriteLine("Client connected {0}", handler.RemoteEndPoint);
            }
            catch (Exception ex) {
                Util.Log(ex.ToString());
            }
        }

        public static void Message(String message) {
            try {
                byte[] msg = Encoding.ASCII.GetBytes(message);

                foreach (Socket socket in clientSockets) {
                    Console.WriteLine("Client listed {0}", socket.RemoteEndPoint);

                    if (SocketConnected(socket)) {
                        socket.Send(msg);
                        Console.WriteLine("Send client {0}, msg {1}", handler.RemoteEndPoint, message);
                    }
                }

                Console.WriteLine(Environment.NewLine);
            }
            catch (Exception ex) {
                Util.Log(ex.ToString());
            }
        }

        private static bool SocketConnected(Socket s) {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);

            if (part1 && part2)
                return false;
            else
                return true;
        }

        public static void CloseAll() {
            handler.Close();
        }
    }

    public class StateObject {
        public Socket workSocket = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
    }
}
