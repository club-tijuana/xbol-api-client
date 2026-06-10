using System.Text.Json.Serialization;

namespace Odasoft.XBOL.Commons.Enums
{
    /// <summary>
    /// Enum to define what kind of movement was made.
    /// </summary>
    /// <remarks>
    /// Drawdown: Standard charge by client
    /// Interest: System-generated interest charge
    /// Fee: Late fees, anual fees, etc
    /// AdjustmentDebit: Correction adding to debt
    ///
    /// Payment: Client pays back
    /// Reversal: Refunding a charge
    /// AdjustmentCredit: Correction reducing debt. This also work for condonations
    /// </remarks>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CreditTransactionType
    {
        // Debits (increase teh Balance Owed)
        Drawdown,   // Standard charge

        Interest,
        Fee,
        AdjustmentDebit, // To avoid deleting or updating records

        // Credits (Decrease the Balance Owed)
        Payment,

        Reversal,
        AdjustmentCredit,
    }
}
