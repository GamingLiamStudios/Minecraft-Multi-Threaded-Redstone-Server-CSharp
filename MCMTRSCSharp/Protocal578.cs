using fNbt;
using fNbt.Tags;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
                value = (int)((uint)value >> 7);
                if(value != 0)
                    temp |= 0b10000000;
                stream.WriteByte(temp);
            } while(value != 0);
            Console.WriteLine();
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
            Console.WriteLine("Recieved Packet: Length: {0}, ID: {1}, Data: {2}", length,
                new BigInteger(pktID.Item1).ToString("x").PadLeft(2, '0'), new BigInteger(data).ToString("x") + " | "
                + Encoding.UTF8.GetString(data));
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
            Console.WriteLine("Recieved Packet: Length: {0}, ID: {1}, Data: {2}", length,
                new BigInteger(pktID.Item1).ToString("x").PadLeft(2, '0'), new BigInteger(data).ToString("x") + " | "
                + Encoding.UTF8.GetString(data));
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
            Console.WriteLine("Sending Packet With ID: " + pktID.Item1);
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

    public struct Player {
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
        public Player(string _uuid, string _name, Property[] _properties, Gamemode _gamemode, int _ping, byte[] _displayName = null) {
            uuid = _uuid;
            name = _name;
            properties = _properties;
            gamemode = _gamemode;
            ping = _ping;
            displayName = _displayName;
            hasDisplayName = displayName != null;
        }
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
        protected JsonElement profileSkin;
        protected Player player;
        protected int next;

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
            PlaySpawnPosition(net);
            PlayPlayerPositionAndLookClient(net);
            PlayChunkData(net, 0, 0);
            PlayChunkData(net, -1, 0);
            PlayChunkData(net, 0, -1);
            PlayChunkData(net, -1, -1);
            //if(profileSkin.Equals(new JsonElement()))
            //    profileSkin = GetJsonFromURL(net, string.Format("https://sessionserver.mojang.com/session/minecraft/profile/{0}?unsigned=false", uuid), false);
            //var skinBlob = profileSkin.GetProperty("properties").EnumerateArray().First();
            //if(skinBlob.TryGetProperty("name", out _)) {
            //    PlayPlayerInfo(net, new PlayerInfoData(PlayerInfoData.Action.AddPlayer, new Player[] { new Player(uuid, username,
            //    new Player.Property[] { new Player.Property(skinBlob.GetProperty("name").GetString(), skinBlob.GetProperty("value").GetString(),
            //    skinBlob.GetProperty("signature").GetString()) }, (Gamemode)Enum.Parse(typeof(Gamemode), Pool.Instance.properties.GetProperty("gamemode").GetString()
            //    ,true), 10) }));
            //} else
            //    PlayPlayerInfo(net, new PlayerInfoData(PlayerInfoData.Action.AddPlayer, new Player[] { new Player(uuid, username,
            //    new Player.Property[] { }, (Gamemode)Enum.Parse(typeof(Gamemode), Pool.Instance.properties.GetProperty("gamemode").GetString(), true), 10) }));
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
            stream.WriteByte((byte)((int)Enum.Parse(typeof(Gamemode), Pool.Instance.properties.GetProperty("gamemode").GetString(), true)
                & (Pool.Instance.properties.GetProperty("hardcore").GetBoolean() ? 0x4 : 0x0)));
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

        private void PlayPlayerPositionAndLookClient(NetworkStream net) {
            var stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(0.0));
            stream.Write(BitConverter.GetBytes(0.0));
            stream.Write(BitConverter.GetBytes(0.0));
            stream.Write(BitConverter.GetBytes(0.0f));
            stream.Write(BitConverter.GetBytes(0.0f));
            stream.WriteByte(0);
            next = new Random().Next(int.MinValue, int.MaxValue);
            stream.Write(VariableNumbers.CreateVarInt(next));
            WritePacket(net, new UCPacket(0x36, stream.ToArray()));
        }

        private void PlayHeldItemChange(NetworkStream net, byte slot) {
            WritePacket(net, new UCPacket(0x40, new byte[] { slot }));
        }

        private void PlaySpawnPosition(NetworkStream net) {
            ulong pos = 0L; //((x & 0x3FFFFFF) << 38) | ((z & 0x3FFFFFF) << 12) | (y & 0xFFF)
            WritePacket(net, new UCPacket(0x4E, new byte[8]));
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
            if(VariableNumbers.ReadVarInt(packet.reader).Item1 != next)
                PlayDisconnect(net, "Teleport Confirm Failed");
            next = 0;
        }

        private void PlayPluginMessageServer(NetworkStream net, UCPacket packet) {
            var identifier = VariableNumbers.ReadVarInt(packet.reader);
            string channel = Encoding.UTF8.GetString(packet.reader.ReadBytes(identifier.Item1));
            string data = Encoding.UTF8.GetString(packet.reader.ReadBytes(packet.data.Length - identifier.Item2));
            //Ignore The Messages
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

        private JsonElement GetJsonFromURL(NetworkStream net, string url, bool timeout) {
            var request = new WebClient();
            request.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 8.0)");
            Task<string> response = request.DownloadStringTaskAsync(url);
            response.Wait(25000);
            JsonElement json = JsonDocument.Parse("{\n\t\"id\": \"\",\n\t\"name\": \"\",\n\t\"properties\": [\n\t\t{\n\t\t}\n\t]\n}", new JsonDocumentOptions { AllowTrailingCommas = true }).RootElement;
            if(response.IsCompleted)
                using(StreamReader sr = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(response.Result)), Encoding.UTF8))
                    json = JsonDocument.Parse(sr.ReadToEnd(), new JsonDocumentOptions { AllowTrailingCommas = true }).RootElement;
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
}
