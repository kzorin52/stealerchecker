using System.IO;

namespace stealerchecker;

public struct Log
{
    public Log(string fullPath)
    {
        FullPath = fullPath;
        Name = Path.GetFileName(fullPath);
    }

    public string FullPath;
    public string Name;
}