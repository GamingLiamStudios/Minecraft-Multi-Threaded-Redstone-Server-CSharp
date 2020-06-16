using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MCMTRS.Protocal578;

namespace MCMTRS {
    class Listener {
        TcpListener listner;
        int port;
        bool running;

        static void Main(string[] args) {
            new Listener(args);
        }

        public Listener(string[] args) {
            ConsoleErrorWriterDecorator.SetToConsole();
            port = 25565;
            running = true;
            for(int i = 0; i < args.Length; i++)
                switch(args[i]) {
                    case "-port":
                        port = int.Parse(args[i++]);
                        break;
                }
            listner = new TcpListener(IPAddress.Any, port);
            Console.WriteLine("Server Starting");
            listner.Start();
            while(running)
                if(listner.Pending())
                    new Task(() => new Client(listner.AcceptTcpClient())).Start();
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
