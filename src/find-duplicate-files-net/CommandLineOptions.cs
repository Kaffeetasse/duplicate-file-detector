using CommandLine;

namespace find_duplicate_files_net
{
    public class CommandLineOptions
    {
        [Option('d', "drive", Required = true, HelpText = "Drive to analyze. eg: C, D, ...")]
        public char DriveLetter { get; set; }

        [Option('s', "save", Required = false, HelpText = "Save result to binary file.")]
        public bool Save { get; set; }

        [Option('o', "out", Required = false, HelpText = "Result in/out file name.", Default = "result.bin")]
        public string SaveFile { get; set; }

        [Option('r', "read", Required = false, HelpText = "Read from binary file.")]
        public bool Read { get; set; }
    }
}