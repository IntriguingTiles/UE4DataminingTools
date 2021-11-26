using System;
using System.IO;
using DumperCommon;

namespace PakDumper {
    class Program {
        private static readonly byte[] MAGIC = { 0xE1, 0x12, 0x6F, 0x5A };

        private static int RFindInArray(byte[] arr, byte[] find) {
            for (int i = arr.Length - find.Length - 1; i >= 0; i--) {
                for (int j = 0; j < find.Length; j++) {
                    if (arr[i + j] != find[j]) break;
                    if (j == find.Length - 1) return i;
                }
            }

            return -1;
        }

        static int Main(string[] args) {
            if (args.Length < 1) {
                Console.Error.WriteLine("Give me a pak!");
                return -1;
            }

            if (!File.Exists(args[0])) {
                Console.Error.WriteLine("File doesn't exist");
                return -1;
            }

            var pakFile = new FileInfo(args[0]);
            var data = File.ReadAllBytes(args[0]);
            var dr = new DataReader(data);
            var magic = RFindInArray(data, MAGIC);
            using var stream = File.Create(pakFile.FullName.Replace(pakFile.Extension, ".txt"));
            using var sr = new StreamWriter(stream);

            if (magic == -1) {
                Console.Error.WriteLine("Couldn't find magic");
                return -1;
            }

            Console.WriteLine($"Found magic at 0x{magic:X}");

            dr.ptr = magic + 4;
            var version = dr.ReadInt32();
            int indexOffset = (int)dr.ReadInt64();
            int indexSize = (int)dr.ReadInt64();

            Console.WriteLine($"Pak version {version}");
            Console.WriteLine($"Index Offset is 0x{indexOffset:X}");
            Console.WriteLine($"Index Size is {indexSize}");

            dr.ptr = indexOffset;
            dr.ReadString(dr.ReadInt32()); // mount point
            var fileCount = dr.ReadInt32();

            Console.WriteLine($"{fileCount} files");
            sr.WriteLine($"{fileCount} files");

            var entries = new PakEntry[fileCount];

            for (int i = 0; i < fileCount; i++) {
                var filename = dr.ReadString(dr.ReadInt32());
                dr.ReadInt64(); // pos
                dr.ReadInt64(); // compressed size
                var uncompressedSize = dr.ReadInt64();
                var compressionIndex = dr.ReadInt32();
                var hash = dr.ReadArray(20);

                if (compressionIndex != 0) {
                    var blockCount = dr.ReadInt32();
                    dr.ptr += 16 * blockCount;
                }

                dr.ptr += 5;
                entries[i] = new PakEntry(filename, hash, uncompressedSize);
            }

            Array.Sort(entries, (a, b) => a.Name.CompareTo(b.Name));

            foreach (var entry in entries) {
                sr.WriteLine($"{entry.Name} hash:{Convert.ToHexString(entry.Hash)} size:{entry.Size}");
            }

            Console.WriteLine("Info dumped to file");

            return 0;
        }
    }

    internal struct PakEntry {
        public string Name;
        public byte[] Hash;
        public long Size;

        public PakEntry(string Name, byte[] Hash, long Size) {
            this.Name = Name;
            this.Hash = Hash;
            this.Size = Size;
        }
    }
}
