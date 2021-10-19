namespace stealerchecker;

public static partial class Program
{
    public class Password
    {
        public string Login;
        public string Pass;
        public Log stealer;

        public string Url;

        public Password(string url, string login, string pass, Log log)
        {
            Url = url;
            Login = login;
            Pass = pass;
            stealer = log;
        }
    }
}