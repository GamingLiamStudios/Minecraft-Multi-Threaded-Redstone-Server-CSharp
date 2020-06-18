using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MCMTRS.Protocal578;

namespace MCMTRS {
    sealed class Pool {
        private static readonly Lazy<Pool> lazy = new Lazy<Pool>(() => new Pool());

        public List<Client> clients = new List<Client>();
        public int port = 25565, maxPlayers = 10, clientsOnline = 0, connected = 0;
        public bool OnlineMode = true;

        public static Pool Instance {
            get {
                return lazy.Value;
            }
        }

        private Pool() {
        }
    }

    class Server {
        TcpListener listener;
        bool running;

        static void Main(string[] args) {
            new Server(args);
        }

        public Server(string[] args) {
            ConsoleErrorWriterDecorator.SetToConsole();
            running = true;
            for(int i = 0; i < args.Length; i++)
                switch(args[i]) {
                    case "-port":
                        Pool.Instance.port = int.Parse(args[i++]);
                        break;
                    case "-maxplayers":
                        Pool.Instance.maxPlayers = int.Parse(args[i++]);
                        break;
                    case "-offlinemode":
                        Pool.Instance.OnlineMode = false;
                        break;
                }
            listener = new TcpListener(IPAddress.Any, Pool.Instance.port);
            Console.WriteLine("Server Started");
            listener.Start();

            while(running) {
                if(listener.Pending()) {
                    ThreadPool.QueueUserWorkItem((object state) => {
                        TcpClient clt = listener.AcceptTcpClient();
                        Client player = new Client();
                        Pool.Instance.clients.Add(player);
                        Pool.Instance.clientsOnline++;
                        player.Start(clt);
                        Pool.Instance.clientsOnline--;
                        Pool.Instance.clients.Remove(player);
                    });
                } else {
                    Thread.Sleep(100);
                }
            }
            listener.Stop();
        }
    }

    public class ConsoleErrorWriterDecorator : TextWriter {
        private TextWriter m_OriginalConsoleStream;

        public ConsoleErrorWriterDecorator(TextWriter consoleTextWriter) {
            m_OriginalConsoleStream = consoleTextWriter;
        }

        public override void WriteLine(string value) {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            m_OriginalConsoleStream.WriteLine(value);

            Console.ForegroundColor = originalColor;
        }

        public override Encoding Encoding {
            get {
                return Encoding.Default;
            }
        }

        public static void SetToConsole() {
            Console.SetError(new ConsoleErrorWriterDecorator(Console.Error));
        }
    }
}
