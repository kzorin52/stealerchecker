namespace stealerchecker.Models
{
    #region MODELS
    public struct CheckResult
    {
        public bool IsValid;
        public bool Payment;
        public string Token;

        public CheckResult(bool isValid, string token, bool isPayment = false)
        {
            IsValid = isValid;
            Payment = isPayment;
            Token = token;
        }
    }

    #endregion
}