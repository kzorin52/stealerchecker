using CommandLine;

namespace stealerchecker
{
    public static partial class Program
    {
        public class Options
        {
            [Option('p', "path", Required = true, HelpText = "Path to folder with logs")]
            public string Path { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "Passwords view verbose mode")]
            public bool Verbose { get; set; }

            [Option('e', "everything", Required = false, HelpText = "Use Everything service")]
            public bool Everything { get; set; }

            [Option('a', "all", Required = false, HelpText = "Search all logs", Hidden = true)]
            public bool All { get; set; }
        }
    }
}