using System.Text;

namespace Odasoft.XBOL.Commons.Helpers
{
    public static class PhoneNumberHelper
    {
        /// <summary>
        /// Normalizes a phone number by removing all non-digit characters
        /// </summary>
        public static string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return "";
            }

            // Remove all non-digit characters
            var digitsOnly = new StringBuilder();

            foreach (var c in phoneNumber)
            {
                // Better use char.IsAsciiDigit instead of char.IsDigit to ensure we only capture standard 0 - 9 numbers and ignore other Unicode number formats.
                if (char.IsAsciiDigit(c))
                {
                    digitsOnly.Append(c);
                }
            }

            return digitsOnly.ToString();
        }
    }
}
