namespace KonciergeUi.Client.Extensions
{
    public static class StringExtensions
    {
        public static string ToTagChipLabel(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            var words = input
                .Trim()
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return string.Join(" ", words.Select(word =>
                word.Length == 1
                    ? char.ToUpperInvariant(word[0]).ToString()
                    : char.ToUpperInvariant(word[0]) + word[1..]));
        }

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
