using System;
using System.Collections.Generic;
using System.Text;

class Lexer
{
    int _i;
    string _code;
    Position _currentPos;

    static Dictionary<string, TokenType> s_keywords = new Dictionary<string, TokenType>()
        {
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "let", TokenType.Let },
            { "infer", TokenType.Infer },
            { "native", TokenType.Native },
            { "gnative", TokenType.GlobalNative },
            { "null", TokenType.Null },
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "while", TokenType.While },
            { "break", TokenType.Break },
            { "continue", TokenType.Continue },
            { "cast", TokenType.Cast }
        };

    static Dictionary<string, TokenType> s_symbols = new Dictionary<string, TokenType>()
        {
            { "+", TokenType.Add },
            { "-", TokenType.Sub },
            { "*", TokenType.Mul },
            { "/", TokenType.Div },
            { "%", TokenType.Mod },
            { "==", TokenType.IsEqual },
            { "!=", TokenType.NotEqual },
            { "<", TokenType.Less },
            { ">", TokenType.Greater },
            { "<=", TokenType.LessEq },
            { ">=", TokenType.GreaterEq },
            { "=", TokenType.Equal },
            { ";", TokenType.Semicolon },
            { "[", TokenType.LeftBracket },
            { "]", TokenType.RightBracket },
            { "{", TokenType.LeftBrace },
            { "}", TokenType.RightBrace },
            { "&&", TokenType.And },
            { "||", TokenType.Or },
            { "!", TokenType.Bang },
            { "(", TokenType.LeftParen },
            { ")", TokenType.RightParen },
            { ",", TokenType.Comma },
            { ".", TokenType.Period },
            // { "#", TokenType.Hash },
            { "~", TokenType.BitwiseNot },
            { "<<", TokenType.BitwiseLeftShift },
            { ">>", TokenType.BitwiseRightShift },
            { "&", TokenType.BitwiseAnd },
            { "^", TokenType.BitwiseXor },
            { "|", TokenType.BitwiseOr },
            { "=>", TokenType.Arrow },
            { "@", TokenType.At },
            { "`", TokenType.BackTick }
        };

    public static string? GetKeywordFromType(TokenType type)
    {
        KeyValuePair<string, TokenType>? value = s_keywords.FirstOrDefault(x => x.Value == type);
        if (value == null)
            return null;
        return value.Value.Key;
    }

    public static string? GetSymbolFromType(TokenType type)
    {
        KeyValuePair<string, TokenType>? value = s_symbols.FirstOrDefault(x => x.Value == type);
        if (value == null)
            return null;
        return value.Value.Key;
    }

    public Lexer(string code, string source)
    {
        _code = code;
        _currentPos = new Position(1, 1, source);
    }

    public List<Token> Lex()
    {
        List<Token> tokens = new List<Token>();

        while (NotAtEnd())
        {
            if (char.IsWhiteSpace(Char()))
            {
                Next();
                continue;
            }

            if (NotAtEnd(1))
            {
                string doubleChar = $"{Char(0)}{Char(1)}";
                if (doubleChar == "//")
                {
                    Next();
                    Next();
                    while (NotAtEnd() && Char() != '\n')
                        Next();
                    continue;
                }
                if (s_symbols.TryGetValue(doubleChar, out TokenType value))
                {
                    tokens.Add(new Token(value, doubleChar, _currentPos));
                    Next();
                    Next();
                    continue;
                }
            }

            if (s_symbols.TryGetValue(Char().ToString(), out TokenType otherValue))
            {
                tokens.Add(new Token(otherValue, Char().ToString(), _currentPos));
                Next();
                continue;
            }

            if (Char() == '"')
            {
                tokens.Add(LexString(true, '"'));
                continue;
            }

            if (Char() == '#')
            {
                tokens.Add(LexString(false, '#'));
                continue;
            }

            if (char.IsLetter(Char()) || Char() == '_')
            {
                tokens.Add(LexAlpha());
                continue;
            }

            if (char.IsDigit(Char()))
            {
                tokens.Add(LexNumber());
                continue;
            }

            if (Char() == '\'')
            {
                Position position = _currentPos;

                Next();

                char c;

                if (Char() == '\\')
                    c = LexEscape();
                else
                {
                    c = Char();
                    Next();
                }

                if (!NotAtEnd() || Char() != '\'')
                    throw new Error("Unterminated character", _currentPos);

                tokens.Add(new Token(TokenType.Char, c.ToString(), position));
                Next();
                continue;
            }

            throw new Error($"Unexpected character '{Char()}'", _currentPos);
        }

        tokens.Add(new Token(TokenType.Eof, "End of File", _currentPos));

        return tokens;
    }

    Token LexString(bool allowEscapes, char target)
    {
        Position position = _currentPos;

        Next();

        StringBuilder sb = new StringBuilder();

        while (NotAtEnd() && Char() != target)
        {
            if (Char() == '\\' && allowEscapes)
            {
                sb.Append(LexEscape());
                continue;
            }
            sb.Append(Char());
            Next();
        }

        if (!NotAtEnd())
            throw new Error("Unterminated string", _currentPos);

        Next();

        return new Token(TokenType.String, sb.ToString(), position);
    }

    Token LexAlpha()
    {
        Position position = _currentPos;

        StringBuilder sb = new StringBuilder();

        while (NotAtEnd() && (char.IsLetterOrDigit(Char()) || Char() == '_'))
        {
            sb.Append(Char());
            Next();
        }

        string value = sb.ToString();

        if (s_keywords.TryGetValue(value, out TokenType keyword))
            return new Token(keyword, value, position);

        return new Token(TokenType.Identifier, value, position);
    }

    Token LexNumber()
    {
        Position position = _currentPos;

        StringBuilder sb = new StringBuilder();
        bool hasDecimal = false;

        while (NotAtEnd() && (char.IsDigit(Char()) || Char() == '.'))
        {
            if (Char() == '.')
            {
                if (hasDecimal)
                    throw new Error("Number already contains a decimal", _currentPos);

                if (!NotAtEnd(1) || !char.IsDigit(Char(1)))
                    throw new Error("Expected number after decimal", _currentPos);

                hasDecimal = true;
            }
            sb.Append(Char());
            Next();
        }

        return new Token(TokenType.Number, sb.ToString(), position);
    }

    char LexEscape()
    {
        Position escapePos = _currentPos;

        Next();

        if (!NotAtEnd())
            throw new Error("Unterminated escape sequence", escapePos);

        char escape = Char() switch
        {
            'n' => '\n',
            't' => '\t',
            'a' => '\a',
            '0' => '\0',
            '"' => '\"',
            '\\' => '\\',
            'f' => '\f',
            'v' => '\v',
            'b' => '\b',
            'e' => '\e',
            'r' => '\r',
            _ => Char(),
        };

        Next();

        return escape;
    }

    char Char(int dst = 0) => _code[_i + dst];
    bool NotAtEnd(int dst = 0) => _i + dst < _code.Length;

    void Next()
    {
        if (Char() == '\n')
        {
            _currentPos.Line++;
            _currentPos.Column = 1;
        }
        else
            _currentPos.Column++;
        _i++;
    }
}
