using System.Text.RegularExpressions;

namespace _Project.Develop.UI.Dialogue
{
    internal static class DialogueMarkup
    {
        public const string ClueLinkPrefix = "dialogue-clue-";

        private static string TagCluePhrases(string text)
        {
            var index = 0;
            return Regex.Replace(text, @"<(?<tag>highlight|h)>(?<phrase>.*?)</\k<tag>>",
                match => string.IsNullOrWhiteSpace(match.Groups["phrase"].Value)
                    ? match.Value
                    : $"<link=\"{ClueLinkPrefix}{index++}\">{match.Value}</link>",
                RegexOptions.Singleline);
        }

        public static string Format(string text, bool markClues)
        {
            text ??= string.Empty;
            if (markClues)
                text = TagCluePhrases(text);

            return text
                .Replace("<highlight>", "<color=#FFD54F>")
                .Replace("</highlight>", "</color>")
                .Replace("<h>", "<color=#FFD54F>")
                .Replace("</h>", "</color>")
                .Replace("<w>", "<wave>")
                .Replace("</w>", "</wave>");
        }
    }
}
