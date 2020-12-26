using System;
using System.Diagnostics;
using System.IO;
using System.IO.Filesystem.Ntfs;

namespace find_duplicate_files_net
{
    public class Program
    {
        internal static Stopwatch StopWatch = new Stopwatch();

        private static void Main(string[] args)
        {
            var drive = "h";

            StopWatch = new Stopwatch();
            StopWatch.Start();

            var driveInfo = new DriveInfo(drive);
            var ntfsReader = new NtfsReader(driveInfo, RetrieveMode.StandardInformations);
            var fileHandler = new FileHandler(ntfsReader, driveInfo);

            var duplicatesAfterChecksum = fileHandler.GetDuplicateFiles();

            StopWatch.Stop();
            var elapsedMs = StopWatch.ElapsedMilliseconds;

            Console.WriteLine("");
            Console.WriteLine("Total Time taken: {0} ms", elapsedMs);
            Console.WriteLine("Total duplicate files: {0}", duplicatesAfterChecksum.Count);
            ExitPrompt();
        }

        private static void ExitPrompt()
        {
            Console.WriteLine("");
            Console.WriteLine("press any key to exit");
            Console.ReadKey();
        }
    }
}