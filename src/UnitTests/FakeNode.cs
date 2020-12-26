using System;
using System.Collections.Generic;
using System.IO.Filesystem.Ntfs;

namespace UnitTests
{
    public class FakeNode : INode
    {
        public Attributes Attributes { get; set; }
        public uint NodeIndex { get; set; }
        public uint ParentNodeIndex { get; set; }
        public string Name { get; set; }
        public ulong Size { get; set; }
        public string FullName { get; set; }
        public IList<IStream> Streams { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastChangeTime { get; set; }
        public DateTime LastAccessTime { get; set; }
    }
}