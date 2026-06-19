using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.RegularExpressions;

namespace Odasoft.XBOL.Data.Extensions
{
    public class PhoneSanitizerConverter : ValueConverter<string, string>
    {
        public PhoneSanitizerConverter() : base(v => Sanitize(v), v => v)
        {
        }

        public static string Sanitize(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            string clean = Regex.Replace(phone.Trim(), @"[^\d]", "");

            return clean;
        }
    }
}