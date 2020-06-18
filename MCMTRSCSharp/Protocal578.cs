using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RSA = Toolbox.EncryptionTools.RSA;

namespace MCMTRS.Protocal578 {

    /*
     * Designed for Minecraft Version 1.15.2, Protocal 578
     */

    #region DataTypes

    //Chat String

    //Identifier String

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

    struct Meta {

    }

    struct Slot {

    }

    struct NBTTag {

    }

    struct Position {

    }

    struct Angle {

    }

    struct UUID {

    }

    struct OptX {

    }

    struct ArrX {

    }

    struct XEnum {

    }

    struct UCPacket {
        public int length;
        public int pktID;
        public byte[] data;
        public UCPacket(BinaryReader stream) {
            length = VariableNumbers.ReadVarInt(stream, out int _);
            pktID = VariableNumbers.ReadVarInt(stream, out int pktIDLen);
            data = stream.ReadBytes(length - pktIDLen);
            Console.WriteLine(pktID);
        }
        public UCPacket(int _pktID, byte[] _data) {
            data = _data;
            pktID = _pktID;
            length = 0;
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
        protected string username, uuid, socketIp;
        protected bool compression, connected, encrypted;
        protected State currentState;
        protected RSACryptoServiceProvider rsa; //TODO: Replace with Custom Lib
        protected AesManaged aes; //TODO: Replace with Custom Lib
        protected ICryptoTransform enc, dec;
        protected byte[] verify, publicKey, response;
        protected JsonDocument json;

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
        }

