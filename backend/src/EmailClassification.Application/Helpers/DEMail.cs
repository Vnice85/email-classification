using System.Text;

namespace EmailClassification.Application.Helpers
{
    public static class DEMail
    {
        public static string DecodeBase64(string base64UrlSafe)
        {

            string base64String = base64UrlSafe.Replace('-', '+').Replace('_', '/');
            int paddingLength = base64String.Length % 4;
            if (paddingLength > 0)
            {
                base64String = base64String.PadRight(base64String.Length + (4 - paddingLength), '=');
            }
            byte[] decodedBytes = Convert.FromBase64String(base64String);
            string decodedString = Encoding.UTF8.GetString(decodedBytes);
            return decodedString;
        }

        public static string EncodeBase64(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            string base64String = Convert.ToBase64String(bytes);
            return base64String.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }
}
