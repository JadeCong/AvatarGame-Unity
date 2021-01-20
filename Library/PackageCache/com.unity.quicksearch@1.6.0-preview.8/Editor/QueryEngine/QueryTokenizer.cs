using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Unity.QuickSearch
{
    enum ParseState
    {
        Matched,
        NoMatch,
        ParseError
    }

    abstract class QueryTokenizer<TUserData>
    {
        // To match a regex at a specific index, use \\G and Match(input, startIndex)
        static readonly Regex k_PhraseRx = new Regex("\\G!?\\\".*?\\\"");
        Regex m_FilterRx = new Regex("\\G([\\w]+)(\\([^\\(\\)]+\\))?([^\\w\\s-{}()\"\\[\\].,/|\\`]+)(\\\".*?\\\"|([a-zA-Z0-9]*?)(?:\\{(?:(?<c>\\{)|[^{}]+|(?<-c>\\}))*(?(c)(?!))\\})|[^\\s{}]+)");
        static readonly Regex k_WordRx = new Regex("\\G!?\\S+");
        static readonly Regex k_NestedQueryRx = new Regex("\\G([a-zA-Z0-9]*?)(\\{(?:(?<c>\\{)|[^{}]+|(?<-c>\\}))*(?(c)(?!))\\})");

        static readonly List<string> k_CombiningToken = new List<string>
        {
            "and",
            "or",
            "not",
            "-"
        };

        delegate int TokenMatcher(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched);
        delegate bool TokenConsumer(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);

        List<Tuple<TokenMatcher, TokenConsumer>> m_TokenConsumers;

        public QueryTokenizer()
        {
            // The order of regex in this list is important. Keep it like that unless you know what you are doing!
            m_TokenConsumers = new List<Tuple<TokenMatcher, TokenConsumer>>
            {
                new Tuple<TokenMatcher, TokenConsumer>(MatchEmpty, ConsumeEmpty),
                new Tuple<TokenMatcher, TokenConsumer>(MatchGroup, ConsumeGroup),
                new Tuple<TokenMatcher, TokenConsumer>(MatchCombiningToken, ConsumeCombiningToken),
                new Tuple<TokenMatcher, TokenConsumer>(MatchNestedQuery, ConsumeNestedQuery),
                new Tuple<TokenMatcher, TokenConsumer>(MatchFilter, ConsumeFilter),
                new Tuple<TokenMatcher, TokenConsumer>(MatchWord, ConsumeWord)
            };
        }

        public void SetFilterRegex(Regex filterRx)
        {
            m_FilterRx = filterRx;
        }

        public ParseState Parse(string text, int startIndex, int endIndex, ICollection<QueryError> errors, TUserData userData)
        {
            var index = startIndex;
            while (index < endIndex)
            {
                var matched = false;
                foreach (var (matcher, consumer) in m_TokenConsumers)
                {
                    var matchLength = matcher(text, index, endIndex, errors, out var sv, out var match, out var consumerMatched);
                    if (!consumerMatched)
                        continue;
                    var consumed = consumer(text, index, index + matchLength, sv, match, errors, userData);
                    if (!consumed)
                    {
                        return ParseState.ParseError;
                    }
                    index += matchLength;
                    matched = true;
                    break;
                }

                if (!matched)
                {
                    errors.Add(new QueryError {index = index, reason = $"Error parsing string. No token could be deduced at {index}"});
                    return ParseState.NoMatch;
                }
            }

            return ParseState.Matched;
        }

        static int MatchEmpty(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            var currentIndex = startIndex;
            var lengthMatched = 0;
            matched = false;
            while (currentIndex < endIndex && QueryEngineUtils.IsWhiteSpaceChar(text[currentIndex]))
            {
                ++currentIndex;
                ++lengthMatched;
                matched = true;
            }

            sv = text.GetStringView(startIndex, startIndex + lengthMatched);
            match = null;
            return lengthMatched;
        }

        static int MatchCombiningToken(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            match = null;
            sv = text.GetStringView();
            var totalUsableLength = endIndex - startIndex;

            foreach (var combiningToken in k_CombiningToken)
            {
                var tokenLength = combiningToken.Length;
                if (tokenLength > totalUsableLength)
                    continue;

                sv = text.GetStringView(startIndex, startIndex + tokenLength);
                if (sv == combiningToken)
                {
                    matched = true;
                    return sv.Length;
                }
            }

            matched = false;
            return -1;
        }

        int MatchFilter(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = m_FilterRx.Match(text, startIndex, endIndex - startIndex);
            if (!match.Success)
            {
                matched = false;
                return -1;
            }

            matched = true;
            return match.Length;
        }

        int MatchWord(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = k_PhraseRx.Match(text, startIndex, endIndex - startIndex);
            if (!match.Success)
                match = k_WordRx.Match(text, startIndex, endIndex - startIndex);
            if (!match.Success)
            {
                matched = false;
                return -1;
            }

            matched = true;
            return match.Length;
        }

        int MatchGroup(string text, int groupStartIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = null;
            if (groupStartIndex >= text.Length || text[groupStartIndex] != '(')
            {
                matched = false;
                return -1;
            }

            matched = true;
            if (groupStartIndex < 0 || groupStartIndex >= text.Length)
            {
                errors.Add(new QueryError{index = 0, reason = $"A group should have been found but index was {groupStartIndex}"});
                return -1;
            }

            var charConsumed = 0;

            var parenthesisCounter = 1;
            var groupEndIndex = groupStartIndex + 1;
            for (; groupEndIndex < text.Length && parenthesisCounter > 0; ++groupEndIndex)
            {
                if (text[groupEndIndex] == '(')
                    ++parenthesisCounter;
                else if (text[groupEndIndex] == ')')
                    --parenthesisCounter;
            }

            // Because of the final ++groupEndIndex, decrement the index
            --groupEndIndex;

            if (parenthesisCounter != 0)
            {
                errors.Add(new QueryError{index = groupStartIndex, reason = $"Unbalanced parenthesis"});
                return -1;
            }

            charConsumed = groupEndIndex - groupStartIndex + 1;
            sv = text.GetStringView(groupStartIndex + 1, groupStartIndex + charConsumed - 1);
            return charConsumed;
        }

        int MatchNestedQuery(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = k_NestedQueryRx.Match(text, startIndex, endIndex - startIndex);
            if (!match.Success)
            {
                matched = false;
                return -1;
            }

            matched = true;
            return match.Length;
        }

        protected abstract bool ConsumeEmpty(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeCombiningToken(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeFilter(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeWord(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeGroup(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeNestedQuery(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
    }
}
