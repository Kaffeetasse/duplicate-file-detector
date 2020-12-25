﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Filesystem.Ntfs;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace find_duplicate_files_net
{
    internal class Program
    {
        public class FoundFile
        {
            public string FullName { get; set; }
            public string Checksum { get; set; }
        }

        private static void Main(string[] args)
        {
            var drive = "e";
            var fileDictionary = new Dictionary<string, List<FoundFile>>();
            var sw = new Stopwatch();

            sw.Start();
            var driveToAnalyze = new DriveInfo(drive);
            using (var ntfsReader = new NtfsReader(driveToAnalyze, RetrieveMode.StandardInformations))
            {
                var nodes = ntfsReader.GetNodes(driveToAnalyze.Name);

                foreach (var node in nodes)
                    if ((node.Attributes & Attributes.System) == 0 &&
                        (node.Attributes & Attributes.Directory) == 0 &&
                        (node.Attributes & Attributes.Archive) != 0 &&
                        !node.FullName.Contains("$"))
                    {
                        // is file
                        var fileKey = GetFileDictionaryKey(node);

                        // does not exist
                        if (!fileDictionary.ContainsKey(fileKey))
                        {
                            fileDictionary.Add(fileKey, new List<FoundFile> { new FoundFile { FullName = node.FullName } });
                            continue;
                        }

                        // does exist
                        fileDictionary[fileKey].Add(new FoundFile { FullName = node.FullName });
                    }
            }

            // get duplicates
            var duplicates = fileDictionary.Where(q => q.Value.Count > 1).ToList();
            var duplicatesAfterChecksum = new Dictionary<string, List<FoundFile>>();

            // detailed checksum comparison
            Parallel.ForEach(duplicates, (entry) =>
            {
                // get checksum
                var checkSums = new List<string>();
                foreach (var foundFile in entry.Value)
                {
                    using (var md5 = MD5.Create())
                    using (var stream = File.OpenRead(foundFile.FullName))
                    {
                        foundFile.Checksum = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLower();
                        checkSums.Add(foundFile.Checksum);
                    }
                }

                // remove unique values
                var query = checkSums.GroupBy(x => x)
                    .Where(g => g.Count() == 1)
                    .Select(y => y.Key)
                    .ToList();
                if (!query.Any())
                {
                    duplicatesAfterChecksum.Add(entry.Key, entry.Value);
                    return;
                }
                foreach (var uniqueChecksum in query)
                {
                    var file = entry.Value.Single(q => q.Checksum == uniqueChecksum);
                    entry.Value.Remove(file);
                }

                // no duplicates => don't add entry
                if (entry.Value.Count > 1)
                {
                    duplicatesAfterChecksum.Add(entry.Key, entry.Value);
                }
            });

            sw.Stop();
            var elapsedMs = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine("Time taken in ms: {0}", elapsedMs);
            Console.WriteLine("Total duplicate files: {0}", duplicatesAfterChecksum.Count);
            ExitPrompt();
        }

        private static void ExitPrompt()
        {
            Console.WriteLine("");
            Console.WriteLine("press any key to exit");
            Console.ReadKey();
        }

        private static string GetFileDictionaryKey(INode node)
        {
            return $"{node.Name}{node.LastAccessTime.Ticks}{node.Size}";
        }
    }
}