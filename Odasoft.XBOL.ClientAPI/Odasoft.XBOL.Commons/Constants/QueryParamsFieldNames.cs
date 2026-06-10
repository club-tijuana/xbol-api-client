namespace Odasoft.XBOL.Commons.Constants
{
    /// <summary>
    /// Provides constant field names for suite-related query parameters.
    /// </summary>
    /// <remarks>Use these constants to avoid hardcoding query parameter names when constructing requests that
    /// filter or identify test suites by name or level.
    /// Also take in consideration that the values must be lower case.</remarks>
    public static class QueryParamsFieldNames
    {
        public const string SUITE_NAME = "name";
        public const string SUITE_LEVEL = "level";

        public const string CREDIT_TRANSACTION_AMOUNT = "amount";
        public const string CREDIT_TRANSACTION_PAYMENT_DATE = "date";
        public const string CREDIT_TRANSACTION_PAYMENT_TYPE = "paymentmethod";
        public const string CREDIT_TRANSACTION_RECEIVED_BY = "receivedby";
        public const string CREDIT_TRANSACTION_REFERENCE_ID = "paymentid";

        public const string ORDER_AMOUNT = "amount";
        public const string ORDER_EVENT = "event";
        public const string ORDER_DATE = "date";
        public const string ORDER_NUMBER_OF_ITEMS = "tickets";
    }
}
