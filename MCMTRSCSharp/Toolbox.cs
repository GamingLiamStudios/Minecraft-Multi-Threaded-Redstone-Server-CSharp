using System;
using System.Text;
using System.Collections;
using System.IO;
using SInt28 = Toolbox.SynchsafeInt28;
using SInt35 = Toolbox.SynchsafeInt35;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using System.Diagnostics;

namespace Toolbox {
    class ByteTools {
        public static string BytesToString(byte[] vs, ref int index, int length) {
            string text = Encoding.ASCII.GetString(vs, index, length);
            index += length;
            //Console.WriteLine("index " + index + ": " + text);
            return text;
        }
        public static string BytesToString(byte[] vs, ref int index, int length, Encoding encoding) {
            string text = encoding.GetString(vs, index, length);
            index += length;
            //Console.WriteLine("index " + index + ": " + text);
            return text;
        }
        public static ushort BytesToUShort(byte[] vs, ref int index, bool isBigEndian = false) {
            byte[] bData = new byte[2];
            Array.Copy(vs, index, bData, 0, 2);
            if(isBigEndian)
                Array.Reverse(bData);
            ushort data = BitConverter.ToUInt16(bData, 0);
            index += 2;
            //Console.WriteLine("Index " + index + ": " + data);
            return data;
        }
        public static byte[] UShortToBytes(ushort data, bool isBigEndian = false) {
            byte[] vs = new byte[2];
            vs[0] = (byte)((data & 0xFF00) >> 8);
            vs[1] = (byte)(data & 0x00FF);
            if(!isBigEndian)
                Array.Reverse(vs);
            //Console.WriteLine("Index " + index + ": " + data);
            return vs;
        }
        public static uint BytesToUInt(byte[] vs, ref int index, bool isBigEndian = false) {
            byte[] bData = new byte[4];
            Array.Copy(vs, index, bData, 0, 4);
            if(isBigEndian)
                Array.Reverse(bData);
            uint data = BitConverter.ToUInt32(bData, 0);
            index += 4;
            //Console.WriteLine("Index " + index + ": " + data);
            return data;
        }
        public static SInt28 BytesToSInt28(byte[] vs, ref int index, bool isBigEndian = false) {
            byte[] bData = new byte[4];
            Array.Copy(vs, index, bData, 0, 4);
            if(!isBigEndian)
                Array.Reverse(bData);
            SInt28 data = bData;
            index += 4;
            //Console.WriteLine("Index " + index + ": " + (uint)data);
            return data;
        }
        public static SInt35 BytesToSInt35(byte[] vs, ref int index, bool isBigEndian = false) {
            byte[] bData = new byte[5];
            Array.Copy(vs, index, bData, 0, 5);
            if(!isBigEndian)
                Array.Reverse(bData);
            SInt35 data = bData;
            index += 5;
            //Console.WriteLine("Index " + index + ": " + (ulong)data);
            return data;
        }
        public static BitArray BytesToBitArray(byte[] vs, ref int index, int byteLength) {
            byte[] bData = new byte[byteLength];
            Array.Copy(vs, index, bData, 0, byteLength);
            BitArray bits = new BitArray(bData);
            index += byteLength;
            //Console.Write("Index " + index + ": ");
            //foreach(bool bit in bits) Console.Write(bit?"1":"0");
            //Console.WriteLine();
            return bits;
        }
    }
    class BitStream : Stream {
        bool[] bits;

        public static implicit operator BitStream(MemoryStream stream) {
            BitStream bitStream = new BitStream();
            byte[] sArr = stream.ToArray();
            BitArray array = new BitArray(sArr);
            array.CopyTo(bitStream.bits, 0);
            Array.Reverse(bitStream.bits);
            return bitStream;
        }
        public override bool CanRead {
            get;
        }

        public override bool CanSeek {
            get;
        }

        public override bool CanWrite {
            get;
        }

        public override long Length {
            get;
        }

        public override long Position {
            get; set;
        }

