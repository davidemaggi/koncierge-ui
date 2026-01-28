using System;
using System.Collections.Generic;
using System.Text;

namespace KonciergeUi.Client.Extensions
{
    public static class StringExtensions
    {
        public static string ToFileNameIfPath(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            try
            {
                // Path.GetFileName handles both paths and non-paths gracefully
                // If it's not a path, it just returns the original string
                return Path.GetFileName(input);
            }
            catch
            {
                // If Path.GetFileName throws (invalid path chars), return original
                return input;
            }
        }
    }

}
