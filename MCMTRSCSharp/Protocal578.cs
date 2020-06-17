using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Toolbox;
using Toolbox.EncryptionTools;
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
            Console.WriteLine("\n"+length + " " + (length - pktIDLen));
            Console.WriteLine(new BigInteger(data).ToString("x"));
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
        protected string username, uuid, json;
        protected bool compression, connected;
        protected State currentState;
        protected RSA.KeyPair key;
        protected AesManaged aes;
        protected ICryptoTransform enc, dec;
        protected byte[] verify, publicKey;

        public Client(TcpClient user) {
            if(user == null)
                return;
            NetworkStream net = user.GetStream();
            var rsa = new RSA();
            key = rsa.GenerateKeyPair();
            rsa.Dispose();
            compression = false;
            connected = true;
            currentState = State.Handshaking;
            while(connected && user.Connected) {
                while(!net.DataAvailable && user.Connected)
                    ;
                HandlePacket(net);
            }
            net.Close();
            net.Dispose();
            user.Close();
            user.Dispose();
            Console.WriteLine("Connection with {0} has been lost", username);
        }

        public void Close() {
            connected = false;
        }

        public void HandlePacket(NetworkStream net) {
            BinaryReader reader = new BinaryReader(net);
            BinaryWriter writer = new BinaryWriter(net);
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
                            break;
                        case 0xFE:
                            //TODO: Implement Server List Ping
                            break;
                    }
                    break;
                case State.Login:
                    switch(packet.pktID) {
                        case 0x00:
                            username = Encoding.UTF8.GetString(packetReader.ReadBytes(VariableNumbers.ReadVarInt(packetReader, out int _)));
                            MemoryStream stream = new MemoryStream();
                            stream.Write(VariableNumbers.CreateVarInt(20));
                            stream.Write(new byte[20]);
                            publicKey = PublicKeyToDER(key);
                            Console.WriteLine(new BigInteger(publicKey).ToString("x").TrimStart('0'));
                            stream.Write(VariableNumbers.CreateVarInt(publicKey.Length));
                            stream.Write(publicKey);
                            stream.Write(VariableNumbers.CreateVarInt(4));
                            verify = new byte[4];
                            new Random().NextBytes(verify);
                            Console.WriteLine(verify.Length);
                            stream.Write(verify);
                            packet = new UCPacket(0x01, stream.ToArray());
                            net.Write(packet.WritePacket());
                            Console.WriteLine("And It nulls here");
                            break;
                        case 0x01:
                            Console.WriteLine("Ooh, A Response");
                            var secret = RSA.DecryptPadded(packetReader.ReadBytes(VariableNumbers.ReadVarInt(packetReader, out _)), key);
                            if(!RSA.DecryptPadded(packetReader.ReadBytes(VariableNumbers.ReadVarInt(packetReader, out _)), key).Equals(verify)) {
                                //TODO: Client Message 'Login Failed'
                                Console.Error.WriteLine("Login Verification Failed!");
                                net.Close();
                                break;
                            }
                            stream = new MemoryStream();
                            stream.Write(new byte[20]);
                            stream.Write(secret);
                            stream.Write(publicKey);
                            var hash = MinecraftShaDigest(new SHA1Managed().ComputeHash(stream));
                            var get = new HttpClient().GetStringAsync(
                                string.Format("https://sessionserver.mojang.com/session/minecraft/hasJoined?username={0}&serverId={1}", username, hash));
                            get.Wait();
                            json = get.Result;
                            aes = new AesManaged();
                            aes.Key = aes.IV = secret;
                            enc = aes.CreateEncryptor();
                            dec = aes.CreateDecryptor();
                            stream.SetLength(0);
                            var tmp = json.Substring(json.IndexOf("\"id\": ") + 1);
                            tmp = tmp.Substring(0, tmp.IndexOf('"') - 1);
                            uuid = tmp.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
                            byte[] txt = Encoding.UTF8.GetBytes(uuid);
                            VariableNumbers.WriteVarInt(writer, txt.Length);
                            stream.Write(txt);
                            txt = Encoding.UTF8.GetBytes(username);
                            VariableNumbers.WriteVarInt(writer, txt.Length);
                            stream.Write(txt);
                            packet = new UCPacket(0x02, stream.ToArray());
                            net.Write(Encrypt(packet.WritePacket()));
                            currentState = State.Play;
                            Console.WriteLine("Player {0} has Joined.", username);
                            break;
                    }
                    break;
            }
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
