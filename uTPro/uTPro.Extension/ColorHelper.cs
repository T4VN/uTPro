using Microsoft.AspNetCore.Http;
using System.Drawing;

namespace uTPro.Extension
{
    public static class ColorHelper
    {
        /// <summary>
        /// Convert hex (#RRGGBB or #RGB) to Color (RGB).
        /// </summary>
        public static Color? HexToRgb(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return null;

            // Remove #
            hex = hex.TrimStart('#');

            // If #RGB to #RRGGBB
            if (hex.Length == 3)
            {
                hex = string.Concat(hex[0], hex[0],
                                    hex[1], hex[1],
                                    hex[2], hex[2]);
            }

            if (hex.Length != 6)
                return null;

            // Parse R, G, B
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// Convert RGB to Hex (#RRGGBB).
        /// </summary>
        public static string RgbToHex(int r, int g, int b)
        {
            r = Math.Clamp(r, 0, 255);
            g = Math.Clamp(g, 0, 255);
            b = Math.Clamp(b, 0, 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        /// <summary>
        /// Convert Color to Hex.
        /// </summary>
        public static string RgbToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
