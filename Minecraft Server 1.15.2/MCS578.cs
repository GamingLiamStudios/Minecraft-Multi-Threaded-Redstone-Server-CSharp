using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Toolbox;

namespace MCS578 {

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
                int value = (read & 0b01111111);
                result |= (value << (7 * numRead));
                numRead++;
                if(numRead > 5)
                    throw new TypeLoadException("VarInt is too big");
            } while((read & 0b10000000) != 0);

            return result;
        }
        public static long ReadVarLong(BinaryReader stream, out int numRead) {
            numRead = 0;
            long result = 0;
            byte read;
            do {
                read = stream.ReadByte();
                int value = (read & 0b01111111);
                result |= value << (7 * numRead);
                numRead++;
                if(numRead > 10)
                    throw new TypeLoadException("VarLong is too big");
            } while((read & 0b10000000) != 0);

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
        }
    }

    enum State {
        Handshaking = 0,
        Status = 1,
        Login = 2
    }

    #endregion

    class Server {
        TcpListener listner;
        int port;
        bool running;
        static void Main(string[] args) {
            new Server(args);
        }
        public Server(string[] args) {
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
            while(running) {
                while(listner.Pending())
                    new Task(() => HandleUsers(listner.AcceptTcpClient())).Start();
            }
            listner.Stop();
        }
        public void HandleUsers(TcpClient user) {
            if(user == null)
                return;
            NetworkStream net = user.GetStream();
            BinaryReader reader = new BinaryReader(net);
            BinaryWriter writer = new BinaryWriter(net);
            string username;
            int _ = 0;
            bool comp = false;
            State currentState = State.Handshaking;
            while(user.Connected) {
                if(comp) {
                    //TODO
                } else {
                    UCPacket packet = new UCPacket(reader);
                    if(currentState.Equals(0)) {
                        //Handshaking
                        switch(packet.pktID) {
                            case 0x00:
                                //Handshake
                                if(VariableNumbers.ReadVarInt(reader, out int _) != 578)
                                    ;//TODO
                                string servAddr = ByteTools.BytesToString(reader.ReadBytes(255), ref _, 255); //Unused
                                ushort servPort = ByteTools.BytesToUShort(reader.ReadBytes(2), ref _); //Unused
                                currentState = (State)VariableNumbers.ReadVarInt(reader, out _);
                                break;
                        }
                    } else if(currentState.Equals(1)) {
                        //Status
                    } else if(currentState.Equals(2)) {
                        //Login
                        switch(packet.pktID) {
                            case 0x00:
                                //Login Start
                                username = ByteTools.BytesToString(reader.ReadBytes(16 * 4), ref _, 16);
                                break;
                        }
                    }
                }
            }
            net.Close();
            net.Dispose();
            user.Close();
            user.Dispose();
        }

        public byte[] PublicKeyToDER(EncryptionTools.KeyPair key) {
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

        public void HandleClientbound(ref State currentState, int pktID) {
            switch(currentState) {
                case State.Login:
                    switch(pktID) {
                        case 0x01:

                            break;
                    }
                    break;
            }
        }
    }
}
