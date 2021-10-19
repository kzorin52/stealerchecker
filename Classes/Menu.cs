using System;
using System.Collections.Generic;

namespace stealerchecker;

public static partial class Program
{
    public struct Menu
    {
        public List<KeyValuePair<string, Action>> menu;
        public string Name;
    }
}