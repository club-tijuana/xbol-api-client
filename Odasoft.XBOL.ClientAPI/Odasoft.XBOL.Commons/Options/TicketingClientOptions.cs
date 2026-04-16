using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Odasoft.XBOL.Commons.Options;

public class TicketingClientOptions
{
    [Required]
    [Description("Base URL for the ticketing API")]
    public string BaseAddress { get; set; } = "";
}
