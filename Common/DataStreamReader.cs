using System;
using System.IO;
using System.Text;

namespace DumperCommon {
    internal class DataStreamReader {
        public FileStream stream;

        public DataStreamReader(FileStream stream) {
            this.stream = stream;
        }

        public int ReadInt32() {
            return BitConverter.ToInt32(ReadArray(4));
        }

        public long ReadInt64() {
            return BitConverter.ToInt64(ReadArray(8));
        }

        public string ReadString(int len) {
            if (len < 0) {
                // UCS2
                len = -len;
                return Encoding.Unicode.GetString(ReadArray(len * 2), 0, (len - 1) * 2);
            } else if (len > 0) {
                return Encoding.ASCII.GetString(ReadArray(len), 0, len - 1);
            } else {
                return "";
            }
        }

        public byte[] ReadArray(int len) {
            byte[] ret = new byte[len];
            stream.Read(ret, 0, len);
            return ret;
        }

        public Guid ReadGuid() {
            return new Guid(ReadArray(16));
        }

        public byte ReadByte() {
            return ReadArray(1)[0];
        }
    }
}
