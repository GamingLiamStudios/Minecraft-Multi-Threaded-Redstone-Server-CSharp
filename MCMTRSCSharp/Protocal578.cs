using fNbt;
using fNbt.Tags;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MCMTRS.Protocal578 {

    /*
     * Designed for Minecraft Version 1.15.2, Protocal 578 as specified by wiki.vg
     */

    #region DataTypes

    public class VariableNumbers {
        public static Tuple<int, byte> ReadVarInt(NetworkStream net) {
            byte numRead = 0;
            int result = 0;
            byte[] read = new byte[1];
            do {
                if(net.Read(read) == 0)
                    return new Tuple<int, byte>(-1, 0);
                int value = (read[0] & 0x7f);
                result |= (value << (7 * numRead));
                numRead++;
                if(numRead > 5)
                    throw new TypeLoadException("VarInt is too big");
            } while((read[0] & 0x80) != 0);
            return new Tuple<int, byte>(result, numRead);
        }

        public static Tuple<long, byte> ReadVarLong(NetworkStream net) {
            byte numRead = 0;
            long result = 0;
            byte[] read = new byte[1];
            do {
                if(net.Read(read) == 0)
                    return new Tuple<long, byte>(-1, 0);
                int value = (read[0] & 0x7f);
                result |= value << (7 * numRead);
                numRead++;
                if(numRead > 10)
                    throw new TypeLoadException("VarInt is too big");
            } while((read[0] & 0x80) != 0);
            return new Tuple<long, byte>(result, numRead);
        }

        public static byte[] CreateVarInt(int value) {
            uint shift = (uint)value;
            var stream = new MemoryStream();
            do {
                byte temp = (byte)(shift & 0b01111111);
                shift >>= 7;
                if(shift != 0)
                    temp |= 0b10000000;
                stream.WriteByte(temp);
            } while(shift != 0);
            return stream.ToArray();
        }

        public static byte[] CreateVarLong(long value) {
            var stream = new MemoryStream();
            do {
                byte temp = (byte)(value & 0b01111111);
                value >>= 7;
                if(value != 0)
                    temp |= 0b10000000;
                stream.WriteByte(temp);
            } while(value != 0);
            return stream.ToArray();
        }

        public static Tuple<int, byte> ReadVarInt(BinaryReader net) {
            byte numRead = 0;
            int result = 0;
            byte[] read = new byte[1];
            do {
                if(net.Read(read) == 0)
                    return new Tuple<int, byte>(-1, 0);
                int value = (read[0] & 0x7f);
                result |= (value << (7 * numRead));
                numRead++;
                if(numRead > 5)
                    throw new TypeLoadException("VarInt is too big");
            } while((read[0] & 0x80) != 0);
            return new Tuple<int, byte>(result, numRead);
        }

        public static Tuple<long, byte> ReadVarLong(BinaryReader net) {
            byte numRead = 0;
            long result = 0;
            byte[] read = new byte[1];
            do {
                if(net.Read(read) == 0)
                    return new Tuple<long, byte>(-1, 0);
                int value = (read[0] & 0x7f);
                result |= value << (7 * numRead);
                numRead++;
                if(numRead > 10)
                    throw new TypeLoadException("VarInt is too big");
            } while((read[0] & 0x80) != 0);
            return new Tuple<long, byte>(result, numRead);
        }
    }

    struct UCPacket {
        public int length;
        public Tuple<int, byte> pktID;
        public byte[] data;
        public BinaryReader reader;
        public UCPacket(NetworkStream net) {
            length = VariableNumbers.ReadVarInt(net).Item1;
            if(length == -1)
                throw new EndOfStreamException("Unable to Load VarInt");
            pktID = VariableNumbers.ReadVarInt(net);
            if(pktID.Item2 == 0)
                throw new EndOfStreamException("Unable to Load VarInt");
            data = new byte[length - pktID.Item2];
            if(data.Length != 0) {
                if(net.Read(data, 0, data.Length) != data.Length)
                    throw new EndOfStreamException("Unable to Load VarInt");
            }
            reader = new BinaryReader(new MemoryStream(data));
        }
        public UCPacket(int _pktID, byte[] _data) {
            data = _data;
            pktID = new Tuple<int, byte>(_pktID, 0);
            length = 0;
            reader = new BinaryReader(new MemoryStream(data));
        }
        public byte[] WritePacket() {
            var stream = new MemoryStream();
            byte[] id = VariableNumbers.CreateVarInt(pktID.Item1);
            stream.Write(id);
            stream.Write(data);
            byte[] pktData = stream.ToArray();
            stream.SetLength(0);
            stream.Write(VariableNumbers.CreateVarInt(pktData.Length));
            stream.Write(pktData);
            return stream.ToArray();
        }
    }

    public struct Vector3d {
        public double X, Y, Z;
        public Vector3d(double x, double y, double z) {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3d operator -(Vector3d a, Vector3d b) {
            return new Vector3d(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }
    }

    #region Entity

    interface IEntity {
        int ID {
            get;
            set;
        }
        double X {
            get;
            set;
        }
        double Y {
            get;
            set;
        }
        double Z {
            get;
            set;
        }
        double vX {
            get;
            set;
        }
        double vY {
            get;
            set;
        }
        double vZ {
            get;
            set;
        }
        float rotX {
            get;
            set;
        }
        float rotY {
            get;
            set;
        }
        bool onFire {
            get;
            set;
        }
    }

    public struct Player : IEntity {
        public int ID {
            get;
            set;
        }
        public double X {
            get;
            set;
        }
        public double Y {
            get;
            set;
        }
        public double Z {
            get;
            set;
        }
        public float rotX {
            get;
            set;
        }
        public float rotY {
            get;
            set;
        }
        public bool onFire {
            get;
            set;
        }
        public double vX {
            get;
            set;
        }
        public double vY {
            get;
            set;
        }
        public double vZ {
            get;
            set;
        }

        public string uuid;
        public string name;
        public struct Property {
            public string name;
            public string value;
            public bool isSigned;
            public string signature;
            public Property(string _name, string _value, string _signature = null) {
                name = _name;
                value = _value;
                signature = _signature;
                isSigned = signature != null;
            }
        }
        public Property[] properties;
        public Gamemode gamemode;
        public int ping;
        public bool hasDisplayName;
        public byte[] displayName;
        public bool isConnected;
        public bool onGround;

        public Player(int ID, Vector3d XYZ, Vector2 rot, string _uuid) {
            //General Entity Init
            this.ID = ID;
            X = XYZ.X;
            Y = XYZ.Y;
            Z = XYZ.Z;
            vX = vY = vZ = 0;
            rotX = rot.X;
            rotY = rot.Y;
            onFire = false;
            //Player Specific Init
            uuid = _uuid;
            name = null;
            properties = null;
            gamemode = 0;
            ping = 0;
            hasDisplayName = false;
            displayName = null;
            isConnected = true;
            onGround = false;
        }

        public Player ChangeVelocity(Vector3d vel) {
            vX = vel.X;
            vY = vel.Y;
            vZ = vel.Z;
            return this;
        }
        public Player ChangePosition(Vector3d pos) {
            X = pos.X;
            Y = pos.Y;
            Z = pos.Z;
            return this;
        }

        public Player ChangeRotation(Vector2 rot) {
            rotX = rot.X;
            rotY = rot.Y;
            return this;
        }

        public Player SetOnGround(bool onGround) {
            this.onGround = onGround;
            return this;
        }

        public Player SetName(string name) {
            this.name = name;
            return this;
        }

        public Player SetProperties(Property[] properties) {
            this.properties = properties;
            return this;
        }

        public Player SetGamemode(Gamemode gamemode) {
            this.gamemode = gamemode;
            return this;
        }

        public Player SetPing(int ping) {
            this.ping = ping;
            return this;
        }

        public Player SetDisplayName(bool hasDisplayName, byte[] displayName = null) {
            this.hasDisplayName = hasDisplayName;
            this.displayName = displayName;
            return this;
        }
    }

    #endregion

    enum State {
        Handshaking = 0,
        Status = 1,
        Login = 2,
        Play = 3,
        Disconnect = 4 //Not Offical State
    }

    struct ClientSettings {
        public string locale;
        public byte renderDistance;
        public int chatMode;
        public bool chatColors;
        public byte displaySkin;
        public int mainHand;
    }

    public enum Gamemode {
        Survival,
        Creative,
        Adventure,
        Spectator,
        Hardcore
    }

    public enum Dimension {
        Nether = -1,
        Overworld,
        End
    }

    public struct PlayerInfoData {
        public enum Action { AddPlayer, UpdateGamemode, UpdateLatency, UpdateDisplayName, RemovePlayer }
        public Action action;
        public Player[] players;
        public PlayerInfoData(Action _action, Player[] _players) {
            action = _action;
            players = _players;
        }
    }

    #endregion

    sealed class Pool {
        private static readonly Lazy<Pool> lazy = new Lazy<Pool>(() => new Pool());

        public List<int> players;
        public List<IEntity> entities;
        public byte[] seedHash;
        public List<Client> clients;
        public JsonElement properties;
        public Queue<UCPacket> broadcast;
        public int tps;
        public bool closeNextTick;

        public static Pool Instance {
            get {
                return lazy.Value;
            }
        }

        private Pool() {
            clients = new List<Client>();
            players = new List<int>();
            entities = new List<IEntity>();
            broadcast = new Queue<UCPacket>();
        }
    }

    class Server {
        TcpListener listener;
        bool running;
        string propertiesPath;

        public Server(string[] args) {
            //Server Init
            ConsoleErrorWriterDecorator.SetToConsole(); //Logging
            ConsoleOutWriter.SetToConsole(); //Logging
            running = true;
            long nspt = 1000000000 / Stopwatch.Frequency;
            propertiesPath = Directory.GetCurrentDirectory() + "\\server.properties";
            if(!File.Exists(propertiesPath))
                CreatePropertiesFile(propertiesPath);
            ReadPropertiesFile(propertiesPath);
            var ip = Pool.Instance.properties.GetProperty("server-ip").GetString();
            listener = new TcpListener(ip != "" ? IPAddress.Parse(ip) : IPAddress.Any, Pool.Instance.properties.GetProperty("server-port")
                .GetInt32());
            Pool.Instance.seedHash = SHA256.Create().ComputeHash(BitConverter.GetBytes(Pool.Instance.properties.GetProperty("level-seed")
                .GetInt64()));
            Pool.Instance.closeNextTick = false;

            //Listen For Clients
            Console.WriteLine("Server Started");
            listener.Start();
            Stopwatch time = Stopwatch.StartNew();
            long lastTime = time.ElapsedTicks * nspt;
            double ammountOfTicks = 20.0;
            double ns = 1000000000.0 / ammountOfTicks;
            double delta = 0;
            int tickCount = 0;
            long timer = time.ElapsedMilliseconds;
            while(running) {
                long now = time.ElapsedTicks * nspt;
                delta += (now - lastTime) / ns;
                lastTime = now;
                while(delta >= 1) {
                    try {
                        if(Pool.Instance.closeNextTick) {
                            foreach(Client c in Pool.Instance.clients)
                                if(c.canTick)
                                    c.Tick();
                            Pool.Instance.broadcast.Clear();
                            running = false;
                        } else {
                            foreach(Client c in Pool.Instance.clients)
                                if(c.canTick)
                                    c.Tick();
                            Pool.Instance.broadcast.Clear();
                        }
                    } catch(Exception) { }
                    delta--;
                    tickCount++;
                }
                if(time.ElapsedMilliseconds - timer >= 1000) {
                    Pool.Instance.tps = tickCount;
                    tickCount = 0;
                    timer = time.ElapsedMilliseconds;
                }
                if(running && listener.Pending()) {
                    Thread thread = new Thread((object state) => {
                        TcpClient clt = listener.AcceptTcpClient();
                        Client client = new Client();
                        int clientID = client.clientID;
                        Pool.Instance.clients.Add(client);
                        client.Start(clt);
                        client.Dispose();
                        Pool.Instance.clients.RemoveAt(Pool.Instance.clients.FindIndex(c => c.clientID == clientID));
                    });
                    thread.Start();
                }
            }

            //Clean Up
            CloseAll();
            listener.Stop();
        }

        public static void CloseAll() {
            var stream = new MemoryStream();
            using(var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true })) {
                writer.WriteStartObject();
                writer.WriteString("text", "Server Closed");
                writer.WriteEndObject();
            }
            Pool.Instance.broadcast.Enqueue(new UCPacket(0x1B, stream.ToArray()));
            Pool.Instance.closeNextTick = true;
        }

        #region Properties

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

        #endregion

        #region World



        #endregion
    }

    class Client {

        /* TODO For Everything thats not Play
         * Replace Encryption Libs
         * Read World Files
         * 
         * Handshake: Wrong Version Error Message at Client
         * Legacy Server List Ping: Implement Server List Ping
         * Set Compression: Add It
         * Login Plguin: Add It
         * Request: Implement faviconIcon
         */

        #region Variables

        public int clientID;
        public bool canTick;
        protected string username, uuid, socketIp;
        protected bool compression, encrypted, stdout;
        protected State currentState;
        protected RSACryptoServiceProvider rsa; //TODO
        protected AesManaged aes; //TODO
        protected ICryptoTransform enc, dec;
        protected byte[] verify, publicKey;
        protected JsonElement profileSkin;
        protected Player? player;
        protected long next, tickCount;
        protected NetworkStream net;
        protected TcpClient user;
        protected Stopwatch latency;
        protected readonly int[] dontLogIN, dontLogOUT;
        protected ClientSettings settings;

        #endregion

        #region Main

        public Client() {
            dontLogIN = new int[] { 0x0F };
            dontLogOUT = new int[] { 0x21, 0x34, 0x22 };
            rsa = new RSACryptoServiceProvider(1024);
            clientID = new Random().Next();
            compression = false;
            encrypted = false;
            canTick = false;
            stdout = true;
            settings = new ClientSettings();
            currentState = State.Handshaking;
            latency = new Stopwatch();
        }

        public void Start(TcpClient _user) {
            if(_user == null)
                return;
            user = _user;
            socketIp = user.Client.RemoteEndPoint.ToString();
            net = user.GetStream();
            net.ReadTimeout = 30000;
            tickCount = 0;
            while(IsConnected(user)) {
                while(!net.DataAvailable && IsConnected(user))
                    ;
                HandlePacket(net);
            }
        }

        public void Dispose() {
            Close();
            rsa.Dispose();
            net.Close();
            net.Dispose();
            user.Close();
            user.Dispose();
            Console.WriteLine("Connection with {0} has been lost", username);
        }

        private bool IsConnected(TcpClient user) {
            if(player.HasValue)
                return !currentState.Equals(State.Disconnect) && user.Connected && player.Value.isConnected;
            else
                return !currentState.Equals(State.Disconnect) && user.Connected;
        }

        public void Close() {
            if(currentState.Equals(State.Play)) {
                Pool.Instance.entities.Remove(player);
                Pool.Instance.players.Remove(player.Value.ID);
                if(IsConnected(user))
                    PlayPlayerInfo(net, new PlayerInfoData(PlayerInfoData.Action.RemovePlayer, new Player[] { player.Value }));
            }
            currentState = State.Disconnect;
        }

        public void Tick() {
            if(!IsConnected(user)) {
                Dispose();
                return;
            }
            if(tickCount % 20 == 0) {
                PlayPlayerInfo(net, new PlayerInfoData(PlayerInfoData.Action.UpdateLatency, Pool.Instance.entities.FindAll(e =>
                        Pool.Instance.players.Contains(e.ID)).Cast<Player>().ToArray()));
                PlayKeepAliveClient(net);
            }
            Pool.Instance.broadcast.ToList().ForEach(packet => HandleBroadcastPacket(net, packet));
            tickCount++;
        }

        private void HandleBroadcastPacket(NetworkStream net, UCPacket packet) {
            WritePacket(net, packet);
            switch(packet.pktID.Item1) {
                case 0x1B:
                    Close();
                    break;
            }
        }

        public void HandlePacket(NetworkStream net) {
            UCPacket packet;
            try {
                if(!net.DataAvailable)
                    return;
                packet = new UCPacket(net);
                if(stdout && !dontLogIN.Contains(packet.pktID.Item1))
                    Console.WriteLine("Recieved Packet: Length: {0}, ID: {1}, Data: {2}", packet.length,
                        new BigInteger(packet.pktID.Item1).ToString("x").PadLeft(2, '0'), new BigInteger(packet.data).ToString("x") + " | "
                        + Encoding.UTF8.GetString(packet.data).Trim());
                if(player.HasValue) {
                    latency.Stop();
                    player.Value.SetPing((int)latency.ElapsedMilliseconds); 
                } 
            } catch(EndOfStreamException) { Close(); return; } //Timeout Detection
            switch(currentState) {
                case State.Handshaking:
                    HandleHandshakingPackets(net, packet);
                    break;
                case State.Status:
                    HandleStatusPackets(net, packet);
                    break;
                case State.Login:
                    HandleLoginPackets(net, packet);
                    break;
                case State.Play:
                    HandlePlayPackets(net, packet);
                    break;
            }
        }

        #endregion

        #region Packets

        #region Handshaking

        private void HandleHandshakingPackets(NetworkStream net, UCPacket packet) {
            switch(packet.pktID.Item1) {
                case 0x00://Handshake
                    HandshakingHandshake(packet);
                    break;
                case 0xFE://Logacy Server List Ping
                    HandshakingLegacyServerListPing(net, packet);
                    break;
            }
        }

        #region Serverbound

        private void HandshakingHandshake(UCPacket packet) {
            if(VariableNumbers.ReadVarInt(packet.reader).Item1 != 578)
                ;//TODO
            packet.reader.ReadBytes(VariableNumbers.ReadVarInt(packet.reader).Item1); //Unused
            packet.reader.ReadBytes(2); //Unused
            currentState = (State)VariableNumbers.ReadVarInt(packet.reader).Item1;
            Console.WriteLine("State: " + currentState.ToString());
        }

        private void HandshakingLegacyServerListPing(NetworkStream net, UCPacket packet) {
            throw new NotImplementedException("Legacy Server List Ping");
        }

        #endregion

        #endregion

        #region Status

        private void HandleStatusPackets(NetworkStream net, UCPacket packet) {
            switch(packet.pktID.Item1) {
                case 0x00: //Request
                    StatusRequest(net, packet);
                    break;
                case 0x01: //Ping
                    StatusPing(net, packet);
                    break;
            }
        }

        #region Serverbound

        private void StatusRequest(NetworkStream net, UCPacket packet) {
            StatusResponse(net, packet);
        }

        private void StatusPing(NetworkStream net, UCPacket packet) {
            StatusPong(net, packet);
        }

        #endregion

        #region Clientbound

        private void StatusResponse(NetworkStream net, UCPacket packet) {
            var options = new JsonWriterOptions {
                Indented = true
            };
            using(var stream = new MemoryStream()) {
                using(var writer = new Utf8JsonWriter(stream, options)) {
                    writer.WriteStartObject();
                    writer.WriteStartObject("version"); //Start Version Object
                    writer.WriteString("name", "1.15.2");
                    writer.WriteNumber("protocol", 578);
                    writer.WriteEndObject(); //End Version Object
                    writer.WriteStartObject("players"); //Start Players Object
                    writer.WriteNumber("max", Pool.Instance.properties.GetProperty("max-players").GetInt64());
                    writer.WriteNumber("online", Pool.Instance.players.Count);
                    writer.WriteStartArray("sample"); //Start Sample Array
                    writer.WriteEndArray(); //End Sample Array
                    writer.WriteEndObject(); //End Players Object
                    writer.WriteStartObject("description"); //Start Description Object
                    writer.WriteString("text", Pool.Instance.properties.GetProperty("motd").GetString());
                    writer.WriteEndObject(); //End Description Object
                    writer.WriteEndObject();
                }
                var response = stream.ToArray();
                stream.SetLength(0);
                stream.Write(VariableNumbers.CreateVarInt(response.Length));
                stream.Write(response);
                packet = new UCPacket(0x00, stream.ToArray());
                WritePacket(net, packet);
            }
        }

        private void StatusPong(NetworkStream net, UCPacket packet) {
            WritePacket(net, packet);
            Close();
        }

        #endregion

        #endregion

        #region Login

        private void HandleLoginPackets(NetworkStream net, UCPacket packet) {
            switch(packet.pktID.Item1) {
                case 0x00:
                    LoginLoginStart(net, packet);
                    break;
                case 0x01:
                    LoginEncryptionResponse(net, packet);
                    break;
            }
        }

        #region Clientbound

        private void LoginDisconnect(NetworkStream net, string reason) {
            var stream = new MemoryStream();
            using(var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true })) {
                writer.WriteStartObject();
                writer.WriteString("text", reason);
                writer.WriteEndObject();
            }
            WriteMessageClose(net, 0x00, stream.ToArray());
            Console.WriteLine("Disconnected {0} for Reason: {1}", username, reason);
        }

        private void LoginEncryptionRequest(NetworkStream net) {
            MemoryStream stream = new MemoryStream();
            stream.Write(VariableNumbers.CreateVarInt(20));
            stream.Write(new byte[20]);
            publicKey = rsa.ExportSubjectPublicKeyInfo();
            stream.Write(VariableNumbers.CreateVarInt(publicKey.Length));
            stream.Write(publicKey);
            stream.Write(VariableNumbers.CreateVarInt(4));
            verify = new byte[4];
            new Random().NextBytes(verify);
            stream.Write(verify);
            UCPacket packet = new UCPacket(0x01, stream.ToArray());
            WritePacket(net, packet);
        }

        private void LoginLoginSuccess(NetworkStream net) {
            MemoryStream stream = new MemoryStream();
            byte[] uuidBytes = Encoding.UTF8.GetBytes(uuid);
            stream.Write(VariableNumbers.CreateVarInt(uuidBytes.Length));
            stream.Write(uuidBytes);
            var nameBytes1 = Encoding.UTF8.GetBytes(username);
            stream.Write(VariableNumbers.CreateVarInt(nameBytes1.Length));
            stream.Write(nameBytes1);
            UCPacket packet = new UCPacket(0x02, stream.ToArray());
            WritePacket(net, packet);
            currentState = State.Play;
            Console.WriteLine("Player {0} has Joined.", username);
            player = new Player(GetNextEntityID(), new Vector3d(0, 0, 0), new Vector2(0, 0), uuid);
            Pool.Instance.players.Add(player.Value.ID);
            PlayStart(net);
        }

        #endregion

        #region Serverbound

        private void LoginLoginStart(NetworkStream net, UCPacket packet) {
            username = Encoding.UTF8.GetString(packet.reader.ReadBytes(VariableNumbers.ReadVarInt(packet.reader).Item1));
            if(Pool.Instance.properties.GetProperty("online-mode").GetBoolean() && !socketIp.StartsWith("127.0.0.1")) { //Online Mode
                LoginEncryptionRequest(net);
            } else { //Offline Mode
                uuid = GenerateOfflineUUID(username).Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
                LoginLoginSuccess(net);
            }
        }

        private void LoginEncryptionResponse(NetworkStream net, UCPacket packet) {
            var secret = rsa.Decrypt(packet.reader.ReadBytes(VariableNumbers.ReadVarInt(packet.reader).Item1),
                        RSAEncryptionPadding.Pkcs1);
            byte[] clientVerify = rsa.Decrypt(packet.reader.ReadBytes(VariableNumbers.ReadVarInt(packet.reader).Item1),
                RSAEncryptionPadding.Pkcs1);
            MemoryStream stream = new MemoryStream();
            if(!ArraysEqual(clientVerify, verify)) {
                using(var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true })) {
                    writer.WriteStartObject();
                    writer.WriteString("text", "Login Verification Failed!");
                    writer.WriteEndObject();
                }
                WriteMessageClose(net, 0x00, stream.ToArray());
                return;
            }
            stream.SetLength(0);
            stream.Write(Encoding.ASCII.GetBytes(Encoding.UTF8.GetString(new byte[20])));
            stream.Write(secret);
            stream.Write(publicKey);
            var hash = MinecraftShaDigest(new SHA1Managed().ComputeHash(stream.ToArray()));
            string url = string.Format("https://sessionserver.mojang.com/session/minecraft/hasJoined?username={0}&serverId={1}",
                username, hash);
            profileSkin = GetJsonFromURL(net, url, true);
            if(profileSkin.TryGetProperty("id", out JsonElement value))
                if(value.GetString().Equals(""))
                    return;
            aes = new AesManaged();
            aes.Key = aes.IV = secret;
            enc = aes.CreateEncryptor();
            dec = aes.CreateDecryptor();
            encrypted = true;
            stream.SetLength(0);
            var tmp = profileSkin.GetProperty("id").GetString();
            uuid = tmp.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
            LoginLoginSuccess(net);
        }

        #endregion

        #endregion

        #region Play

        private void HandlePlayPackets(NetworkStream net, UCPacket packet) {
            switch(packet.pktID.Item1) {
                case 0x00:
                    PlayTeleportConfirm(net, packet);
                    break;
                case 0x03:
                    PlayChatMessageServ(net, packet);
                    break;
                case 0x05:
                    PlayClientSettings(net, packet);
                    break;
                case 0x0B:
                    PlayPluginMessageServer(net, packet);
                    break;
                case 0x0F:
                    PlayKeepAliveServer(net, packet);
                    break;
                case 0x11:
                    PlayPlayerPosition(net, new BinaryReader(new MemoryStream(packet.data)));
                    break;
                case 0x12:
                    PlayPlayerPosRotServ(net, packet);
                    break;
                case 0x13:
                    PlayPlayerRotation(net, new BinaryReader(new MemoryStream(packet.data)));
                    break;
            }
        }

        private void PlayStart(NetworkStream net) {
            player.Value.SetDisplayName(true, Encoding.UTF8.GetBytes(username));
            player.Value.SetGamemode((Gamemode)Enum.Parse(typeof(Gamemode), Pool.Instance.properties.GetProperty("gamemode").GetString(), true));
            player.Value.SetName(username);
            player.Value.SetPing(-1);
            PlayJoinGame(net);
            PlayPluginMessageClient(net, "minecraft:brand");
            uuid = GetRealUUID(username);
            JsonElement json = GetJsonFromURL(net, "https://sessionserver.mojang.com/session/minecraft/profile/" + uuid.Replace("-", ""), false)
                .GetProperty("properties").EnumerateArray().First();
            player.Value.SetProperties(new Player.Property[] { new Player.Property(json.GetProperty("name").GetString(), json.GetProperty("value").GetString()) });
            PlayPlayerInfo(net, new PlayerInfoData(PlayerInfoData.Action.AddPlayer, Pool.Instance.entities.FindAll(e => 
                Pool.Instance.players.Contains(e.ID)).Cast<Player>().ToArray()));
            PlayPlayerInfo(net, new PlayerInfoData(PlayerInfoData.Action.UpdateLatency, Pool.Instance.entities.FindAll(e =>
                Pool.Instance.players.Contains(e.ID)).Cast<Player>().ToArray()));
            PlayUpdateViewPosition(net);

            PlaySpawnPosition(net);
            PlayPlayerPositionAndLookClient(net);
            canTick = true;
        }

        #region Clientbound

        private void PlayPluginMessageClient(NetworkStream net, string serverMessage) {
            var stream = new MemoryStream();
            var messageSplit = serverMessage.Split(":");
            stream.Write(VariableNumbers.CreateVarInt(messageSplit[0].Length));
            stream.Write(Encoding.UTF8.GetBytes(messageSplit[0]));
            if(serverMessage.Split(":")[0] == "minecraft")
                switch(serverMessage.Split(":")[1]) {
                    case "brand":
                        stream.Write(Encoding.UTF8.GetBytes("brand:mcmtrs"));
                        break;
                }
            var packet = new UCPacket(0x19, stream.ToArray());
            WritePacket(net, packet);
        }

        private void PlayDisconnect(NetworkStream net, string reason) {
            var stream = new MemoryStream();
            using(var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true })) {
                writer.WriteStartObject();
                writer.WriteString("text", reason);
                writer.WriteEndObject();
            }
            WriteMessageClose(net, 0x1B, stream.ToArray());
            Console.WriteLine("Disconnected {0} for Reason: {1}", username, reason);
        }

        private void PlayKeepAliveClient(NetworkStream net) {
            while(next != 0)
                ;
            next = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            latency.Restart();
            WritePacket(net, new UCPacket(0x21, BitConverter.GetBytes(next)));
        }

        private void PlayChunkData(NetworkStream net, int x, int y) {
            var stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(x));
            stream.Write(BitConverter.GetBytes(y));
            stream.WriteByte(0x01); //Full Chunk
            stream.Write(VariableNumbers.CreateVarInt(0)); //Bitmask for every 16×16×16 chunk section whose data is included in Data.
            var nbt = new NbtWriter(stream, "");
            nbt.WriteTag(new NbtLongArray("MOTION_BLOCKING", Enumerable.Repeat(0L, 1024).ToArray()));
            nbt.EndCompound();
            nbt.Finish();
            stream.Write(Enumerable.Repeat(127, 1024).SelectMany(BitConverter.GetBytes).ToArray()); //Set All Biomes to 'The Void'
            stream.Write(VariableNumbers.CreateVarInt(0)); //Size of Data in bytes
            stream.Write(VariableNumbers.CreateVarInt(0)); //Number of block entities
            var packet = new UCPacket(0x22, stream.ToArray());
            WritePacket(net, packet);
        }

        private void PlayJoinGame(NetworkStream net) {
            var stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(GetNextEntityID()));
            stream.WriteByte((byte)((int)player.Value.gamemode & (Pool.Instance.properties.GetProperty("hardcore").GetBoolean() ? 0x4 : 0x0)));
            stream.Write(BitConverter.GetBytes((int)Dimension.Overworld));
            stream.Write(Pool.Instance.seedHash.AsSpan().Slice(0, 8));
            stream.WriteByte(20); //Unused by Client and actual max players MAY be over 255
            var levelType = Encoding.UTF8.GetBytes(Pool.Instance.properties.GetProperty("level-type").GetString());
            stream.Write(VariableNumbers.CreateVarInt(levelType.Length));
            stream.Write(levelType);
            stream.Write(VariableNumbers.CreateVarInt(Pool.Instance.properties.GetProperty("view-distance").GetInt32()));
            stream.Write(new byte[2] { 0x01, 0x01 }); //Reduced Debug Info Enabled & doImmediateRespawn False cause no GameRules yet
            var packet = new UCPacket(0x26, stream.ToArray());
            WritePacket(net, packet);
        }

        private void PlayPlayerInfo(NetworkStream net, PlayerInfoData data) {
            //Do shit
            var stream = new MemoryStream();
            stream.Write(VariableNumbers.CreateVarInt((int)data.action));
            stream.Write(VariableNumbers.CreateVarInt(data.players.Length));
            foreach(var player in data.players) {
                var uuidBytes = Encoding.UTF8.GetBytes(player.uuid);
                stream.Write(VariableNumbers.CreateVarInt(uuidBytes.Length));
                stream.Write(uuidBytes);
                switch(data.action) {
                    case PlayerInfoData.Action.AddPlayer:
                        stream.Write(Encoding.UTF8.GetBytes(player.name));
                        stream.Write(VariableNumbers.CreateVarInt(player.properties.Length));
                        foreach(var property in player.properties) {
                            var tempBytes = Encoding.UTF8.GetBytes(property.name);
                            stream.Write(VariableNumbers.CreateVarInt(tempBytes.Length));
                            stream.Write(tempBytes);
                            tempBytes = Encoding.UTF8.GetBytes(property.value);
                            stream.Write(VariableNumbers.CreateVarInt(tempBytes.Length));
                            stream.Write(tempBytes);
                            stream.WriteByte((byte)(property.isSigned ? 0x01 : 0x00));
                            if(property.isSigned) {
                                tempBytes = Encoding.UTF8.GetBytes(property.signature);
                                stream.Write(VariableNumbers.CreateVarInt(tempBytes.Length));
                                stream.Write(tempBytes);
                            }
                        }
                        stream.Write(VariableNumbers.CreateVarInt((int)player.gamemode));
                        stream.Write(VariableNumbers.CreateVarInt(player.ping));
                        stream.WriteByte((byte)(player.hasDisplayName ? 0x01 : 0x00));
                        if(player.hasDisplayName)
                            stream.Write(player.displayName);
                        break;
                    case PlayerInfoData.Action.UpdateGamemode:
                        stream.Write(VariableNumbers.CreateVarInt((int)player.gamemode));
                        break;
                    case PlayerInfoData.Action.UpdateLatency:
                        stream.Write(VariableNumbers.CreateVarInt(player.ping));
                        break;
                    case PlayerInfoData.Action.UpdateDisplayName:
                        stream.WriteByte((byte)(player.hasDisplayName ? 0x01 : 0x00));
                        if(player.hasDisplayName)
                            stream.Write(player.displayName);
                        break;
                }
            }
            WritePacket(net, new UCPacket(0x34, stream.ToArray()));
        }

        private void PlayUpdateViewPosition(NetworkStream net) {
            var stream = new MemoryStream();
            stream.Write(VariableNumbers.CreateVarInt((int)(player.Value.X / 16)));
            stream.Write(VariableNumbers.CreateVarInt((int)(player.Value.Z / 16)));
            WritePacket(net, new UCPacket(0x41, stream.ToArray()));
        }

        private void PlayPlayerPositionAndLookClient(NetworkStream net) {
            var stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(0.0));
            stream.Write(BitConverter.GetBytes(0.0));
            stream.Write(BitConverter.GetBytes(0.0));
            stream.Write(BitConverter.GetBytes(0.0f));
            stream.Write(BitConverter.GetBytes(0.0f));
            stream.WriteByte(0);
            next = new Random().Next(int.MinValue, int.MaxValue);
            stream.Write(VariableNumbers.CreateVarInt((int)next));
            WritePacket(net, new UCPacket(0x36, stream.ToArray()));
        }

        private void PlayHeldItemChange(NetworkStream net, byte slot) {
            WritePacket(net, new UCPacket(0x40, new byte[] { slot }));
        }

        private void PlaySpawnPosition(NetworkStream net) {
            ulong pos = 0L; //((x & 0x3FFFFFF) << 38) | ((z & 0x3FFFFFF) << 12) | (y & 0xFFF)
            WritePacket(net, new UCPacket(0x4E, BitConverter.GetBytes(pos)));
        }

        private void PlayDeclareRecipes(NetworkStream net) {
            var stream = new MemoryStream();
            stream.Write(VariableNumbers.CreateVarInt(0));
            //TODO
            WritePacket(net, new UCPacket(0x5B, stream.ToArray()));
        }

        private void PlayTags(NetworkStream net) {
            var stream = new MemoryStream();
            stream.Write(VariableNumbers.CreateVarInt(0));
            stream.Write(VariableNumbers.CreateVarInt(0));
            stream.Write(VariableNumbers.CreateVarInt(0));
            stream.Write(VariableNumbers.CreateVarInt(0));
            //TODO
            WritePacket(net, new UCPacket(0x5B, stream.ToArray()));
        }

        #endregion

        #region Serverbound

        private void PlayTeleportConfirm(NetworkStream net, UCPacket packet) {
            if(VariableNumbers.ReadVarInt(packet.reader).Item1 != (int)next)
                PlayDisconnect(net, "Teleport Confirm Failed");
            next = 0;
        }

        private void PlayChatMessageServ(NetworkStream net, UCPacket packet) {
            string text = Encoding.UTF8.GetString(packet.reader.ReadBytes(VariableNumbers.ReadVarInt(packet.reader).Item1));
            bool broadcast = false;
            var stream = new MemoryStream();
            using(var writer = new Utf8JsonWriter(stream)) {
                writer.WriteStartObject();
                if(text.StartsWith('/')) {
                    //Command Handling
                    switch(text.Substring(1)) {
                        case "help":
                            writer.WriteString("text", "Go ask GLS for help");
                            break;
                        case "tps":
                            writer.WriteString("text", "TPS: " + Pool.Instance.tps);
                            break;
                        case "stop":
                            writer.WriteString("text", "Out Of Order");
                            //Server.CloseAll();
                            break;
                        default:
                            writer.WriteString("color", "dark_red");
                            writer.WriteString("text", "Command Doesn't Exist");
                            break;
                    }
                } else {
                    writer.WriteString("translate", "chat.type.text");
                    writer.WriteStartArray("with");
                    writer.WriteStartObject();
                    writer.WriteString("text", username);
                    writer.WriteStartObject("clickEvent");
                    writer.WriteString("action", "suggest_command");
                    writer.WriteString("value", "/msg " + username + " ");
                    writer.WriteEndObject();
                    writer.WriteStartObject("clickEvent");
                    writer.WriteString("action", "show_entity");
                    writer.WriteString("value", "{id:" + uuid + ",name:" + username + "}");
                    writer.WriteEndObject();
                    writer.WriteString("insertion", username);
                    writer.WriteEndObject();
                    writer.WriteStartObject();
                    writer.WriteString("text", text);
                    writer.WriteEndObject();
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }
            byte[] temp = stream.ToArray();
            stream = new MemoryStream();
            stream.Write(VariableNumbers.CreateVarInt(temp.Length));
            stream.Write(temp);
            stream.Write(VariableNumbers.CreateVarInt(text.StartsWith('/') ? 1 : 0));
            if(broadcast)
                Pool.Instance.broadcast.Enqueue(new UCPacket(0x0F, stream.ToArray()));
            else
                WritePacket(net, new UCPacket(0x0F, stream.ToArray()));
        }

        private void PlayClientSettings(NetworkStream net, UCPacket packet) {
            settings.locale = Encoding.UTF8.GetString(packet.reader.ReadBytes(VariableNumbers.ReadVarInt(packet.reader).Item1));
            settings.renderDistance = packet.reader.ReadByte();
            settings.chatMode = VariableNumbers.ReadVarInt(packet.reader).Item1;
            settings.chatColors = packet.reader.ReadByte() != 0;
            settings.displaySkin = packet.reader.ReadByte();
            settings.mainHand = VariableNumbers.ReadVarInt(packet.reader).Item1;
        }

        private void PlayPluginMessageServer(NetworkStream net, UCPacket packet) {
            var identifier = VariableNumbers.ReadVarInt(packet.reader);
            string channel = Encoding.UTF8.GetString(packet.reader.ReadBytes(identifier.Item1));
            string data = Encoding.UTF8.GetString(packet.reader.ReadBytes(packet.data.Length - identifier.Item2));
            //Ignore The Messages
        }

        private void PlayKeepAliveServer(NetworkStream net, UCPacket packet) {
            if(BitConverter.ToInt64(packet.data) != next)
                PlayDisconnect(net, "Keep Alive Failed");
            next = 0;
        }

        private void PlayPlayerPosition(NetworkStream net, BinaryReader data) {
            var next = new Vector3d(data.ReadDouble(), data.ReadDouble(), data.ReadDouble());
            double max = 32000000.0;
            if(next.X > max || next.Y > max || !double.IsFinite(next.X) || !double.IsFinite(next.Y) || !double.IsFinite(next.Z))
                PlayDisconnect(net, "Invalid move player packet received");
            var tDV = next - new Vector3d(player.Value.X, player.Value.Y, player.Value.Z);
            double tD = (tDV.X * tDV.X) + (tDV.Y * tDV.Y) + (tDV.Z * tDV.Z);
            double eD = (player.Value.vX * player.Value.vX) + (player.Value.vY * player.Value.vY) + (player.Value.vZ * player.Value.vZ);
            if(tD - eD <= 100)
                player.Value.ChangePosition(next);
            else
                PlayPlayerPositionAndLookClient(net);
            player.Value.SetOnGround(data.ReadByte() != 0);
        }

        private void PlayPlayerPosRotServ(NetworkStream net, UCPacket packet) {
            PlayPlayerPosition(net, new BinaryReader(new MemoryStream(packet.data.AsSpan().Slice(0, 24).ToArray().Concat(new byte[] { packet.data[32] })
                .ToArray())));
            PlayPlayerRotation(net, new BinaryReader(new MemoryStream(packet.data.AsSpan().Slice(24, 9).ToArray())));
        }

        private void PlayPlayerRotation(NetworkStream net, BinaryReader data) {
            player.Value.ChangeRotation(new Vector2(data.ReadSingle(), data.ReadSingle()));
            player.Value.SetOnGround(data.ReadByte() != 0);
        }

        #endregion

        #endregion

        #endregion

        #region Misc

        private void WriteMessageClose(NetworkStream net, byte LeaveID, byte[] jsonMessage) {
            MemoryStream stream = new MemoryStream();
            stream.Write(VariableNumbers.CreateVarInt(jsonMessage.Length));
            stream.Write(jsonMessage);
            UCPacket packet = new UCPacket(LeaveID, stream.ToArray());
            WritePacket(net, packet);
            Close();
        }

        private int GetNextEntityID() {
            return new Random().Next();
        }

        public void WritePacket(NetworkStream net, UCPacket packet) {
            if(stdout && !dontLogOUT.Contains(packet.pktID.Item1))
                Console.WriteLine("Sending Packet With ID: " + new BigInteger(packet.pktID.Item1).ToString("x").PadLeft(2, '0'));
            byte[] packetData = packet.WritePacket();
            try {
                if(encrypted)
                    net.Write(Encrypt(packetData));
                else
                    net.Write(packetData);
            } catch(IOException) { Close(); return; };
        }

        public bool ArraysEqual(byte[] a, byte[] b) {
            if(a.Length != b.Length)
                return false;
            for(int i = 0; i < a.Length; i++)
                if(a[i] != b[i])
                    return false;
            return true;
        }

        public byte[] Encrypt(byte[] data) {
            byte[] encrypted;
            MemoryStream stream = new MemoryStream();
            using(CryptoStream cs = new CryptoStream(stream, enc, CryptoStreamMode.Write)) {
                cs.Write(data);
                encrypted = stream.ToArray();
            }
            stream.Dispose();
            return encrypted;
        }

        public static string MinecraftShaDigest(byte[] hash) {
            Array.Reverse(hash);
            BigInteger b = new BigInteger(hash);
            if(b < 0)
                return "-" + (-b).ToString("x").TrimStart('0');
            else
                return b.ToString("x").TrimStart('0');
        }

        private string GenerateOfflineUUID(string name) {
            var temp1 = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + username));
            temp1[6] = ((byte)((temp1[6] & 0x0F) | 0x30));
            temp1[8] = ((byte)((temp1[8] & 0x3f) | 0x80));
            return MinecraftShaDigest(temp1);
        }

        private string GetRealUUID(string username) {
            HttpWebRequest httpRequest = (HttpWebRequest) WebRequest.Create("https://api.mojang.com/profiles/minecraft");
            httpRequest.Method = WebRequestMethods.Http.Post;
            string postData = "[\n\t\""+username+ "\",\n\t\"nonExistingPlayer\"\n]";
            httpRequest.ContentLength = postData.Length;
            httpRequest.ContentType = "application/json";
            StreamWriter requestWriter = new StreamWriter(
                httpRequest.GetRequestStream(),
                System.Text.Encoding.ASCII);
            requestWriter.Write(postData);
            requestWriter.Close();
            HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            Stream httpResponseStream = httpResponse.GetResponseStream();
            var stream = new MemoryStream();
            byte[] buffer = new byte[1024];
            int bytesRead;
            while((bytesRead = httpResponseStream.Read(buffer, 0, 1024)) != 0)
                stream.Write(buffer, 0, bytesRead);
            var json = JsonDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()), new JsonDocumentOptions { AllowTrailingCommas = true })
                .RootElement;
            stream.Dispose();
            return json.EnumerateArray().First().GetProperty("id").GetString();
        }

        private JsonElement GetJsonFromURL(NetworkStream net, string url, bool timeout) {
            var request = new WebClient();
            request.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 8.0)");
            Task<string> response = request.DownloadStringTaskAsync(url);
            response.Wait(25000);
            JsonElement json = JsonDocument.Parse("{\n\t\"id\": \"\",\n\t\"name\": \"\",\n\t\"properties\": [\n\t\t{\n\t\t}\n\t]\n}", new JsonDocumentOptions { AllowTrailingCommas = true }).RootElement;
            Console.WriteLine(response.Result);
            if(response.IsCompleted)
                json = JsonDocument.Parse(response.Result, new JsonDocumentOptions { AllowTrailingCommas = true }).RootElement;
            else if(timeout) {
                if(currentState.Equals(State.Login))
                    LoginDisconnect(net, "Timed out");
                if(currentState.Equals(State.Play))
                    PlayDisconnect(net, "Timed out");
            }
            return json;
        }

        #endregion
    
    }

    #region Logging

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

    #endregion

}