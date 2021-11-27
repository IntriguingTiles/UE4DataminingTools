using System;
using System.Collections.Generic;
using System.IO;
using DumperCommon;

namespace LocresDumper {
    class Program {
        private static readonly Guid headerGuid = new Guid("{7574140E-4A67-FC03-4A15-909DC3377F1B}");

        static int Main(string[] args) {
            if (args.Length < 1) {
                Console.Error.WriteLine("Give me a locres!");
                return -1;
            }

            if (!File.Exists(args[0])) {
                Console.Error.WriteLine("File doesn't exist");
                return -1;
            }

            var locFile = new FileInfo(args[0]);
            var data = File.ReadAllBytes(args[0]);
            var dr = new DataReader(data);
            using var stream = File.Create(locFile.FullName.Replace(locFile.Extension, ".txt"));
            using var sr = new StreamWriter(stream);

            var readGuid = dr.ReadGuid();

            if (readGuid != headerGuid) {
                Console.Error.WriteLine("Invalid locres");
                return -1;
            }

            var version = dr.ReadByte();

            Console.WriteLine($"Locres version {version}");

            var valueOffset = dr.ReadInt64();

            if (version >= 2) {
                var entriesCount = dr.ReadInt32();
                Console.WriteLine($"{entriesCount} entries");
                sr.WriteLine($"{entriesCount} entries");
            }

            var namespaceCount = dr.ReadInt32();
            Console.WriteLine($"{namespaceCount} namespaces");
            sr.WriteLine($"{namespaceCount} namespaces");

            var oldPtr = dr.ptr;
            dr.ptr = (int)valueOffset;
            var valueCount = dr.ReadInt32();
            string[] values = new string[valueCount];

            Console.WriteLine($"{valueCount} values");
            sr.WriteLine($"{valueCount} values");

            for (int i = 0; i < valueCount; i++) {
                values[i] = dr.ReadString(dr.ReadInt32());

                if (version >= 2) dr.ReadInt32(); // refcount
            }

            dr.ptr = oldPtr;

            for (int i = 0; i < namespaceCount; i++) {
                if (version >= 2) dr.ReadInt32(); // hash
                var ns = dr.ReadString(dr.ReadInt32());
                var keyCount = dr.ReadInt32();
                sr.WriteLine($"\nNamespace {(ns.Length == 0 ? "Global" : ns)} ({keyCount} keys)");

                for (int j = 0; j < keyCount; j++) {
                    if (version >= 2) dr.ReadInt32(); // hash
                    var key = dr.ReadString(dr.ReadInt32());
                    dr.ReadInt32(); // source string hash
                    var index = dr.ReadInt32();
                    sr.WriteLine($"\t{key} = \"{values[index]}\"");
                }
            }

            return 0;
        }
    }
}