        public override void Flush() {
            throw new NotImplementedException();
        }
        public int Read(int countBits) {
            if(Position + countBits > Length)
                throw new IndexOutOfRangeException();
            int data = 0;
            for(int i = 0; i < countBits; i++)
                data |= bits[Position + i] ? 1 : 0 << i;
            Position += countBits;
            return data;
        }
        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }
    }
    class BitTools {
        public static int BitArrayToInt(BitArray bitArray, ref int index, int length) {
            if(index + length > bitArray.Length)
                throw new IndexOutOfRangeException();
            int value = 0;
            for(int i = index; i < index + length; i++)
                if(bitArray[i])
                    value += Convert.ToInt16(Math.Pow(2, i));
            index += length;
            return value;
        }
    }

    namespace EncryptionTools {
        class RSA {
            public struct KeyPair {
                public BigInteger n;
                public BigInteger e;
                public BigInteger d;
            }
            RNGCryptoServiceProvider rNG;

            public RSA() {
                rNG = new RNGCryptoServiceProvider();
            }

            public void Dispose() {
                rNG.Dispose();
            }

            public KeyPair GenerateKeyPair() {
                Stopwatch s = new Stopwatch();
                s.Start();
                KeyPair key = new KeyPair();
                BigInteger p, q;
                key.d = p = q = BigInteger.Zero;
                int k = key.n.ToByteArray().Length;
                while(BigInteger.Pow(new BigInteger(256), k - 1) <= key.n & key.n < BigInteger.Pow(new BigInteger(256), k)) {
                    p = CreateCryptoBigInteger(512);
                    while(!p.IsProbablyPrime())
                        p = CreateCryptoBigInteger(512);
                    q = CreateCryptoBigInteger(512);
                    while(!q.IsProbablyPrime())
                        q = CreateCryptoBigInteger(512);
                    key.n = p * q;
                    k = key.n.ToByteArray().Length;
                }
                key.e = 65537;
                key.d = BigInteger.ModPow(key.e, -1, LCM(p - 1, q - 1));
                return key;
            }

            public byte[] DecryptPadded(byte[] paddedData, KeyPair key) {
                var stream = new MemoryStream(BigInteger.ModPow(new BigInteger(paddedData), key.d, key.n).ToByteArray());
                stream.Read(new byte[2], 0, 2);
                int r = key.n.ToByteArray().Length - 3 - 16;
                stream.Read(new byte[r], 0, r);
                stream.ReadByte();
                byte[] decryptedData = new byte[16];
                stream.Read(decryptedData, 0, 16);
                return decryptedData;
            }

            BigInteger LCM(BigInteger a, BigInteger b) {
                return a / GCD(a, b) * b;
            }

            BigInteger GCD(BigInteger x, BigInteger y) {
                while(y != 0) {
                    BigInteger tmp = x % y;
                    x = y;
                    y = tmp;
                }
                return x;
            }

            BigInteger CreateCryptoBigInteger(int bits) {
                byte[] bytes = new byte[bits / 8];
                rNG.GetBytes(bytes);
                return new BigInteger(bytes);
            }
        }

        public static class PrimeExtensions {
            // Random generator (thread safe)
            private static ThreadLocal<Random> s_Gen = new ThreadLocal<Random>(
              () => {
                  return new Random();
              }
            );
            // Random generator (thread safe)
            private static Random Gen {
                get {
                    return s_Gen.Value;
                }
            }
            public static Boolean IsProbablyPrime(this BigInteger value, int witnesses = 10) {
                if(value <= 1)
                    return false;
                if(witnesses <= 0)
                    witnesses = 10;
                BigInteger d = value - 1;
                int s = 0;
                while(d % 2 == 0) {
                    d /= 2;
                    s += 1;
                }
                Byte[] bytes = new Byte[value.ToByteArray().LongLength];
                BigInteger a;
                for(int i = 0; i < witnesses; i++) {
                    do {
                        Gen.NextBytes(bytes);
                        a = new BigInteger(bytes);
                    }
                    while(a < 2 || a >= value - 2);
                    BigInteger x = BigInteger.ModPow(a, d, value);
                    if(x == 1 || x == value - 1)
                        continue;
                    for(int r = 1; r < s; r++) {
                        x = BigInteger.ModPow(x, 2, value);
                        if(x == 1)
                            return false;
                        if(x == value - 1)
                            break;
                    }
                    if(x != value - 1)
                        return false;
                }
                return true;
            }
        }
    }

    public struct SynchsafeInt28 {
        private UInt32 VALUE;
        public static implicit operator UInt32(SynchsafeInt28 value) {
            return value.VALUE;
        }
        public static implicit operator SynchsafeInt28(int value) {
            SynchsafeInt28 _ReturnValue = new SynchsafeInt28();
            _ReturnValue.VALUE = (uint)value;
            return _ReturnValue;
        }
        public static SynchsafeInt28 operator -(SynchsafeInt28 value, int value1) {
            SynchsafeInt28 _ReturnValue = new SynchsafeInt28();
            _ReturnValue.VALUE = value.VALUE - (uint)value1;
            return _ReturnValue;
        }
        public static implicit operator SynchsafeInt28(byte[] value) {
            SynchsafeInt28 _ReturnValue = new SynchsafeInt28();
            UInt32 byte0 = value[0];
            UInt32 byte1 = value[1];
            UInt32 byte2 = value[2];
            UInt32 byte3 = value[3];
            _ReturnValue.VALUE = byte0 << 21 | byte1 << 14 | byte2 << 7 | byte3;
            return _ReturnValue;
        }
    }
    public struct SynchsafeInt35 {
        private UInt64 VALUE;
        public static implicit operator UInt64(SynchsafeInt35 value) {
            return value.VALUE;
        }
        public static implicit operator SynchsafeInt35(int value) {
            SynchsafeInt35 _ReturnValue = new SynchsafeInt35();
            _ReturnValue.VALUE = (uint)value;
            return _ReturnValue;
        }
        public static SynchsafeInt35 operator -(SynchsafeInt35 value, int value1) {
            SynchsafeInt35 _ReturnValue = new SynchsafeInt35();
            _ReturnValue.VALUE = value.VALUE - (uint)value1;
            return _ReturnValue;
        }
        public static implicit operator SynchsafeInt35(byte[] value) {
            SynchsafeInt35 _ReturnValue = new SynchsafeInt35();
            UInt64 byte0 = value[0];
            UInt64 byte1 = value[1];
            UInt64 byte2 = value[2];
            UInt64 byte3 = value[3];
            UInt64 byte4 = value[4];
            _ReturnValue.VALUE = byte0 << 28 | byte1 << 21 | byte2 << 14 | byte3 << 7 | byte4;
            return _ReturnValue;
        }
    }
}
