using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MCMTRS.Protocal578;

namespace MCMTRS {
    class Server {
        TcpListener listner;
        int port, maxPlayers;
        bool running;
        List<Task<Client>> clients;

        static void Main(string[] args) {
            new Server(args);
        }

        public Server(string[] args) {
            ConsoleErrorWriterDecorator.SetToConsole();
            maxPlayers = 10;
            port = 25565;
            running = true;
            for(int i = 0; i < args.Length; i++)
                switch(args[i]) {
                    case "-port":
                        port = int.Parse(args[i++]);
                        break;
                    case "-maxplayers":
                        maxPlayers = int.Parse(args[i++]);
                        break;
                }
            clients = new List<Task<Client>>();
            listner = new TcpListener(IPAddress.Any, port);
            Console.WriteLine("Server Starting");
            listner.Start();
            while(running) {
                if(listner.Pending()) {
                    if(clients.Count < maxPlayers) {
                        Task<Client> client = new Task<Client>(() => new Client(listner.AcceptTcpClient()));
                        client.Start();
                        clients.Add(client);
                    }
                }
                clients.RemoveAll(x => x.IsCompleted);
            }
            clients.ForEach(x => x.Result.Close());
            listner.Stop();
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
