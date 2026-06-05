using System.ComponentModel;

namespace Odasoft.XBOL.Commons.Enums
{
    public enum OrderAction
    {
        [Description("Order created.")]
        OrderCreated,

        [Description("Order renewed.")]
        OrderRenewed,

        [Description("Cancel with refund.")]
        CancelWithRefund,

        [Description("Cancel without refund.")]
        CancelWithoutRefund,

        [Description("Resend receipt.")]
        ResendReceipt,

        [Description("Update order holder.")]
        UpdateOrderHolder,

        [Description("Reissue tickets.")]
        ReissueTickets,

        [Description("Send individual tickets")]
        SendIndividualTickets,

        [Description("Resend courtesy tickets.")]
        ResendCourtesyTickets,

        [Description("Convert to digital/physical tickets.")]
        ConvertToDigitalPhysical,

        [Description("Cancel tickets.")]
        CancelTickets
    }
}
