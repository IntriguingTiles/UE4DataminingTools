using System;
using System.IO;
using DumperCommon;

namespace PakDumper {
    class Program {
        private static readonly byte[] MAGIC = { 0xE1, 0x12, 0x6F, 0x5A };

        private static int FindInStream(FileStream stream, byte[] find) {
            while (stream.Position < stream.Length) {
                var arr = new byte[find.Length];
                stream.Read(arr, 0, find.Length);

                for (int i = 0; i < find.Length; i++) {
                    if (arr[i] != find[i]) break;
                    if (i == find.Length - 1) return i;
                }

                stream.Seek(-(find.Length - 1), SeekOrigin.Current);
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
            using var fs = pakFile.OpenRead();
            fs.Seek(-500, SeekOrigin.End);
            var magic = FindInStream(fs, MAGIC);
            var dr = new DataStreamReader(fs);
            using var stream = File.Create(pakFile.FullName.Replace(pakFile.Extension, ".txt"));
            using var sw = new StreamWriter(stream);

            if (magic == -1) {
                Console.Error.WriteLine("Couldn't find magic");
                return -1;
            }

            Console.WriteLine("Found magic");

            var version = dr.ReadInt32();
            var indexOffset = dr.ReadInt64();
            var indexSize = dr.ReadInt64();

            Console.WriteLine($"Pak version {version}");
            Console.WriteLine($"Index Offset is 0x{indexOffset:X}");
            Console.WriteLine($"Index Size is {indexSize}");

            dr.stream.Seek(indexOffset, SeekOrigin.Begin);
            dr.ReadString(dr.ReadInt32()); // mount point
            var fileCount = dr.ReadInt32();

            Console.WriteLine($"{fileCount} files");
            sw.WriteLine($"{fileCount} files");

            var entries = new PakEntry[fileCount];

            for (int i = 0; i < fileCount; i++) {
                var filename = dr.ReadString(dr.ReadInt32());
                dr.ReadInt64(); // pos
                dr.ReadInt64(); // compressed size
                var uncompressedSize = dr.ReadInt64();
                int compressionIndex = 0;

                if (version == 8) {
                    compressionIndex = dr.ReadByte();
                } else {
                    compressionIndex = dr.ReadInt32();
                }

                var hash = dr.ReadArray(20);

                if (compressionIndex != 0) {
                    var blockCount = dr.ReadInt32();
                    dr.stream.Seek(16 * blockCount, SeekOrigin.Current);
                }

                dr.stream.Seek(5, SeekOrigin.Current);
                entries[i] = new PakEntry(filename, hash, uncompressedSize);
            }

            Array.Sort(entries, (a, b) => a.Name.CompareTo(b.Name));

            foreach (var entry in entries) {
                sw.WriteLine($"{entry.Name} hash:{Convert.ToHexString(entry.Hash)} size:{entry.Size}");
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
