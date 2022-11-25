using DumperCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AssetDumper {
    class Program {
        static string[] names;
        static DataReader drE;
        static StreamWriter sw;
        static ObjectImport[] imports;
        static ObjectExport[] exports;

        static int Main(string[] args) {
            if (args.Length < 1) {
                Console.Error.WriteLine("Give me an asset!");
                return -1;
            }

            if (!File.Exists(args[0])) {
                Console.Error.WriteLine("File doesn't exist");
                return -1;
            }

            var assetFile = new FileInfo(args[0]);
            var data = File.ReadAllBytes(args[0]);
            var dataE = File.Exists(assetFile.FullName.Replace(assetFile.Extension, ".uexp")) ? File.ReadAllBytes(assetFile.FullName.Replace(assetFile.Extension, ".uexp")) : null;
            var dr = new DataReader(data);
            drE = dataE != null ? new DataReader(dataE) : null;
            using var stream = File.Create(assetFile.FullName.Replace(assetFile.Extension, ".txt"));
            using var _sw = new StreamWriter(stream);
            sw = _sw;

            var magic = dr.ReadUInt32();

            if (magic != 0x9E2A83C1) {
                Console.Error.WriteLine("Not an asset");
                return -1;
            }

            Console.WriteLine("Found magic");

            var legacyVersion = dr.ReadInt32();

            Debug.Assert(legacyVersion == -7, "Expected legacy version to be -7.");

            if (legacyVersion >= 0) {
                Console.Error.WriteLine("Old assets aren't supported");
                return -1;
            }

            if (legacyVersion != -4) {
                // legacy ue3 version
                Console.WriteLine($"UE3 version is {dr.ReadInt32()}");
            }

            // File version
            Console.WriteLine($"File version is {dr.ReadInt32()}");
            // File version licensee
            Console.WriteLine($"File version licensee is {dr.ReadInt32()}");

            // custom versions
            Console.WriteLine($"Custom version count is {dr.ReadInt32()}");

            // total header size
            Console.WriteLine($"Total header size is {dr.ReadInt32()}");

            // package group
            Console.WriteLine($"Package Group is {dr.ReadString(dr.ReadInt32())}");

            // package flags
            uint packageFlags = dr.ReadUInt32();
            Console.WriteLine($"Package flags are 0x{packageFlags:X}");

            Debug.Assert((packageFlags & 0x80000000) != 0, "Expected package to have editor-only data filtered out.");

            int nameCount = dr.ReadInt32();
            Console.WriteLine($"Name count is {nameCount}");

            int nameOffset = dr.ReadInt32();
            Console.WriteLine($"Name offset is 0x{nameOffset:X}");

            // gatherable data count
            Console.WriteLine($"gather {dr.ReadInt32()}");
            // gatherable data offset
            Console.WriteLine($"gather 0x{dr.ReadInt32():X}");

            int exportCount = dr.ReadInt32();
            Console.WriteLine($"export count is {exportCount}");

            int exportOffset = dr.ReadInt32();
            Console.WriteLine($"export offset is 0x{exportOffset:X}");

            int importCount = dr.ReadInt32();
            Console.WriteLine($"import count is {importCount}");

            int importOffset = dr.ReadInt32();
            Console.WriteLine($"import offset is 0x{importOffset:X}");

            // depends offset
            Console.WriteLine($"depends 0x{dr.ReadInt32():X}");

            // package ref count
            Console.WriteLine($"package ref {dr.ReadInt32()}");
            // package ref offset
            Console.WriteLine($"package ref 0x{dr.ReadInt32():X}");

            // searchable names offset
            Console.WriteLine($"searchable 0x{dr.ReadInt32():X}");

            // thumbnail offset
            Console.WriteLine($"thumbnail 0x{dr.ReadInt32():X}");

            // guid
            Console.WriteLine($"guid {dr.ReadGuid()}");

            // generation count
            int genCount = dr.ReadInt32();
            Console.WriteLine($"generation count {genCount}");

            for (int i = 0; i < genCount; i++) {
                // generations have export counts and name counts
                Console.WriteLine($"gen {i}: {dr.ReadInt32()}, {dr.ReadInt32()}");
            }

            // saved with version
            Console.WriteLine($"major {dr.ReadUInt16()}");
            Console.WriteLine($"minor {dr.ReadUInt16()}");
            Console.WriteLine($"patch {dr.ReadUInt16()}");
            Console.WriteLine($"changelist {dr.ReadInt32()}");
            Console.WriteLine($"branch {dr.ReadString(dr.ReadInt32())}");

            // compatible engine version
            Console.WriteLine($"major {dr.ReadUInt16()}");
            Console.WriteLine($"minor {dr.ReadUInt16()}");
            Console.WriteLine($"patch {dr.ReadUInt16()}");
            Console.WriteLine($"changelist {dr.ReadInt32()}");
            Console.WriteLine($"branch {dr.ReadString(dr.ReadInt32())}");

            int compressionFlags = dr.ReadInt32();
            Console.WriteLine($"compression flags are 0x{compressionFlags:X}");
            Debug.Assert(compressionFlags == 0, "Expected package to not be compressed.");

            Console.WriteLine($"compressed count {dr.ReadInt32()}");
            Console.WriteLine($"package source 0x{dr.ReadInt32():X}");

            int additionalPackagesCount = dr.ReadInt32();
            Console.WriteLine($"additional packages to cook {additionalPackagesCount}");

            for (int i = 0; i < additionalPackagesCount; i++) {
                Console.WriteLine(dr.ReadString(dr.ReadInt32()));
            }

            Console.WriteLine($"asset registry offset 0x{dr.ReadInt32():X}");
            Console.WriteLine($"bulk data offset 0x{dr.ReadInt64():X}");
            Console.WriteLine($"world tile offset 0x{dr.ReadInt32():X}");

            int chunkCount = dr.ReadInt32();
            Console.WriteLine($"chunk count is {chunkCount}");

            for (int i = 0; i < chunkCount; i++) {
                Console.WriteLine(dr.ReadInt32());
            }

            Console.WriteLine($"preload count {dr.ReadInt32()}");
            Console.WriteLine($"preload offset 0x{dr.ReadInt32():X}");

            Debug.Assert(dr.ptr == nameOffset, "Expected cursor to be at the name offset");

            names = new string[nameCount];
            sw.WriteLine("Name List:");

            for (int i = 0; i < nameCount; i++) {
                names[i] = dr.ReadString(dr.ReadInt32());
                sw.WriteLine($"\t{names[i]}");
                dr.ReadInt32(); // hash
            }

            sw.WriteLine();

            Debug.Assert(dr.ptr == importOffset, "Expected cursor to be at the import offset");

            imports = new ObjectImport[importCount];
            string[][] tableData = new string[importCount][];

            for (int i = 0; i < importCount; i++) {
                var import = new ObjectImport();

                import.Index = -(i + 1);
                import.ClassPackage = new NameReference(dr.ReadInt32(), dr.ReadInt32());
                import.ClassName = new NameReference(dr.ReadInt32(), dr.ReadInt32());
                import.OuterIndex = dr.ReadInt32();
                import.ObjectName = new NameReference(dr.ReadInt32(), dr.ReadInt32());
                imports[i] = import;
                tableData[i] = import.ToStringArray();
            }

            Console.WriteLine(Table(new string[] { "Index", "Package", "Type", "Outer Index", "Object Name", "Number" }, tableData));
            sw.WriteLine("Imports:");
            sw.WriteLine(Table(new string[] { "Index", "Package", "Type", "Outer Index", "Object Name", "Number" }, tableData));

            Debug.Assert(dr.ptr == exportOffset, "Expected cursor to be at the export offset");

            exports = new ObjectExport[exportCount];
            tableData = new string[exportCount][];

            for (int i = 0; i < exportCount; i++) {
                Console.WriteLine($"at 0x{dr.ptr:X}");
                var export = new ObjectExport();

                export.ClassIndex = dr.ReadInt32();
                export.SuperIndex = dr.ReadInt32();
                export.TemplateIndex = dr.ReadInt32();
                export.OuterIndex = dr.ReadInt32();
                export.ObjectName = new NameReference(dr.ReadInt32(), dr.ReadInt32());
                export.ObjectFlags = dr.ReadInt32();
                export.SerialSize = dr.ReadInt64();
                export.SerialOffset = dr.ReadInt64();
                export.bForcedExport = dr.ReadByte();
                export.bNotForClient = dr.ReadByte();
                export.bNotForServer = dr.ReadByte();
                export.PackageGuid = dr.ReadGuid();
                export.PackageFlags = dr.ReadUInt32();
                export.bNotAlwaysLoadedForEditorGame = dr.ReadByte();
                export.bIsAsset = dr.ReadByte();
                export.FirstExportDependency = dr.ReadInt32();
                export.SerializationBeforeSerializationDependencies = dr.ReadInt32();
                export.CreateBeforeSerializationDependencies = dr.ReadInt32();
                export.SerializationBeforeCreateDependencies = dr.ReadInt32();
                export.CreateBeforeCreateDependencies = dr.ReadInt32();

                exports[i] = export;
                tableData[i] = new string[] { IndexToString(export.ClassIndex), IndexToString(export.SuperIndex), IndexToString(export.TemplateIndex), export.ObjectName.ToString() };
                dr.ptr += 15; // mysterious data
            }

            Console.WriteLine(Table(new string[] { "Class", "Parent", "Template", "Object Name" }, tableData));
            sw.WriteLine("Exports:");
            sw.WriteLine(Table(new string[] { "Class", "Parent", "Template", "Object Name" }, tableData));

            if (drE != null) {
                for (int i = 0; i < exportCount; i++) {
                    Console.WriteLine($"\nExport {exports[i].ObjectName}:");
                    sw.WriteLine($"\nExport {exports[i].ObjectName}:");

                    sw.Flush();

                    var prevPtr = drE.ptr;
                    var prevPos = sw.BaseStream.Position;

#if DEBUG
                    while (ProcessProperty()) { }
#else
                    try {
                        while (ProcessProperty()) { }
                    } catch (Exception e) {
                        sw.WriteLine($"**WARNING** Exception occured while parsing export {exports[i].ObjectName} (index {i}), skipping to next export");
                        sw.WriteLine(e);
                        Console.Error.WriteLine($"**WARNING** Exception occured while parsing export {exports[i].ObjectName} (index {i}), skipping to next export");
                        Console.Error.WriteLine(e);
                    }
#endif

                    sw.Flush();

                    if (prevPos == sw.BaseStream.Position) {
                        Console.WriteLine("<empty>");
                        sw.WriteLine("<empty>");
                    }

                    // mysterious data
                    drE.ptr = prevPtr + (int)exports[i].SerialSize;
                }
            }

            Console.WriteLine($"at 0x{dr.ptr:X}");

            sw.Close();
            sw.Dispose();

            return 0;
        }

        private static void WriteEntry(int depth, string type, string name, object val, bool valIsType = false) {
            Console.WriteLine($"{"".PadLeft(depth * 4, '-')}[{type}] {name} = {(valIsType ? $"[{val}]" : val)}");
            sw.WriteLine($"{"".PadLeft(depth * 4, '-')}[{type}] {name} = {(valIsType ? $"[{val}]" : val)}");
        }

        private static bool ProcessProperty(int depth = 0, string nameOverride = null, string typeOverride = null, bool readLength = true, bool readIndex = true, bool skipByte = true) {
            Console.WriteLine($"at 0x{drE.ptr:X}\n");
            // did we reach the end of the file?
            if (drE.PeekUInt32() == 0x9E2A83C1 && drE.ptr == drE.data.Length - 4) return false;

            var prop = new Property {
                Name = nameOverride ?? new NameReference(drE.ReadInt32(), drE.ReadInt32()).ToString()
            };

            Console.WriteLine(prop.Name);
            if (prop.Name == "None") return false;

            prop.Type = typeOverride ?? new NameReference(drE.ReadInt32(), drE.ReadInt32()).ToString();
            prop.Length = readLength ? drE.ReadInt32() : 0;
            prop.ArrayIndex = readIndex ? drE.ReadInt32() : 0;  // array index
            Console.WriteLine($"array index is {prop.ArrayIndex}");
            Console.WriteLine($"length is {prop.Length}");
            Console.WriteLine($"at 0x{drE.ptr:X}");
            Console.WriteLine($"ends at 0x{drE.ptr + prop.Length + 1:X}");

            switch (prop.Type) {
                case "TextProperty":
                    if (skipByte) drE.ptr++; // probably
                    var textFlags = drE.ReadInt32();
                    Console.WriteLine($"flags are 0x{textFlags:X}");
                    var historyFlags = (TextHistoryType)drE.ReadSByte();
                    Console.WriteLine($"history flags are 0x{historyFlags:X}");

                    //TODO: clean this up

                    if ((textFlags & (int)TextFlags.CultureInvariant) != 0) {
                        // claims to be bool but is actually int32?
                        var hasCultureInvariantString = drE.ReadInt32();

                        if (hasCultureInvariantString != 0) {
                            WriteEntry(depth, prop.Type, prop.Name, $"\"{drE.ReadString(drE.ReadInt32())}\"");
                        }
                    } else {
                        if (historyFlags == TextHistoryType.StringTableEntry) {
                            var tableID = new NameReference(drE.ReadInt32(), drE.ReadInt32());
                            var key = drE.ReadString(drE.ReadInt32());
                            WriteEntry(depth, prop.Type, prop.Name, $"{tableID}.{key}");
                        } else {
                            var ns = drE.ReadString(drE.ReadInt32());
                            var key = drE.ReadString(drE.ReadInt32());
                            var sourceString = drE.ReadString(drE.ReadInt32());
                            WriteEntry(depth, prop.Type, prop.Name, $"{(ns.Length == 0 ? "" : $"{{{ns}}} ")}\"{sourceString}\"");
                        }
                    }
                    break;
                case "StrProperty":
                    if (skipByte) drE.ptr++; // probably
                    WriteEntry(depth, prop.Type, prop.Name, $"\"{drE.ReadString(drE.ReadInt32())}\"");
                    break;
                case "NameProperty":
                    if (skipByte) drE.ptr++; // probably
                    WriteEntry(depth, prop.Type, prop.Name, $"\"{new NameReference(drE.ReadInt32(), drE.ReadInt32())}\"");
                    break;
                case "ObjectProperty":
                    if (skipByte) drE.ptr++;
                    var isType = drE.PeekInt32() < 0;
                    WriteEntry(depth, prop.Type, prop.Name, isType ? imports[-1 - drE.ReadInt32()].ObjectName : drE.ReadInt32(), isType);
                    break;
                case "SoftObjectProperty":
                    if (skipByte) drE.ptr++;
                    WriteEntry(depth, prop.Type, prop.Name, new NameReference(drE.ReadInt32(), drE.ReadInt32()), true);
                    drE.ptr += 4; // u32
                    break;
                case "FloatProperty":
                    if (skipByte) drE.ptr++;
                    WriteEntry(depth, prop.Type, prop.Name, drE.ReadFloat());
                    break;
                case "IntProperty":
                    if (skipByte) drE.ptr++;
                    WriteEntry(depth, prop.Type, prop.Name, drE.ReadInt32());
                    break;
                case "UInt32Property":
                    if (skipByte) drE.ptr++;
                    WriteEntry(depth, prop.Type, prop.Name, drE.ReadUInt32());
                    break;
                case "UInt16Property":
                    if (skipByte) drE.ptr++;
                    WriteEntry(depth, prop.Type, prop.Name, drE.ReadUInt16());
                    break;
                case "BoolProperty":
                    if (skipByte) drE.ptr++;
                    WriteEntry(depth, prop.Type, prop.Name, drE.ReadByte() > 0 ? "true" : "false");
                    break;
                case "InterfaceProperty":
                    // this might not be correct
                    if (skipByte) drE.ptr++;
                    WriteEntry(depth, prop.Type, prop.Name, IndexToString(drE.ReadInt32()), true);
                    break;
                case "EnumProperty": {
                        var enumType = new NameReference(drE.ReadInt32(), drE.ReadInt32());
                        if (skipByte) drE.ptr++; // probably
                        var enumName = new NameReference(drE.ReadInt32(), drE.ReadInt32());
                        WriteEntry(depth, prop.Type, prop.Name, $"[{enumType}] {enumName}");
                        break;
                    }
                case "ByteProperty":
                    // this is probably very incorrect
                    Debug.Assert(prop.Length == 8 || prop.Length == 0 || prop.Length == 1, "Expected ByteProperty length to be 0, 1, or 8");

                    if (prop.Length == 8) {
                        var enumType = new NameReference(drE.ReadInt32(), drE.ReadInt32());
                        drE.ptr++;
                        var enumName = new NameReference(drE.ReadInt32(), drE.ReadInt32());
                        WriteEntry(depth, prop.Type, prop.Name, $"[{enumType}] {enumName}");
                    } else if (prop.Length == 1) {
                        // possibly int64
                        var num1 = drE.ReadInt32();
                        drE.ReadInt32();
                        drE.ptr++;
                        var num2 = drE.ReadByte();
                        WriteEntry(depth, prop.Type, prop.Name, $"{num1}, {num2}");
                    } else {
                        WriteEntry(depth, prop.Type, prop.Name, drE.ReadByte());
                    }

                    break;
                case "Vector":
                case "Rotator":
                    if (skipByte) drE.ptr++; // probably
                    WriteEntry(depth, prop.Type, prop.Name, $"({drE.ReadFloat()}, {drE.ReadFloat()}, {drE.ReadFloat()})");
                    break;
                case "Vector2D":
                    if (skipByte) drE.ptr++; // probably
                    WriteEntry(depth, prop.Type, prop.Name, $"({drE.ReadFloat()}, {drE.ReadFloat()})");
                    break;
                case "Vector4":
                case "LinearColor":
                case "Quat":
                    if (skipByte) drE.ptr++; // probably
                    WriteEntry(depth, prop.Type, prop.Name, $"({drE.ReadFloat()}, {drE.ReadFloat()}, {drE.ReadFloat()}, {drE.ReadFloat()})");
                    break;
                case "Guid":
                    if (skipByte) drE.ptr++;
                    WriteEntry(depth, prop.Type, prop.Name, drE.ReadGuid());
                    break;
                case "ColorMaterialInput":
                case "Color":
                    if (skipByte) drE.ptr++; // probably
                    WriteEntry(depth, prop.Type, prop.Name, $"({drE.ReadByte()}, {drE.ReadByte()}, {drE.ReadByte()}, {drE.ReadByte()})");
                    break;
                case "Box":
                    if (skipByte) drE.ptr++; // probably
                    WriteEntry(depth, prop.Type, prop.Name, $"({drE.ReadFloat()}, {drE.ReadFloat()}, {drE.ReadFloat()}), ({drE.ReadFloat()}, {drE.ReadFloat()}, {drE.ReadFloat()})");
                    drE.ptr++; // IsValid
                    break;
                case "IntPoint":
                    if (skipByte) drE.ptr++; // probably
                    WriteEntry(depth, prop.Type, prop.Name, $"({drE.ReadInt32()}, {drE.ReadInt32()})");
                    break;
                case "GameplayTagContainer":
                    if (skipByte) drE.ptr++; // probably
                    var amount = drE.ReadInt32();

                    for (int i = 0; i < amount; i++) {
                        // should this be written as a type?
                        WriteEntry(depth, prop.Type, prop.Name, new NameReference(drE.ReadInt32(), drE.ReadInt32()));
                    }

                    break;
                case "NiagaraDataInterfaceGPUParamInfo":
                    if (skipByte) drE.ptr++; // probably
                    WriteEntry(depth, "StrProperty", "DataInterfaceHLSLSymbol", $"\"{drE.ReadString(drE.ReadInt32())}\"");
                    WriteEntry(depth, "StrProperty", "DIClassName", $"\"{drE.ReadString(drE.ReadInt32())}\"");
                    WriteEntry(depth, "ArrayProperty", "GeneratedFunctions", "NiagaraDataInterfaceGeneratedFunction", true);

                    var count = drE.ReadInt32();

                    for (int i = 0; i < count; i++) {
                        WriteEntry(depth + 1, "NameProperty", "DefinitionName", $"\"{new NameReference(drE.ReadInt32(), drE.ReadInt32())}\"");
                        WriteEntry(depth + 1, "StrProperty", "DataInterfaceHLSLSymbol", $"\"{drE.ReadString(drE.ReadInt32())}\"");
                        // u32 at the end? this might be the specifier values
                        Debug.Assert(drE.ReadInt32() == 0, "Expected final Int32 to be 0");
                    }

                    break;
                case "RichCurveKey":
                    if (skipByte) drE.ptr++; // probably
                    WriteEntry(depth, "ByteProperty", "InterpMode", drE.ReadByte());
                    WriteEntry(depth, "ByteProperty", "TangentMode", drE.ReadByte());
                    WriteEntry(depth, "ByteProperty", "TangentWeightMode", drE.ReadByte());
                    WriteEntry(depth, "FloatProperty", "Time", drE.ReadFloat());
                    WriteEntry(depth, "FloatProperty", "Value", drE.ReadFloat());
                    WriteEntry(depth, "FloatProperty", "ArriveTangent", drE.ReadFloat());
                    WriteEntry(depth, "FloatProperty", "ArriveTangentWeight", drE.ReadFloat());
                    WriteEntry(depth, "FloatProperty", "LeaveTangent", drE.ReadFloat());
                    WriteEntry(depth, "FloatProperty", "LeaveTangentWeight", drE.ReadFloat());
                    break;
                case "MovieSceneFrameRange":
                    if (skipByte) drE.ptr++;
                    WriteEntry(depth, "StructProperty", "LowerBound", "Int32RangeBound", true);
                    WriteEntry(depth + 1, "ByteProperty", "Type", drE.ReadSByte());
                    WriteEntry(depth + 1, "IntProperty", "Value", drE.ReadInt32());
                    WriteEntry(depth, "StructProperty", "UpperBound", "Int32RangeBound", true);
                    WriteEntry(depth + 1, "ByteProperty", "Type", drE.ReadSByte());
                    WriteEntry(depth + 1, "IntProperty", "Value", drE.ReadInt32());
                    break;
                case "MovieSceneFloatValue":
                    if (skipByte) drE.ptr++;
                    WriteEntry(depth, "FloatProperty", "Value", drE.ReadFloat());
                    WriteEntry(depth, "StructProperty", "Tangent", "MovieSceneTangentData", true);
                    WriteEntry(depth + 1, "FloatProperty", "ArriveTangent", drE.ReadFloat());
                    WriteEntry(depth + 1, "FloatProperty", "LeaveTangent", drE.ReadFloat());
                    WriteEntry(depth + 1, "FloatProperty", "ArriveTangentWeight", drE.ReadFloat());
                    WriteEntry(depth + 1, "FloatProperty", "LeaveTangentWeight", drE.ReadFloat());
                    WriteEntry(depth + 1, "ByteProperty", "TangentWeightMode", drE.ReadByte());
                    WriteEntry(depth, "ByteProperty", "InterpMode", drE.ReadByte());
                    WriteEntry(depth, "ByteProperty", "TangentMode", drE.ReadByte());
                    break;
                case "MovieSceneFloatChannel": {
                        // i hate this property
                        if (skipByte) drE.ptr++;
                        WriteEntry(depth, "ByteProperty", "PreInfinityExtrap", drE.ReadSByte());
                        WriteEntry(depth, "ByteProperty", "PostInfinityExtrap", drE.ReadSByte());
                        drE.ReadInt32(); // TimesStructLength
                        var length = drE.ReadInt32();
                        WriteEntry(depth, "ArrayProperty", "Times", "IntProperty", true);

                        for (int i = 0; i < length; i++) {
                            WriteEntry(depth + 1, "IntProperty", "Time", drE.ReadInt32());
                        }

                        drE.ReadInt32(); // ValuesStructLength

                        length = drE.ReadInt32();
                        WriteEntry(depth, "ArrayProperty", "Values", "StructProperty", true);

                        for (int i = 0; i < length; i++) {
                            var old = drE.ptr;
                            WriteEntry(depth + 1, "StructProperty", "Value", "MovieSceneFloatValue", true);
                            ProcessProperty(depth + 2, "", "MovieSceneFloatValue", false, false, false);
                            drE.ReadInt32(); // ????
                        }

                        drE.ReadInt32(); // ????
                        drE.ptr += length - 1; // ????

                        WriteEntry(depth, "FloatProperty", "DefaultValue", drE.ReadFloat());
                        WriteEntry(depth, "BoolProperty", "HasDefaultValue", drE.ReadByte() > 0 ? "true" : "false");

                        WriteEntry(depth, "StructProperty", "TickResolution", "FrameRate", true);
                        WriteEntry(depth + 1, "IntProperty", "Numerator", drE.ReadInt32());
                        WriteEntry(depth + 1, "IntProperty", "Denominator", drE.ReadInt32());
                        break;
                    }
                case "MovieSceneSegment": {
                        if (skipByte) drE.ptr++;
                        WriteEntry(depth, "StructProperty", "Value", "MovieSceneSegment", true);
                        WriteEntry(depth + 1, "StructProperty", "Range", "FrameNumberRange", true);
                        WriteEntry(depth + 2, "StructProperty", "LowerBound", "Int32RangeBound", true);
                        WriteEntry(depth + 3, "ByteProperty", "Type", drE.ReadSByte());
                        WriteEntry(depth + 3, "IntProperty", "Value", drE.ReadInt32());
                        WriteEntry(depth + 2, "StructProperty", "UpperBound", "Int32RangeBound", true);
                        WriteEntry(depth + 3, "ByteProperty", "Type", drE.ReadSByte());
                        WriteEntry(depth + 3, "IntProperty", "Value", drE.ReadInt32());
                        WriteEntry(depth + 1, "IntProperty", "ID", drE.ReadInt32());
                        WriteEntry(depth + 1, "BoolProperty", "AllowEmpty", drE.ReadInt32() > 0 ? "true" : "false");
                        var length = drE.ReadInt32();
                        WriteEntry(depth + 1, "ArrayProperty", "Impls", "StructProperty", true);
                        for (int i = 0; i < length; i++) {
                            WriteEntry(depth + 2, "StructProperty", "Data", "SectionEvaluationData", true);
                            while (ProcessProperty(depth + 3)) { }
                        }
                        break;
                    }
                case "SetProperty":
                    typeOverride = new NameReference(drE.ReadInt32(), drE.ReadInt32()).ToString();
                    WriteEntry(depth, prop.Type, prop.Name, typeOverride, true);
                    Console.WriteLine($"{drE.ptr:X}");
                    var NumElementsToRemove = drE.ReadInt32();
                    drE.ptr++;
                    Console.WriteLine($"{drE.ptr:X}");
                    Debug.Assert(NumElementsToRemove == 0, "Expected NumElementsToRemove to be 0");
                    var NumElements = drE.ReadInt32();

                    for (int i = 0; i < NumElements; i++) {
                        ProcessProperty(depth + 1, prop.Name, typeOverride, false, false, false);
                    }

                    break;
                case "MapProperty":
                    var keyType = new NameReference(drE.ReadInt32(), drE.ReadInt32()).ToString();
                    var valueType = new NameReference(drE.ReadInt32(), drE.ReadInt32()).ToString();
                    WriteEntry(depth, prop.Type, prop.Name, $"([{keyType}], [{valueType}])");
                    drE.ptr++; // complete guess
                    var NumKeysToRemove = drE.ReadInt32();
                    Debug.Assert(NumKeysToRemove == 0, "Expected NumKeysToRemove to be 0");
                    var NumEntries = drE.ReadInt32();

                    for (int i = 0; i < NumEntries; i++) {
                        // key
                        if (keyType == "StructProperty") {
                            // mystery i32?
                            drE.ReadInt32();
                            ProcessProperty(depth + 1);
                        } else {
                            ProcessProperty(depth + 1, prop.Name, keyType, false, false, false);
                        }

                        // value
                        if (valueType == "StructProperty") {
                            ProcessProperty(depth + 1);
                        } else {
                            ProcessProperty(depth + 1, prop.Name, valueType, false, false, false);
                        }
                    }
                    break;
                case "ArrayProperty":
                    typeOverride = new NameReference(drE.ReadInt32(), drE.ReadInt32()).ToString();
                    nameOverride = prop.Name;
                    drE.ptr++;
                    var arrayLength = drE.ReadInt32();
                    var structProp = new Property();
                    string structInnerType = null;
                    Console.WriteLine($"num vals: {arrayLength}");

                    WriteEntry(depth, prop.Type, prop.Name, typeOverride, true);

                    if (typeOverride == "StructProperty") {
                        structProp.Name = new NameReference(drE.ReadInt32(), drE.ReadInt32()).ToString();
                        structProp.Type = new NameReference(drE.ReadInt32(), drE.ReadInt32()).ToString();
                        structProp.Length = drE.ReadInt32();
                        structProp.ArrayIndex = drE.ReadInt32();
                        structInnerType = new NameReference(drE.ReadInt32(), drE.ReadInt32()).ToString();
                        drE.ptr += 17; // u64 * 2 + 1
                    }

                    for (int i = 0; i < arrayLength; i++) {
                        if (typeOverride == "StructProperty") {
                            WriteEntry(depth + 1, structProp.Type, structProp.Name, structInnerType, true);

                            // gotta love unreal engine
                            switch (structInnerType) {
                                case "Color":
                                case "NiagaraDataInterfaceGPUParamInfo":
                                case "Vector":
                                case "RichCurveKey":
                                case "MovieSceneSegment":
                                    ProcessProperty(depth + 2, "<no-name>", structInnerType, false, false, false);
                                    break;
                                default:
                                    while (ProcessProperty(depth + 2)) { };
                                    break;
                            }
                        } else {
                            ProcessProperty(depth + 1, nameOverride, typeOverride, false, false, false);
                        }
                    }

                    break;
                case "StructProperty":
                    var innerType = new NameReference(drE.ReadInt32(), drE.ReadInt32()).ToString();
                    WriteEntry(depth, prop.Type, prop.Name, innerType, true);
                    drE.ptr += 16; // u64 * 2
                    if (skipByte) drE.ptr++;

                    // hacks for specific inner types
                    switch (innerType) {
                        case "Guid":
                        case "Vector":
                        case "Vector2D":
                        case "Vector4":
                        case "Color":
                        case "LinearColor":
                        case "GameplayTagContainer":
                        case "Box":
                        case "Rotator":
                        case "Quat":
                        case "IntPoint":
                        case "ColorMaterialInput":
                        case "MovieSceneFrameRange":
                        case "MovieSceneFloatChannel":
                        case "FrameNumber":
                            ProcessProperty(depth + 1, "<no-name>", innerType, false, false, false);
                            break;
                        default:
                            while (ProcessProperty(depth + 1)) { }
                            break;
                    }
                    break;
                default:
                    throw new NotImplementedException($"Unknown property type {prop.Type}");
            }

            return true;
        }

        private static string Table(string[] columns, string[][] data) {
            var sizes = new int[columns.Length];

            for (int i = 0; i < data.Length; i++) {
                for (int j = 0; j < columns.Length; j++) {
                    if (data[i][j].Length > sizes[j]) sizes[j] = data[i][j].Length;
                    if (columns[j].Length > sizes[j]) sizes[j] = columns[j].Length;
                }
            }

            string ret = "";

            for (int i = 0; i < columns.Length; i++) {
                ret += columns[i].PadRight(sizes[i]);
                if (i != columns.Length - 1) ret += " | ";
            }

            ret += $"\n{"".PadLeft(ret.Length, '-')}\n";

            for (int i = 0; i < data.Length; i++) {
                for (int j = 0; j < columns.Length; j++) {
                    ret += data[i][j].PadRight(sizes[j]);
                    if (j != columns.Length - 1) ret += " | ";
                }
                ret += "\n";
            }

            return ret;
        }

        private static string IndexToString(int index) {
            if (index < 0) return imports[-1 - index].ObjectName.ToString();
            else if (index > 0) return exports[index - 1].ObjectName.ToString();
            else return "0";
        }

        internal struct NameReference {
            public int Index, Number;

            public NameReference(int index, int number) {
                Index = index;
                Number = number;
            }

            public override string ToString() {
                return names[Index];
            }
        }

        internal struct ObjectImport {
            public NameReference ClassPackage, ClassName, ObjectName;
            public int Index, OuterIndex;

            public override string ToString() {
                return $"{Index} {ClassPackage} {ClassName} {OuterIndex} {ObjectName}";
            }

            public string[] ToStringArray() {
                return new string[] { Index.ToString(), ClassPackage.ToString(), ClassName.ToString(), OuterIndex.ToString(), ObjectName.ToString(), ObjectName.Number.ToString() };
            }
        }

        internal struct ObjectExport {
            public int ClassIndex, SuperIndex, TemplateIndex, ObjectFlags;
            public long SerialSize, SerialOffset;
            public byte bForcedExport, bNotForClient, bNotForServer, bNotAlwaysLoadedForEditorGame, bIsAsset;
            public Guid PackageGuid;
            public uint PackageFlags;
            public int FirstExportDependency, SerializationBeforeSerializationDependencies, CreateBeforeSerializationDependencies, SerializationBeforeCreateDependencies, CreateBeforeCreateDependencies, OuterIndex;
            public NameReference ObjectName;
        }

        internal struct Property {
            public string Name, Type;
            public int Length, ArrayIndex;
        }

        internal enum TextFlags {
            Transient = (1 << 0),
            CultureInvariant = (1 << 1),
            ConvertedProperty = (1 << 2),
            Immutable = (1 << 3),
            InitializedFromString = (1 << 4)
        }

        internal enum TextHistoryType : sbyte {
            None = -1,
            Base = 0,
            NamedFormat,
            OrderedFormat,
            ArgumentFormat,
            AsNumber,
            AsPercent,
            AsCurrency,
            AsDate,
            AsTime,
            AsDateTime,
            Transform,
            StringTableEntry,
            TextGenerator,
        }
    }

}
