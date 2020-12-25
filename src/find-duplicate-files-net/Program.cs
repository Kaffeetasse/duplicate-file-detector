using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Filesystem.Ntfs;

namespace find_duplicate_files_net
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var drive = "e";
            var listOfFiles = new List<string>();
            var sw = new Stopwatch();

            sw.Start();
            var driveToAnalyze = new DriveInfo(drive);
            using (var ntfsReader = new NtfsReader(driveToAnalyze, RetrieveMode.Minimal))
            {
                var nodes = ntfsReader.GetNodes(driveToAnalyze.Name);

                foreach (var node in nodes)
                    if ((node.Attributes & Attributes.System) == 0 &&
                        (node.Attributes & Attributes.Archive) != 0 &&
                        !node.FullName.Contains("$"))
                    {
                        // is file
                        listOfFiles.Add(node.FullName);
                        Console.WriteLine(node.FullName);
                    }
            }

            sw.Stop();
            var elapsedMs = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine("Total files: {0}", listOfFiles.Count);
            Console.WriteLine("Total ms: {0}", elapsedMs);
            //ExitPrompt();
        }

        private static void ExitPrompt()
        {
            Console.WriteLine("press any key to exit");
            Console.ReadKey();
        }
    }
}