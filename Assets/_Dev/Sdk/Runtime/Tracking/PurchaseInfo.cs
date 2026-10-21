namespace MoodBoost.Sdk.Tracking
{
    // A real purchase, sent to the MMP to compute IAP revenue and ROAS.
    //
    // Receipt and Signature are not redundant fields: the MMP uses them to VALIDATE the transaction with the store.
    // Leaving them empty still gets the revenue onto the dashboard, but as unvalidated - and that number cannot be
    // used to optimize campaigns, which is exactly the reason for attaching an MMP in the first place.
    //
    //  - Google Play: Receipt is the purchase data JSON string, Signature is the accompanying signature.
    //  - App Store: Receipt is the base64 app receipt, Signature is left empty.
    public readonly struct PurchaseInfo
    {
        public readonly string ProductId;
        public readonly string CurrencyCode;
        public readonly int Quantity;
        public readonly double UnitPrice;
        public readonly string TransactionId;
        public readonly string Receipt;
        public readonly string Signature;

        public PurchaseInfo(
            string productId,
            string currencyCode,
            double unitPrice,
            string transactionId,
            string receipt = null,
            string signature = null,
            int quantity = 1)
        {
            ProductId = productId ?? string.Empty;
            CurrencyCode = string.IsNullOrEmpty(currencyCode) ? "USD" : currencyCode;
            Quantity = quantity <= 0 ? 1 : quantity;
            UnitPrice = unitPrice;
            TransactionId = transactionId ?? string.Empty;
            Receipt = receipt ?? string.Empty;
            Signature = signature ?? string.Empty;
        }
    }
}
