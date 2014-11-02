using System;
using System.Net;
using System.Text;

namespace FileServer.Core.Messages
{
    public class AnnounceMessage
    {
        private static readonly DateTime UnixTimeStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public AnnounceMessage(string name, IPAddress ip, DateTime timestamp, UInt32 fileCount)
        {
            Name = name;
            Ip = ip;
            Timestamp = timestamp;
            FileCount = fileCount;
        }

        public string Name { get; protected set; }
        public IPAddress Ip { get; protected set; }
        public DateTime Timestamp { get; protected set; }
        public UInt32 FileCount { get; protected set; }

        public int Write(byte[] outData)
        {
            byte[] addrBytes = Ip.GetAddressBytes();
            Array.Copy(addrBytes, 0, outData, 0, 4);
            byte[] countBytes = BitConverter.GetBytes(FileCount);
            Array.Reverse(countBytes);
            Array.Copy(countBytes, 0, outData, 4, 4);
            byte[] timestampBytes =
                BitConverter.GetBytes(
                    (UInt64) Timestamp.Subtract(UnixTimeStart).TotalMilliseconds);
            Array.Reverse(timestampBytes);
            Array.Copy(timestampBytes, 0, outData, 8, 8);
            byte[] nameBytes = Encoding.UTF8.GetBytes(Name + '\0');
            Array.Copy(nameBytes, 0, outData, 16, nameBytes.Length);
            return 17 + nameBytes.Length;
        }

        public static AnnounceMessage Parse(byte[] data)
        {
            if (data.Length < 17)
            {
                throw new ArgumentException("Corrupted data");
            }

            uint ipData = BitConverter.ToUInt32(data, 0);
            var ip = new IPAddress(ipData);

            Array.Reverse(data, 4, 4);
            uint fileCount = BitConverter.ToUInt32(data, 4);

            Array.Reverse(data, 8, 8);
            DateTime timestamp =
                new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(BitConverter.ToUInt64(data, 8));

            int length = 0;
            for (int i = 16; i < data.Length; i++)
            {
                if (data[i] == 0)
                {
                    break;
                }
                length++;
            }
            string name = Encoding.UTF8.GetString(data, 16, length);
            return new AnnounceMessage(name, ip, timestamp, fileCount);
        }
    }
}