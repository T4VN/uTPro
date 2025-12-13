using System.Globalization;
using System.Text;
namespace uTPro.Extension.FileHelper
{
    public class FileNameHelper
    {
        private const int MaxFileNameLength = 100; // Maximum file name length

        /// <summary>
        /// Sanitizes a file name by removing diacritics, replacing invalid characters, and limiting its length.
        /// </summary>
        /// <param name="fileName">The original file name</param>
        /// <returns>A valid file name</returns>
        public static string GetValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Invalid file name!");

            // 1. Remove diacritics
            string normalized = RemoveDiacritics(fileName);

            // 2. Replace invalid characters with "_"
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = string.Join("_", normalized.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            // 3. Limit the file name length to 100 characters
            if (sanitized.Length > MaxFileNameLength)
            {
                sanitized = sanitized.Substring(0, MaxFileNameLength);
            }

            return sanitized;
        }

        /// <summary>
        /// Removes diacritics (accents) from a string.
        /// </summary>
        /// <param name="text">The input string</param>
        /// <returns>A string without diacritics</returns>
        private static string RemoveDiacritics(string text)
        {
            string normalizedString = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
