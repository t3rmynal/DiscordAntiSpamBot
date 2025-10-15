using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AntiSpamBot
{
    public static class SpamDetector
    {
        private static readonly Regex UrlRegex = new Regex(@"\b(?:https?:\/\/|www\.)\S{3,}\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex InviteRegex = new Regex(@"\b(?:discord(?:app)?\.com\/invite|discord\.gg)\/[A-Za-z0-9\-_.]{2,}\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex MoneyRegex = new Regex(@"\b(?:\p{Sc}\s*[\d\s.,]{1,8}|[\d\s.,]{1,8}\s*\p{Sc})\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PromoRegex = new Regex(@"\b(?:free|gratis|gift|giveaway|promo|promo\s?code|claim|win|won|prize)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SpecificServiceRegex = new Regex(@"\b(?:nitro|steam(?:\s*gift|key)?|roblox|robux|fortnite|crypto|airdrop)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex MassMentionRegex = new Regex(@"@(?:everyone|here)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ExcessivePunctRegex = new Regex(@"[!?.]{4,}|(?:[:;*\-_=~`#@]){6,}|(?:\p{So}){5,}",
            RegexOptions.Compiled);

        private static readonly Regex CallToActionRegex = new Regex(@"\b(?:click here|join now|claim (?:your )?(?:gift|prize)|redeem now)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RussianMoneyWordsRegex = new Regex(@"\b(?:руб(?:\.|лей)?|р\.)\s*[\d\s.,]{1,8}\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly string[] BlacklistWords =
        {
            "free nitro", "free nitro code", "free steam", "free steam key", "free gift", "get free",
            "claim nitro", "claim steam", "get nitro", "get steam", "free robux"
        };

        private static readonly Dictionary<char, char> CharMap = new Dictionary<char, char>
        {
            // циферки -> буквы
            ['0'] = 'o',
            ['1'] = 'i',
            ['3'] = 'e',
            ['4'] = 'a',
            ['5'] = 's',
            ['7'] = 't',
            ['і'] = 'i',
            ['І'] = 'i',
            ['о'] = 'o',
            ['О'] = 'o',
            ['с'] = 'c',
            ['С'] = 'c',
            ['к'] = 'k',
            ['К'] = 'k',
            ['е'] = 'e',
            ['Е'] = 'e',
            ['а'] = 'a',
            ['А'] = 'a',
            ['\u200B'] = ' ', 
            ['\u200C'] = ' ',
            ['\u200D'] = ' ',
            ['\uFEFF'] = ' '
        };

        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var sb = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                if (CharMap.TryGetValue(ch, out var mapped))
                {
                    sb.Append(mapped);
                    continue;
                }

                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc == UnicodeCategory.Format || uc == UnicodeCategory.Control)
                {
                    sb.Append(' ');
                    continue;
                }

                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                }
                else
                {
                    // пробелы и пунктуация -> пробел
                    sb.Append(' ');
                }
            }

            var normalized = Regex.Replace(sb.ToString().ToLowerInvariant(), @"\s{2,}", " ").Trim();
            return normalized;
        }

        public static bool IsSpam(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (UrlRegex.IsMatch(text) || InviteRegex.IsMatch(text) || MassMentionRegex.IsMatch(text))
                return true;

            if (ExcessivePunctRegex.IsMatch(text))
                return true;

            var norm = Normalize(text);

            if (MoneyRegex.IsMatch(text) || RussianMoneyWordsRegex.IsMatch(norm))
            {
                if (PromoRegex.IsMatch(norm) || SpecificServiceRegex.IsMatch(norm) || CallToActionRegex.IsMatch(norm))
                    return true;
            }

            if (PromoRegex.IsMatch(norm) && (SpecificServiceRegex.IsMatch(norm) || MoneyRegex.IsMatch(norm)))
                return true;

            if (SpecificServiceRegex.IsMatch(norm) && (PromoRegex.IsMatch(norm) || MoneyRegex.IsMatch(text)))
                return true;

            foreach (var bad in BlacklistWords)
            {
                if (norm.Contains(bad))
                    return true;
            }

            if (CallToActionRegex.IsMatch(norm) && (UrlRegex.IsMatch(text) || MoneyRegex.IsMatch(text) || InviteRegex.IsMatch(text)))
                return true;

            if (Regex.IsMatch(text, @"(?:[\d\W_]{2,}\s*){3,}"))
                return true;

            return false;
        }
    }
}
