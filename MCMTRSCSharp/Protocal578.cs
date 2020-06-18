using System;
using System.Diagnostics;
using System.IO;
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
        public static int ReadVarInt(BinaryReader stream, out int numRead) {
            numRead = 0;
            int result = 0;
            byte read;
            do {
                read = stream.ReadByte();
                int value = (read & 0x7f);
                result |= (value << (7 * numRead));
                numRead++;
                if(numRead > 5)
                    throw new TypeLoadException("VarInt is too big");
            } while((read & 0x80) != 0);

            return result;
        }
        public static long ReadVarLong(BinaryReader stream, out int numRead) {
            numRead = 0;
            long result = 0;
            byte read;
            do {
                read = stream.ReadByte();
                int value = (read & 0x7f);
                result |= value << (7 * numRead);
                numRead++;
                if(numRead > 10)
                    throw new TypeLoadException("VarLong is too big");
            } while((read & 0x80) != 0);

            return result;
        }
        public static void WriteVarInt(BinaryWriter stream, int value) {
            do {
                byte temp = (byte)(value & 0b01111111);
                value >>= 7;
                if(value != 0)
                    temp |= 0b10000000;
                stream.Write(temp);
            } while(value != 0);
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
        public static void WriteVarLong(BinaryWriter stream, long value) {
            do {
                byte temp = (byte)(value & 0b01111111);
                value >>= 7;
                if(value != 0)
                    temp |= 0b10000000;
                stream.Write(temp);
            } while(value != 0);
        }
    }

    struct UCPacket {
        public int length;
        public int pktID;
        public byte[] data;
        public BinaryReader reader;
        public UCPacket(BinaryReader stream) {
            length = VariableNumbers.ReadVarInt(stream, out int _);
            pktID = VariableNumbers.ReadVarInt(stream, out int pktIDLen);
            data = stream.ReadBytes(length - pktIDLen);
            reader = new BinaryReader(new MemoryStream(data));
        }
        public UCPacket(int _pktID, byte[] _data) {
            data = _data;
            pktID = _pktID;
            length = 0;
            reader = new BinaryReader(new MemoryStream(data));
        }
        public byte[] WritePacket() {
            var stream = new MemoryStream();
            byte[] id = VariableNumbers.CreateVarInt(pktID);
            stream.Write(id);
            stream.Write(data);
            byte[] pktData = stream.ToArray();
            stream.SetLength(0);
            stream.Write(VariableNumbers.CreateVarInt(pktData.Length));
            stream.Write(pktData);
            return stream.ToArray();
        }
    }

    enum State {
        Handshaking = 0,
        Status = 1,
        Login = 2,
        Play = 3
    }

    #endregion

    class Client {

        /* TODO
         * Replace Encryption Libs
         * Implement Config File
         * Make Everything Async
         * 
         * Handshake: Wrong Version Error Message at Client
         * Legacy Server List Ping: Implement Server List Ping
         * Set Compression: Add It
         * Login Plguin: Add It
         * Request: Implement MOTD
         */

        #region Variables

        protected string username, uuid, socketIp;
        protected bool compression, connected, encrypted;
        protected State currentState;
        protected RSACryptoServiceProvider rsa; //TODO
        protected AesManaged aes; //TODO
        protected ICryptoTransform enc, dec;
        protected byte[] verify, publicKey;
        protected JsonDocument json;

        #endregion

        #region Main

        public Client() {
            rsa = new RSACryptoServiceProvider(1024);
            compression = false;
            connected = true;
            encrypted = false;
            currentState = State.Handshaking;
        }

        public void Start(TcpClient user) {
            if(user == null)
                return;
            socketIp = user.Client.RemoteEndPoint.ToString();
            NetworkStream net = user.GetStream();
            Stopwatch s = new Stopwatch();
            s.Start();
            //TODO: Improve this
            while(connected && user.Connected) {
                while(!net.DataAvailable && user.Connected && connected && s.ElapsedMilliseconds < 30000)
                    ;
                if(s.ElapsedMilliseconds >= 30000) {
                    Close();
                    break;
                }
                HandlePacket(net);
                s.Restart();
            }
            Close();
            s.Stop();
            rsa.Dispose();
            net.Close();
            net.Dispose();
            user.Close();
            user.Dispose();
            Console.WriteLine("Connection with {0} has been lost", username);
        }

        public void Close() {
            connected = false;
            if(currentState.Equals(State.Play))
                Pool.Instance.connected--;
            currentState = State.Handshaking;
        }

        public void HandlePacket(NetworkStream net) {
            UCPacket packet = new UCPacket(new BinaryReader(net));
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
            }
        }

        #endregion

        #region Packets

        #region Handshaking

        private void HandleHandshakingPackets(NetworkStream net, UCPacket packet) {
            switch(packet.pktID) {
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
            if(VariableNumbers.ReadVarInt(packet.reader, out int _) != 578)
                ;//TODO
            packet.reader.ReadBytes(VariableNumbers.ReadVarInt(packet.reader, out int __)); //Unused
            packet.reader.ReadBytes(2); //Unused
            currentState = (State)VariableNumbers.ReadVarInt(packet.reader, out __);
        }

        private void HandshakingLegacyServerListPing(NetworkStream net, UCPacket packet) {
            throw new NotImplementedException("Legacy Server List Ping");
        }

        #endregion

        #endregion

        #region Status

        private void HandleStatusPackets(NetworkStream net, UCPacket packet) {
            switch(packet.pktID) {
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
                    writer.WriteNumber("max", Pool.Instance.maxPlayers);
                    writer.WriteNumber("online", Pool.Instance.connected);
                    writer.WriteStartArray("sample"); //Start Sample Array
                    writer.WriteEndArray(); //End Sample Array
                    writer.WriteEndObject(); //End Players Object
                    writer.WriteStartObject("description"); //Start Description Object
                    writer.WriteString("text", "TODO: Implement MOTD");
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
            WritePacket(net, packet); //Pong
            Close();
        }

        #endregion

        #endregion

        #region Login

        private void HandleLoginPackets(NetworkStream net, UCPacket packet) {
            switch(packet.pktID) {
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
            Pool.Instance.connected++;
            Close();
        }

        #endregion

        #region Serverbound

        private void LoginLoginStart(NetworkStream net, UCPacket packet) {
            username = Encoding.UTF8.GetString(packet.reader.ReadBytes(VariableNumbers.ReadVarInt(packet.reader, out int _)));
            if(Pool.Instance.OnlineMode) { //Online Mode
                LoginEncryptionRequest(net);
            } else { //Offline Mode
                uuid = GenerateOfflineUUID(username).Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
                LoginLoginSuccess(net);
            }
        }

        private void LoginEncryptionResponse(NetworkStream net, UCPacket packet) {
            var secret = rsa.Decrypt(packet.reader.ReadBytes(VariableNumbers.ReadVarInt(packet.reader, out _)),
                        RSAEncryptionPadding.Pkcs1);
            byte[] clientVerify = rsa.Decrypt(packet.reader.ReadBytes(VariableNumbers.ReadVarInt(packet.reader, out _)),
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
