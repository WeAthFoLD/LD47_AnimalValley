using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public interface IMatcher
{
    /// <summary>
    /// Return the number of characters that this "regex" or equivalent
    /// matches.
    /// </summary>
    /// <param name="text">The text to be matched</param>
    /// <returns>The number of characters that matched</returns>
    int Match(string text);
}

sealed class RegexMatcher : IMatcher
{
    private readonly Regex regex;
    public RegexMatcher(string regex)
    {
        this.regex = new Regex(string.Format("^({0})", regex));
    }

    public int Match(string text)
    {
        var m = regex.Match(text);
        return m.Success ? m.Length : 0;
    }

    public override string ToString()
    {
        return regex.ToString();
    }
}

public sealed class TokenDefinition
{
    public readonly IMatcher Matcher;
    public readonly object Token;
    public readonly bool skip;

    public TokenDefinition(string regex, object token, bool skip = false)
    {
        this.Matcher = new RegexMatcher(regex);
        this.Token = token;
        this.skip = skip;
    }
}

public sealed class Lexer : IDisposable
{
    class SavedState {
        public string content;
        public object token;
    }

    public Action<string> errorHandler;

    private readonly TextReader reader;
    private readonly TokenDefinition[] tokenDefinitions;

    private string lineRemaining;

    private Stack<SavedState> savedLexemes = new Stack<SavedState>();

    public Lexer(TextReader reader, TokenDefinition[] tokenDefinitions)
    {
        this.reader = reader;
        this.tokenDefinitions = tokenDefinitions;
        nextLine();
    }

    private void nextLine()
    {
        do {
            lineRemaining = reader.ReadLine();
            if (lineRemaining != null) {
                _totalReadLineCharacters += lineRemaining.Length + 1;
                _thisLineLength = lineRemaining.Length;
            }

            ++LineNumber;
            Position = 0;
        } while (lineRemaining != null && lineRemaining.Length == 0);
    }

    public void SkipThisLine() {
        savedLexemes.Clear();
        nextLine();
    }

    public bool Next()
    {
        if (savedLexemes.Count > 0) {
            var saved = savedLexemes.Pop();
            Token = saved.token;
            TokenContents = saved.content;
            return true;
        }

        if (lineRemaining == null)
            return false;

        int maxMatched = 0;
        TokenDefinition matchedDef = null;
        foreach (var def in tokenDefinitions) {
            var matched = def.Matcher.Match(lineRemaining);
            if (matched > maxMatched) {
                maxMatched = matched;
                matchedDef = def;
            }
        }

        if (maxMatched > 0) {
            Position += maxMatched;
            Token = matchedDef.Token;
            TokenContents = lineRemaining.Substring(0, maxMatched);
            lineRemaining = lineRemaining.Substring(maxMatched);
            if (lineRemaining.Length == 0)
                nextLine();

            if (matchedDef.skip) {
                if (lineRemaining == null) {
                    return false;
                } else {
                    return Next();
                }
            } else {
                return true;
            }
        }

        var msg = "Unable to match against any tokens";
        if (errorHandler == null)
            throw new Exception(msg + " at line " + LineNumber + " col " + Position);

        errorHandler(msg);
        return false;
    }

    public void PushCurrent() {
        savedLexemes.Push(new SavedState { content = TokenContents, token = Token });
    }

    public string NextContent() {
        if (Next()) {
            return TokenContents;
        } else {
            return null;
        }
    }

    public string TokenContents { get; private set; }

    public object Token { get; private set; }

    public int LineNumber { get; private set; }

    public int Position { get; private set; }

    public TextPos Caret => new TextPos {Line = LineNumber, Column = Position};

    public TextPos LastTokenStartCaret => new TextPos { Line = LineNumber, Column = TokenContents?.Length ?? 0 };

    private int _totalReadLineCharacters;

    private int _thisLineLength;

    public void Dispose()
    {
        reader.Dispose();
    }
}