        public void HandlePacket(NetworkStream net) {
            BinaryReader reader = new BinaryReader(net);
            UCPacket packet = new UCPacket(reader);
            BinaryReader packetReader = new BinaryReader(new MemoryStream(packet.data));
            switch(currentState) {
                case State.Handshaking:
                    switch(packet.pktID) {
                        case 0x00:
                            if(VariableNumbers.ReadVarInt(packetReader, out int _) != 578)
                                ;//TODO: Wrong Version Error Message at Client
                            packetReader.ReadBytes(VariableNumbers.ReadVarInt(packetReader, out int __)); //Unused
                            packetReader.ReadBytes(2); //Unused
                            currentState = (State)VariableNumbers.ReadVarInt(packetReader, out __);
                            Console.WriteLine("Handshake");
                            break;
                        case 0xFE:
                            //TODO: Implement Server List Ping
                            Console.WriteLine("Server List Ping");
                            break;
                    }
                    break;
                case State.Status:
                    switch(packet.pktID) {
                        case 0x00: //Request
                            //Response
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
                            break;
                        case 0x01: //Ping
                            WritePacket(net, packet); //Pong
                            Console.WriteLine("PONG " + new BigInteger(packet.data).ToString("x"));
                            Close();
                            break;
                    }
                    break;
                case State.Login:
                    switch(packet.pktID) {
                        case 0x00:
                            username = Encoding.UTF8.GetString(packetReader.ReadBytes(VariableNumbers.ReadVarInt(packetReader, out int _)));
                            MemoryStream stream = new MemoryStream();
                            if(Pool.Instance.OnlineMode && !socketIp.StartsWith("127.0.0.1")) { //Online Mode Auth
                                stream.Write(VariableNumbers.CreateVarInt(20));
                                stream.Write(new byte[20]);
                                publicKey = rsa.ExportSubjectPublicKeyInfo();
                                Console.WriteLine(new BigInteger(publicKey).ToString("x").TrimStart('0'));
                                stream.Write(VariableNumbers.CreateVarInt(publicKey.Length));
                                stream.Write(publicKey);
                                stream.Write(VariableNumbers.CreateVarInt(4));
                                verify = new byte[4];
                                new Random().NextBytes(verify);
                                Console.WriteLine(verify.Length);
                                stream.Write(verify);
                                packet = new UCPacket(0x01, stream.ToArray());
                                WritePacket(net, packet);
                            } else { //Offline Mode Auth
                                var temp1 = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + username));
                                temp1[6] = ((byte)((temp1[6] & 0x0F) | 0x30));
                                temp1[8] = ((byte)((temp1[8] & 0x3f) | 0x80));
                                uuid = MinecraftShaDigest(temp1);
                                uuid = uuid.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
                                byte[] uuidBytes1 = Encoding.UTF8.GetBytes(uuid);
                                stream.Write(VariableNumbers.CreateVarInt(uuidBytes1.Length));
                                stream.Write(uuidBytes1);
                                Console.WriteLine(uuid);
                                var nameBytes1 = Encoding.UTF8.GetBytes(username);
                                stream.Write(VariableNumbers.CreateVarInt(nameBytes1.Length));
                                stream.Write(nameBytes1);
                                packet = new UCPacket(0x02, stream.ToArray());
                                WritePacket(net, packet);
                                currentState = State.Play;
                                Console.WriteLine("Player {0} has Joined.", username);
                                Pool.Instance.connected++;
                                Thread.Sleep(1000);
                                Close();
                            }
                            break;
                        case 0x01:
                            var secret = rsa.Decrypt(packetReader.ReadBytes(VariableNumbers.ReadVarInt(packetReader, out _)), RSAEncryptionPadding.Pkcs1);
                            byte[] clientVerify = rsa.Decrypt(packetReader.ReadBytes(VariableNumbers.ReadVarInt(packetReader, out _)), RSAEncryptionPadding.Pkcs1);
                            if(!ArraysEqual(clientVerify, verify)) {
                                using(var tempStream = new MemoryStream()) {
                                    using(var writer = new Utf8JsonWriter(tempStream, new JsonWriterOptions { Indented = true })) {
                                        writer.WriteStartObject();
                                        writer.WriteString("text", "Login Verification Failed!");
                                        writer.WriteEndObject();
                                    }
                                    var tempArr = tempStream.ToArray();
                                    tempStream.SetLength(0);
                                    tempStream.Write(VariableNumbers.CreateVarInt(tempArr.Length));
                                    tempStream.Write(tempArr);
                                    packet = new UCPacket(0x00, tempStream.ToArray());
                                    WritePacket(net, packet);
                                }
                                Console.Error.WriteLine("Login Verification Failed!");
                                Close();
                                break;
                            }
                            stream = new MemoryStream();
                            stream.Write(Encoding.ASCII.GetBytes(Encoding.UTF8.GetString(new byte[20])));
                            stream.Write(secret);
                            stream.Write(publicKey);
                            var hash = MinecraftShaDigest(new SHA1Managed().ComputeHash(stream.ToArray()));
                            Console.WriteLine("Checking Authorization");
                            string URL = string.Format("https://sessionserver.mojang.com/session/minecraft/hasJoined?username={0}&serverId={1}", username, hash);
                            Console.WriteLine("Starting Request to sessionserver.mojang.com");
                            var request = new WebClient();
                            Task<byte[]> response = request.DownloadDataTaskAsync(URL);
                            response.Wait(5000);
                            if(response.IsCompleted) {
                                Console.WriteLine("Request OK");
                                using(StreamReader sr = new StreamReader(new MemoryStream(response.Result), Encoding.UTF8)) {
                                    json = JsonDocument.Parse(sr.ReadToEnd(), new JsonDocumentOptions { AllowTrailingCommas = true });
                                }
                            } else {
                                Console.WriteLine("Timed Out");
                                using(var tempStream = new MemoryStream()) {
                                    using(var writer = new Utf8JsonWriter(tempStream, new JsonWriterOptions { Indented = true })) {
                                        writer.WriteStartObject();
                                        writer.WriteString("text", "Timeout");
                                        writer.WriteEndObject();
                                    }
                                    var tempArr = tempStream.ToArray();
                                    tempStream.SetLength(0);
                                    tempStream.Write(VariableNumbers.CreateVarInt(tempArr.Length));
                                    tempStream.Write(tempArr);
                                    packet = new UCPacket(0x00, tempStream.ToArray());
                                    WritePacket(net, packet);
                                }
                                Close();
                                break;
                            }
                            aes = new AesManaged();
                            aes.Key = aes.IV = secret;
                            enc = aes.CreateEncryptor();
                            dec = aes.CreateDecryptor();
                            stream.SetLength(0);
                            var tmp = json.RootElement.GetProperty("id").GetString();
                            uuid = tmp.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
                            var uuidBytes = Encoding.UTF8.GetBytes(uuid);
                            stream.Write(VariableNumbers.CreateVarInt(uuidBytes.Length));
                            stream.Write(uuidBytes);
                            var nameBytes = Encoding.UTF8.GetBytes(json.RootElement.GetProperty("name").GetString());
                            stream.Write(VariableNumbers.CreateVarInt(nameBytes.Length));
                            stream.Write(nameBytes);
                            packet = new UCPacket(0x02, stream.ToArray());
                            WritePacket(net, packet);
                            currentState = State.Play;
                            Console.WriteLine("Player {0} has Joined.", username);
                            Pool.Instance.connected++;
                            Thread.Sleep(1000);
                            Close();
                            break;
                    }
                    break;
            }
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
        
