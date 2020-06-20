using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SimplexNoise;

namespace MCMTRS.Protocal578 {

    /*
     * Designed for Minecraft Version 1.15.2, Protocal 578
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
            Console.Write("Length: " + length);
            pktID = VariableNumbers.ReadVarInt(net);
            if(pktID.Item2 == 0)
                throw new EndOfStreamException("Unable to Load VarInt");
            Console.Write(", ID: " + new BigInteger(pktID.Item1).ToString("x").PadLeft(2, '0'));
            data = new byte[length - pktID.Item2];
            if(data.Length != 0) {
                if(net.Read(data, 0, data.Length) != data.Length)
                    throw new EndOfStreamException("Unable to Load VarInt");
            }
            reader = new BinaryReader(new MemoryStream(data));
            Console.WriteLine(", Data: " + new BigInteger(data).ToString("X"));
        }
        public UCPacket(BinaryReader stream) {
            length = VariableNumbers.ReadVarInt(stream).Item1;
            if(length == -1)
                throw new EndOfStreamException("Unable to Load VarInt");
            pktID = VariableNumbers.ReadVarInt(stream);
            if(pktID.Item2 == 0)
                throw new EndOfStreamException("Unable to Load VarInt");
            data = new byte[length - pktID.Item2];
            if(stream.Read(data) != data.Length)
                throw new EndOfStreamException("Unable to Load VarInt");
            reader = new BinaryReader(new MemoryStream(data));
            Console.WriteLine("Length: {0}, ID: {1}, Data: {2}", length, pktID.Item1, new BigInteger(data).ToString("x"));
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

    struct Entity {
        public int ID;
    }

    enum State {
        Handshaking = 0,
        Status = 1,
        Login = 2,
        Play = 3,
        Disconnect = 4 //Not Offical State
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

    #endregion

    class Client {

        /* TODO For Everything thats not Play
         * Replace Encryption Libs
         * Make Everything Async
         * 
         * Handshake: Wrong Version Error Message at Client
         * Legacy Server List Ping: Implement Server List Ping
         * Set Compression: Add It
         * Login Plguin: Add It
         * Request: Implement faviconIcon
         */

        #region Variables

        protected string username, uuid, socketIp;
        protected bool compression, encrypted;
        protected State currentState;
        protected RSACryptoServiceProvider rsa; //TODO
        protected AesManaged aes; //TODO
        protected ICryptoTransform enc, dec;
        protected byte[] verify, publicKey;

        #endregion

        #region Main

        public Client() {
            rsa = new RSACryptoServiceProvider(1024);
            compression = false;
            encrypted = false;
            currentState = State.Handshaking;
        }

        public void Start(TcpClient user) {
            if(user == null)
                return;
            socketIp = user.Client.RemoteEndPoint.ToString();
            NetworkStream net = user.GetStream();
            net.ReadTimeout = 30000;
            //TODO: Improve this
            while(IsConnected(user)) {
                while(!net.DataAvailable && IsConnected(user))
                    ; //Waits for next Packet unless Disconnected
                HandlePacket(net);
            }
            Close();
            rsa.Dispose();
            net.Close();
            net.Dispose();
            user.Close();
            user.Dispose();
            Console.WriteLine("Connection with {0} has been lost", username);
        }

        private bool IsConnected(TcpClient user) {
            return !currentState.Equals(State.Disconnect) && user.Connected;
        }

        public void Close() {
            currentState = State.Disconnect;
        }

        public void HandlePacket(NetworkStream net) {
            Console.WriteLine("Reading Next Packet");
            UCPacket packet;
            try { packet = new UCPacket(net); } catch(EndOfStreamException e) { Close(); return; } //Timeout Detection
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
            Console.WriteLine("Responded");
        }

        private void StatusPing(NetworkStream net, UCPacket packet) {
            StatusPong(net, packet);
            Console.WriteLine("Ponged");
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
                Console.WriteLine(Encoding.UTF8.GetString(response));
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
            Pool.Instance.players.Add(this);
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
            string URL = string.Format("https://sessionserver.mojang.com/session/minecraft/hasJoined?username={0}&serverId={1}",
                username, hash);
            Console.WriteLine(URL);
            var request = new WebClient();
            request.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 8.0)");
            Task<string> response = request.DownloadStringTaskAsync(URL);
            response.Wait(25000);
            JsonDocument json;
            if(response.IsCompleted)
                using(StreamReader sr = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(response.Result)), Encoding.UTF8))
                    json = JsonDocument.Parse(sr.ReadToEnd(), new JsonDocumentOptions { AllowTrailingCommas = true });
            else {
                stream.SetLength(0);
                using(var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true })) {
                    writer.WriteStartObject();
                    writer.WriteString("text", "Timeout");
                    writer.WriteEndObject();
                }
                Console.WriteLine(socketIp + " Failed To Authenticate With Mojang Servers");
                WriteMessageClose(net, 0x00, stream.ToArray());
                return;
            }
            aes = new AesManaged();
            aes.Key = aes.IV = secret;
            enc = aes.CreateEncryptor();
            dec = aes.CreateDecryptor();
            encrypted = true;
            stream.SetLength(0);
            var tmp = json.RootElement.GetProperty("id").GetString();
            uuid = tmp.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
            LoginLoginSuccess(net);
        }

        #endregion

        #endregion

        #region Play

        private void HandlePlayPackets(NetworkStream net, UCPacket packet) {
            switch(packet.pktID.Item1) {
                case 0x05://Client Settings
                    //TODO: Implement
                    break;
                case 0x0B://Plugin Message (serverbound)
                    PlayPluginMessageServer(net, packet);
                    break;
            }
        }

        private void PlayStart(NetworkStream net) {
            PlayJoinGame(net);
            PlayPluginMessageClient(net, "minecraft:brand");
            PlayDisconnect(net, "The Ban Hammer has Spoken");
        }

        #region Clientbound

        private void PlayJoinGame(NetworkStream net) {
            var stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(GetNextEntityID()));
            stream.WriteByte((byte)(int)Enum.Parse(typeof(Gamemode), Pool.Instance.properties.GetProperty("gamemode").GetString(), true));
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

        #endregion

        #region Serverbound

        private void PlayPluginMessageServer(NetworkStream net, UCPacket packet) {
            var identifier = VariableNumbers.ReadVarInt(packet.reader);
            string channel = Encoding.UTF8.GetString(packet.reader.ReadBytes(identifier.Item1));
            string data = Encoding.UTF8.GetString(packet.reader.ReadBytes(packet.data.Length - identifier.Item2));
            Console.WriteLine(channel + ":" + data);
            //TODO: Implement all the important messages
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
            return Pool.Instance.entities.Count;
        }

        public void WritePacket(NetworkStream net, UCPacket packet) {
            if(encrypted)
                net.Write(Encrypt(packet.WritePacket()));
            else
                net.Write(packet.WritePacket());
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
                using(BinaryWriter sw = new BinaryWriter(cs))
                    sw.Write(data);
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

        #endregion
    
    }
}
