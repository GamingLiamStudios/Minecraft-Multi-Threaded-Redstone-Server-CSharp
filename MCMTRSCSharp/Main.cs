using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using MCMTRS.Protocal578;

namespace MCMTRS {
    sealed class Pool {
        private static readonly Lazy<Pool> lazy = new Lazy<Pool>(() => new Pool());

        public List<int> players;
        public List<IEntity> entities;
        public byte[] seedHash;
        public List<Client> clients;
        public JsonElement properties;

        public static Pool Instance {
            get {
                return lazy.Value;
            }
        }

        private Pool() {
            clients = new List<Client>();
            players = new List<int>();
            entities = new List<IEntity>();
        }
    }

    class Server {
        TcpListener listener;
        bool running;
        string propertiesPath;

        static void Main(string[] args) {
            new Server(args);
        }

        public Server(string[] args) {
            //Server Init
            ConsoleErrorWriterDecorator.SetToConsole();
            ConsoleOutWriter.SetToConsole();
            running = true;
            propertiesPath = Directory.GetCurrentDirectory() + "\\server.properties";
            if(!File.Exists(propertiesPath))
                CreatePropertiesFile(propertiesPath);
            ReadPropertiesFile(propertiesPath);
            var ip = Pool.Instance.properties.GetProperty("server-ip").GetString();
            listener = new TcpListener(ip!=""?IPAddress.Parse(ip):IPAddress.Any, Pool.Instance.properties.GetProperty("server-port")
                .GetInt32());
            Pool.Instance.seedHash = SHA256.Create().ComputeHash(BitConverter.GetBytes(Pool.Instance.properties.GetProperty("level-seed")
                .GetInt64()));

            //Listen For Clients
            Console.WriteLine("Server Started");
            listener.Start();
            while(running) {
                if(listener.Pending()) {
                    ThreadPool.QueueUserWorkItem((object state) => {
                        TcpClient clt = listener.AcceptTcpClient();
                        Client client = new Client();
                        int clientID = client.clientID;
                        Pool.Instance.clients.Add(client);
                        client.Start(clt);
                        Pool.Instance.clients.RemoveAt(Pool.Instance.clients.FindIndex(c => c.clientID == clientID));

                    });
                } else {
                    Thread.Sleep(100);
                }
            }

            //Clean Up
            listener.Stop();
        }

        public void CloseAll() {
            running = false;
            Pool.Instance.entities.FindAll(e => Pool.Instance.players.Contains(e.ID)).ForEach(e => {
                var player = (Player)e; player.isConnected = false;
            });
        }

        public void CreatePropertiesFile(string path) {
            var seed = new byte[8];
            new Random().NextBytes(seed);
            StreamWriter writer = File.CreateText(path);
            writer.WriteLine("#Minecraft server properties");
            writer.WriteLine("#({0})", DateTime.Now.ToString());
            writer.Write("spawn-protection=16\nmax-tick-time=60000\nquery.port=25565\ngenerator-settings=\nsync-chunk-writes=true" +
                "\nforce-gamemode=false\nallow-nether=true\nenforce-whitelist=false\ngamemode=survival\nbroadcast-console-to-ops=true" +
                "\nenable-query=false\nplayer-idle-timeout=0\ndifficulty=easy\nbroadcast-rcon-to-ops=true\nspawn-monsters=true" +
                "\nop-permission-level=4\npvp=true\nsnooper-enabled=true\nlevel-type=default\nhardcore=false" +
                "\nenable-command-block=false\nnetwork-compression-threshold=256\nmax-players=20\nmax-world-size=29999984" +
                "\nresource-pack-sha1=\nfunction-permission-level=2\nrcon.port=25575\nserver-port=25565\nserver-ip=\nspawn-npcs=true" +
                "\nallow-flight=false\nlevel-name=world\nview-distance=10\nresource-pack=\nspawn-animals=true\nwhite-list=false" +
                "\nrcon.password=\ngenerate-structures=true\nonline-mode=true\nmax-build-height=256\nlevel-seed=" + 
                 BitConverter.ToInt64(seed) + "\nprevent-proxy-connections=false\nuse-native-transport=true" +
                "\nmotd=A Minecraft Server\nenable-rcon=false");
            writer.Flush();
            writer.Close();
            writer.Dispose();
        }

        public void ReadPropertiesFile(string path) {
            StreamReader reader = File.OpenText(path);
            var options = new JsonWriterOptions {
                Indented = true
            };
            var stream = new MemoryStream();
            using(var writer = new Utf8JsonWriter(stream, options)) {
                writer.WriteStartObject();
                string line;
                while((line = reader.ReadLine()) != null) {
                    if(!line.StartsWith("#")) {
                        var split = line.Split("=");
                        if(split[1] == "")
                            writer.WriteString(split[0], split[1]);
                        else if(char.IsDigit(split[1].First()))
                            writer.WriteNumber(split[0], long.Parse(split[1]));
                        else if(split[1].Equals("true") || split[1].Equals("false"))
                            writer.WriteBoolean(split[0], split[1].Equals("true"));
                        else
                            writer.WriteString(split[0], split[1]);
                    }
                }
                writer.WriteEndObject();
            }
            Pool.Instance.properties = JsonDocument.Parse(Encoding.UTF8.GetString(stream.ToArray())).RootElement;
            stream.Close();
            reader.Close();
            reader.Dispose();
        }
    }

    public class ConsoleOutWriter : TextWriter {
        private TextWriter m_OriginalConsoleStream;

        public ConsoleOutWriter(TextWriter consoleTextWriter) {
            m_OriginalConsoleStream = consoleTextWriter;
        }

        public override void WriteLine(string value) {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            m_OriginalConsoleStream.Write("[{0}] ", DateTime.Now.ToString());
            Console.ForegroundColor = originalColor;
            m_OriginalConsoleStream.WriteLine(value);
        }

        public override void Write(string value) {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = originalColor;
            m_OriginalConsoleStream.Write(value);
        }

        public override Encoding Encoding {
            get {
                return Encoding.Default;
            }
        }

        public static void SetToConsole() {
            Console.SetOut(new ConsoleOutWriter(Console.Out));
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
            m_OriginalConsoleStream.Write("[{0}] {1}", DateTime.Now.ToString(), value);
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