        public byte[] PublicKeyToDER(RSA.KeyPair key) {
            byte[] modulusData = key.n.ToByteArray();
            byte[] exponentData = key.e.ToByteArray();
            var stream = new MemoryStream();
            WriteByteArray(ref stream, 0x02, modulusData);
            WriteByteArray(ref stream, 0x02, exponentData);
            byte[] publicKeyData = stream.ToArray();
            stream.SetLength(0);
            WriteByteArray(ref stream, 0x30, publicKeyData);
            publicKeyData = stream.ToArray();
            stream.SetLength(0);
            stream.Write(new byte[] { 0x30, 0x0d, 0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01, 0x05, 0x00 }, 0, 15);
            WriteByteArray(ref stream, 0x03, publicKeyData);
            publicKeyData = stream.ToArray();
            stream.SetLength(0);
            WriteByteArray(ref stream, 0x30, publicKeyData);
            return stream.ToArray();
        }

        //public byte[] PublicKeyToDER(RSA.KeyPair key) {
        //    byte[] modulusData = key.n.ToByteArray();
        //    byte[] exponentData = key.e.ToByteArray();
        //    var stream = new MemoryStream();
        //    WriteByteArray(ref stream, 0x02, modulusData);
        //    WriteByteArray(ref stream, 0x02, exponentData);
        //    byte[] publicKeyData = stream.ToArray();
        //    stream.SetLength(0);
        //    WriteByteArray(ref stream, 0x30, publicKeyData);
        //    return stream.ToArray();
        //}

        public byte[] DERToPEM(byte[] DER) {
            string conv = Convert.ToBase64String(DER);
            conv += "\n-----END RSA PUBLIC KEY-----";
            return Encoding.UTF8.GetBytes("-----BEGIN RSA PUBLIC KEY-----\n" + conv);
        }

        public static string MinecraftShaDigest(byte[] hash) {
            Array.Reverse(hash);
            BigInteger b = new BigInteger(hash);
            if(b < 0)
                return "-" + (-b).ToString("x").TrimStart('0');
            else
                return b.ToString("x").TrimStart('0');
        }

        public void WriteByteArray(ref MemoryStream stream, byte identifier, byte[] data) {
            if(data.Length > 127) {
                byte[] Length = VariableNumbers.CreateVarInt(data.Length);
                stream.Write(new byte[] { identifier, (byte)(0x80 + Length.Length) }, 0, 2);
                stream.Write(Length, 0, Length.Length);
            } else
                stream.Write(new byte[] { identifier, (byte)data.Length }, 0, 2);
            if(identifier == 0x03)
                stream.WriteByte(0x00);
            stream.Write(data, 0, data.Length);
        }
    } 
}
