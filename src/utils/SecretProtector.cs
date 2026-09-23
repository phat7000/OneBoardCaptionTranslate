using System.Security.Cryptography;
using System.Text;

namespace LiveCaptionsTranslator.utils
{
    public static class SecretProtector
    {
        private const string Prefix = "dpapi:";
        private static readonly byte[] entropy = Encoding.UTF8.GetBytes("OneBoard Capture Translate/settings/v1");

        public static string Protect(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            if (value.StartsWith(Prefix, StringComparison.Ordinal))
                return value;

            byte[] plaintext = Encoding.UTF8.GetBytes(value);
            byte[] encrypted = ProtectedData.Protect(plaintext, entropy, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(encrypted);
        }

        public static string Unprotect(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            if (!value.StartsWith(Prefix, StringComparison.Ordinal))
                return value;

            try
            {
                byte[] encrypted = Convert.FromBase64String(value[Prefix.Length..]);
                byte[] plaintext = ProtectedData.Unprotect(encrypted, entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plaintext);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                return string.Empty;
            }
        }
    }
}
