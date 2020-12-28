using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Filesystem.Ntfs;
using System.Linq;
using CommandLine;
using GroBuf;
using GroBuf.DataMembersExtracters;

namespace find_duplicate_files_net
{
    public class Program
    {
        internal static Stopwatch StopWatch = new Stopwatch();

        internal static Serializer ResultSerializer =>
            new Serializer(new PropertiesExtractor(), options: GroBufOptions.WriteEmptyObjects);

        private static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<CommandLineOptions>(args)
                    .WithParsed(RunOptions);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                ExitPrompt();
            }
        }

        private static void RunOptions(CommandLineOptions opts)
        {
            var drive = opts.DriveLetter.ToString();

            StopWatch = new Stopwatch();
            StopWatch.Start();

            var duplicatesAfterChecksum = !opts.Read ? ScanDrive(drive) : ReadSaveFile(opts.SaveFile);

            StopWatch.Stop();
            var elapsedMs = StopWatch.ElapsedMilliseconds;

            var totalDuplicates = 0;
            ulong totalSize = 0;
            foreach (var duplicate in duplicatesAfterChecksum)
            {
                var count = duplicate.Value.Count - 1;
                var size = duplicate.Value.First().Size * Convert.ToUInt64(count);
                totalDuplicates += count;
                totalSize += size;
            }

            Console.WriteLine("");
            if (opts.Read) Console.WriteLine("Read from save file");
            Console.WriteLine("Time taken: {0} ms", elapsedMs);
            Console.WriteLine("Duplicate files: {0}", duplicatesAfterChecksum.Count);
            Console.WriteLine("Duplicates: {0}", totalDuplicates);
            Console.WriteLine("Duplicates size: {0:F} mb", totalSize / Math.Pow(1000, 2));

            if (!opts.Save) return;
            CreateSaveFile(opts.SaveFile, duplicatesAfterChecksum);
            Console.WriteLine("Created save file");
        }

        private static Dictionary<string, List<FoundFile>> ReadSaveFile(string saveFile)
        {
            if (string.IsNullOrWhiteSpace(saveFile))
                throw new Exception("Save file was undefined");
            if (!File.Exists(saveFile))
                throw new Exception("Save file not found");
            byte[] binaryData;
            using (var reader = new BinaryReader(File.OpenRead(saveFile)))
            {
                const int bufferSize = 4096;
                using (var ms = new MemoryStream())
                {
                    var buffer = new byte[bufferSize];
                    int count;
                    while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                        ms.Write(buffer, 0, count);
                    binaryData = ms.ToArray();
                }
            }

            var serializer = ResultSerializer;
            return serializer.Deserialize<Dictionary<string, List<FoundFile>>>(binaryData);
        }

        private static Dictionary<string, List<FoundFile>> ScanDrive(string drive)
        {
            var driveInfo = new DriveInfo(drive);
            var ntfsReader = new NtfsReader(driveInfo, RetrieveMode.StandardInformations);
            var fileHandler = new FileHandler(ntfsReader, driveInfo);

            var duplicatesAfterChecksum = fileHandler.GetDuplicateFiles();
            return duplicatesAfterChecksum;
        }

        private static void CreateSaveFile(string saveFile, Dictionary<string, List<FoundFile>> duplicates)
        {
            if (!duplicates.Any() || string.IsNullOrWhiteSpace(saveFile))
                return;
            var serializer = ResultSerializer;
            var binaryData = serializer.Serialize(duplicates);
            using (var writer = new BinaryWriter(File.Open(saveFile, FileMode.Create)))
            {
                writer.Write(binaryData);
            }
        }

        private static void ExitPrompt()
        {
            Console.WriteLine("");
            Console.WriteLine("press any key to exit");
            Console.ReadKey();
        }
    }
}