namespace Bynder.Content.SitecoreConnector.SitecoreRepositories.Extensions
{
    using System.Linq;

    public static class StringExtensions
    {
        public static string GetUrlOptimisedItemName(string name)
        {
            char[] allowedChars = name
                .ToLower()
                .ToCharArray()
                .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray();

            string newName = new string(allowedChars).Trim('-');

            newName = RemoveDuplicateSymbols(newName, "-");

            return newName;
        }

        public static string RemoveDuplicateSymbols(string input, string symbol)
        {
            var duplicate = symbol + symbol;
            while (input.Contains(duplicate))
            {
                input = input.Replace(duplicate, symbol);
            }

            return input;
        }
    }
}
