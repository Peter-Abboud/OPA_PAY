namespace OPA_Pay.Helpers
{
    public static class SerialNumberGenerator
    {
        public static string AccountNumber()
            => $"OPA-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";

        public static string TransferReference()
            => $"TRX-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";

        public static string ReceiptNumber()
            => $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";

        public static string PickupCode()
            => Random.Shared.Next(100000, 999999).ToString();
    }
}
