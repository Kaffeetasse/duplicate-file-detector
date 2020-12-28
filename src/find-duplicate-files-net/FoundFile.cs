namespace find_duplicate_files_net
{
    public class FoundFile
    {
        public string FullName { get; set; }
        public string Checksum { get; set; }
        public ulong Size { get; set; }
    }
}