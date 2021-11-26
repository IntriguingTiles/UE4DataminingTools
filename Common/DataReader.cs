using System;
using System.Text;

namespace DumperCommon {
    internal class DataReader {
        public int ptr = 0;
        public byte[] data;

        public DataReader(byte[] data) {
            this.data = data;
        }

        public int ReadInt32() {
            var ret = BitConverter.ToInt32(data, ptr);
            ptr += 4;
            return ret;
        }

        public long ReadInt64() {
            var ret = BitConverter.ToInt64(data, ptr);
            ptr += 8;
            return ret;
        }

        public string ReadString(int len) {
            if (len < 0) {
                // UCS2
                len = -len;
                var ret = Encoding.Unicode.GetString(data, ptr, (len - 1) * 2);
                ptr += len * 2;
                return ret;
            } else if (len > 0) {
                var ret = Encoding.ASCII.GetString(data, ptr, len - 1);
                ptr += len;
                return ret;
            } else {
                return "";
            }
        }

        public byte[] ReadArray(int len) {
            var ret = data[ptr..(ptr + len)];
            ptr += len;
            return ret;
        }

        public Guid ReadGuid() {
            var ret = new Guid(data[ptr..(ptr + 16)]);
            ptr += 16;
            return ret;
        }

        public byte ReadByte() {
            var ret = data[ptr];
            ptr++;
            return ret;
        }
    }
}
