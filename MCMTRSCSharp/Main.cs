using System.Net;
using System.Net.Sockets;
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
            port = 25565;
            running = true;
            for(int i = 0; i < args.Length; i++)
                switch(args[i]) {
                    case "-port":
                        port = int.Parse(args[i++]);
                        break;
                }
            listner = new TcpListener(IPAddress.Any, port);
            listner.Start();
            while(running)
                while(listner.Pending())
                    new Task(() => new Client(listner.AcceptTcpClient())).Start();
            listner.Stop();
        }
    }
}
