using System.Collections.Generic;

namespace System.IO.Filesystem.Ntfs
{
    public interface INtfsReader
    {
        /// <summary>
        /// Get the drive on which this instance is bound to.
        /// </summary>
        DriveInfo DriveInfo { get; }

        /// <summary>
        /// Get information about the NTFS volume.
        /// </summary>
        IDiskInfo DiskInfo { get; }

        /// <summary>
        /// Get a single node that match exactly the given path
        /// </summary>
        INode GetNode(string fullPath);

        /// <summary>
        /// Get all nodes under the specified rootPath.
        /// </summary>
        /// <param name="rootPath">The rootPath must at least contains the drive and may include any number of subdirectories. Wildcards aren't supported.</param>
        List<INode> GetNodes(string rootPath);

        unsafe byte[] ReadFile(INode node);
        byte[] GetVolumeBitmap();
        void Dispose();

        /// <summary>
        /// Raised once the bitmap data has been read.
        /// </summary>
        event EventHandler BitmapDataAvailable;
    }
}