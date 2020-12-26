using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Filesystem.Ntfs;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace find_duplicate_files_net
{
    public class FileHandler
    {
        private readonly INtfsReader _ntfsReader;
        private readonly IFileSystem _fileSystem;
        private readonly DriveInfoBase _driveInfo;

        public FileHandler(INtfsReader ntfsReader, IFileSystem fileSystem, DriveInfoBase driveInfo)
        {
            _ntfsReader = ntfsReader;
            _fileSystem = fileSystem;
            _driveInfo = driveInfo;
        }

        public FileHandler(INtfsReader ntfsReader, DriveInfo drive)
        {
            _ntfsReader = ntfsReader;
            _fileSystem = new FileSystem();
            _driveInfo = new DriveInfoWrapper(_fileSystem, drive);
        }

        public Dictionary<string, List<FoundFile>> GetDuplicateFiles()
        {
            Console.WriteLine("read ntfs mft");
            var fileDictionary = GetFiles();

            var duplicates = GetFileDuplicates(fileDictionary);

            Console.WriteLine("found {0} duplicate files before checksum compare", duplicates.Count);

            var timeBeforeHash = Program.StopWatch.ElapsedMilliseconds;

            var duplicatesAfterChecksum = DuplicatesCompareChecksum(duplicates);

            var timeHash = Program.StopWatch.ElapsedMilliseconds - timeBeforeHash;
            Console.WriteLine("generating checksum took {0} ms", timeHash);
            return duplicatesAfterChecksum;
        }

        public Dictionary<string, List<FoundFile>> DuplicatesCompareChecksum(
            List<KeyValuePair<string, List<FoundFile>>> duplicates)
        {
            // detailed checksum comparison
            var duplicatesAfterChecksum = new ConcurrentDictionary<string, List<FoundFile>>();
            Parallel.ForEach(duplicates, entry =>
            {
                // get checksum
                var checkSums = new List<string>();
                foreach (var foundFile in entry.Value)
                    using (var md5 = MD5.Create())
                    using (var stream = _fileSystem.File.OpenRead(foundFile.FullName))
                    {
                        foundFile.Checksum = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty)
                            .ToLower();
                        checkSums.Add(foundFile.Checksum);
                    }

                // remove unique values
                var query = checkSums.GroupBy(x => x)
                    .Where(g => g.Count() == 1)
                    .Select(y => y.Key)
                    .ToList();
                if (!query.Any())
                {
                    duplicatesAfterChecksum.TryAdd(entry.Key, entry.Value);
                    return;
                }

                foreach (var uniqueChecksum in query)
                {
                    var file = entry.Value.Single(q => q.Checksum == uniqueChecksum);
                    entry.Value.Remove(file);
                }

                // no duplicates => don't add entry
                if (entry.Value.Count > 1) duplicatesAfterChecksum.TryAdd(entry.Key, entry.Value);
            });
            return duplicatesAfterChecksum.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static List<KeyValuePair<string, List<FoundFile>>> GetFileDuplicates(
            Dictionary<string, List<FoundFile>> fileDictionary)
        {
            // get duplicates
            var duplicates = fileDictionary.Where(q => q.Value.Count > 1).ToList();
            return duplicates;
        }

        public Dictionary<string, List<FoundFile>> GetFiles()
        {
            var fileDictionary = new Dictionary<string, List<FoundFile>>();

            var nodes = _ntfsReader.GetNodes(_driveInfo.Name);
            var ntfsTime = Program.StopWatch.ElapsedMilliseconds;
            Console.WriteLine("mft read in {0} ms", ntfsTime);

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

            var ntfsFileTime = Program.StopWatch.ElapsedMilliseconds - ntfsTime;
            Console.WriteLine("found {0} files in {1} ms", fileDictionary.Count, ntfsFileTime);

            return fileDictionary;
        }

        private static string GetFileDictionaryKey(INode node)
        {
            return $"{node.Name}{node.LastAccessTime.Ticks}{node.Size}";
        }
    }
}