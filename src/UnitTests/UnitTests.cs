using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Filesystem.Ntfs;
using System.Linq;
using find_duplicate_files_net;
using Moq;
using Xunit;

namespace UnitTests
{
    public class UnitTests
    {
        private static List<INode> GetNodeFakes()
        {
            return new List<INode>
            {
                new FakeNode
                {
                    Attributes = Attributes.Archive,
                    NodeIndex = 1,
                    ParentNodeIndex = 0,
                    Name = "test.txt",
                    Size = 1337,
                    FullName = @"C:\test.txt",
                    Streams = null,
                    CreationTime = new DateTime(2020, 12, 26),
                    LastAccessTime = new DateTime(2020, 12, 26),
                    LastChangeTime = new DateTime(2020, 12, 26)
                },
                new FakeNode
                {
                    Attributes = Attributes.Archive,
                    NodeIndex = 1,
                    ParentNodeIndex = 0,
                    Name = "test.txt",
                    Size = 1337,
                    FullName = @"C:\bla\test.txt",
                    Streams = null,
                    CreationTime = new DateTime(2020, 12, 26),
                    LastAccessTime = new DateTime(2020, 12, 26),
                    LastChangeTime = new DateTime(2020, 12, 26)
                },new FakeNode
                {
                    Attributes = Attributes.Archive,
                    NodeIndex = 1,
                    ParentNodeIndex = 0,
                    Name = "testNope.txt",
                    Size = 1337,
                    FullName = @"C:\bla\testNope.txt",
                    Streams = null,
                    CreationTime = new DateTime(2020, 12, 26),
                    LastAccessTime = new DateTime(2020, 12, 26),
                    LastChangeTime = new DateTime(2020, 12, 26)
                }
            };
        }

        [Fact]
        public void Test()
        {
            const string driveName = "C:\\";
            var ntfsReader = new Mock<INtfsReader>();
            ntfsReader.Setup(q => q.GetNodes(driveName)).Returns(GetNodeFakes());
            var mockFileSystem = new MockFileSystem();
            var mockInputFile = new MockFileData("line1\nline2\nline3");
            mockFileSystem.AddFile(@"C:\test.txt", mockInputFile);
            mockFileSystem.AddFile(@"C:\bla\test.txt", mockInputFile);
            mockFileSystem.AddFile(@"C:\bla\testNope.txt", mockInputFile);
            var driveInfo = new MockDriveInfo(mockFileSystem, driveName);

            var sut = new FileHandler(ntfsReader.Object, mockFileSystem, driveInfo);
            var duplicatesAfterChecksum = sut.GetDuplicateFiles();

            Assert.Single(duplicatesAfterChecksum);
            Assert.Equal(2, duplicatesAfterChecksum.First().Value.Count);
        }
    }
}