using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading;
using Colorful;
using static stealerchecker.Program;

namespace stealerchecker;

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
        catch
        {
            Print(menu);
        }

        PrintMainMenu();
    }

    internal static void WriteLine(this StringBuilder builder, string value)
    {
        builder.Append(value).Append(NewLine);
    }

    public static string Input(string text, Color color)
    {
        Console.WriteLine(text, color);
        return Console.ReadLine();
    }

    public static bool AnyNew(this string a, string b)
    {
        return CultureInfo
            .CurrentCulture
            .CompareInfo
            .IndexOf(a, b, 0, a.Length, CompareOptions.IgnoreCase) >= 0;
    }

    public static void Wait(this Thread th)
    {
        while (th.IsAlive) ;
    }

    public static string Join(this IEnumerable<string> ts)
    {
        return string.Join(NewLine, ts);
    }

    public static string Join(this IEnumerable<string> ts, string del)
    {
        return string.Join(del, ts);
    }

    public static string Join(this string[] ts)
    {
        return string.Join(NewLine, ts);
    }

    public static string Join(this string[] ts, string del)
    {
        return string.Join(del, ts);
    }
}