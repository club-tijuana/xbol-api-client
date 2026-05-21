using System.ComponentModel.DataAnnotations;

namespace Odasoft.XBOL.Models
{
    public class Role
    {
        [Key]
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? NormalizedName { get; set; }
        public string? ConcurrencyStamp { get; set; }
    }
}
