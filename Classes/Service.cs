using System.Collections.Generic;

namespace stealerchecker;

public static partial class Program
{
    internal struct Service
    {
        public string Name;
        public IEnumerable<string> Services;
    }
}