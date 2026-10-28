using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

class Lexer
{
    public Position CurrentPos;
    int _i;
    string _code;

    static Dictionary<string, TokenType> s_keywords = new Dictionary<string, TokenType>()
        {
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "let", TokenType.Let },
            { "infer", TokenType.Infer },
            { "native", TokenType.Native },
            // { "gnative", TokenType.GlobalNative }, SCRAPED
            { "null", TokenType.Null },
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "while", TokenType.While },
            { "break", TokenType.Break },
            { "continue", TokenType.Continue },
            { "cast", TokenType.Cast },
            { "return", TokenType.Return },
            { "cname", TokenType.CName },
            { "fmt", TokenType.Fmt },
            { "struct", TokenType.Struct },
            { "type", TokenType.Type },
            { "numeric", TokenType.Numeric },
            { "typeis", TokenType.TypeIs },
            { "union", TokenType.Union },
            { "enum", TokenType.Enum },
            { "extern", TokenType.Extern },
            { "macro", TokenType.Macro }
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
            // { "<<", TokenType.BitwiseLeftShift },
            // { ">>", TokenType.BitwiseRightShift },
            { "&", TokenType.BitwiseAnd },
            { "^", TokenType.BitwiseXor },
            { "|", TokenType.BitwiseOr },
            { "=>", TokenType.Arrow },
            { "@", TokenType.At },
            { "`", TokenType.BackTick },
            { "...", TokenType.Variadic },
            { "::", TokenType.DoubleColon }
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
        CurrentPos = new Position(1, 1, source);
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

            if (NotAtEnd(2))
            {
                string tripleChar = $"{Char()}{Char(1)}{Char(2)}";

                if (tripleChar == "\"\"\"")
                {
                    var position = CurrentPos;

                    Next();
                    Next();
                    Next();

                    var content = new StringBuilder();
                    bool terminated = false;

                    while (NotAtEnd())
                    {
                        if (NotAtEnd(2) && Char() is '"' && Char(1) is '"' && Char(2) is '"')
                        {
                            Next();
                            Next();
                            Next();

                            terminated = true;
                            break;
                        }

                        content.Append(Char());
                        Next();
                    }

                    if (!terminated)
                        throw new Error("Unterminated triple-quoted string", CurrentPos);

                    tokens.Add(new Token(TokenType.String, content.ToString(), position));

                    continue;
                }

                if (s_symbols.TryGetValue(tripleChar, out TokenType value))
                {
                    tokens.Add(new Token(value, tripleChar, CurrentPos));
                    Next();
                    Next();
                    Next();
                    continue;
                }
            }

            if (NotAtEnd(1))
            {
                string doubleChar = $"{Char()}{Char(1)}";
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
                    tokens.Add(new Token(value, doubleChar, CurrentPos));
                    Next();
                    Next();
                    continue;
                }
            }

            if (s_symbols.TryGetValue(Char().ToString(), out TokenType otherValue))
            {
                tokens.Add(new Token(otherValue, Char().ToString(), CurrentPos));
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
                Position position = CurrentPos;

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
                    throw new Error("Unterminated character", CurrentPos);

                tokens.Add(new Token(TokenType.Char, c.ToString(), position));
                Next();
                continue;
            }

            throw new Error($"Unexpected character '{Char()}'", CurrentPos);
        }

        return tokens;
    }

    Token LexString(bool allowEscapes, char target)
    {
        Position position = CurrentPos;

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
            throw new Error("Unterminated string", CurrentPos);

        Next();

        return new Token(TokenType.String, sb.ToString(), position);
    }

    Token LexAlpha()
    {
        Position position = CurrentPos;

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
        Position position = CurrentPos;

        if (Char() == '0' && NotAtEnd(1))
        {
            int numberBase = Char(1) switch
            {
                'b' or 'B' => 2,
                'o' or 'O' => 8,
                'x' or 'X' => 16,
                _ => 0
            };

            if (numberBase != 0)
            {
                Next();
                Next();

                BigInteger value = 0;
                bool hasDigits = false;

                while (NotAtEnd() && char.IsLetterOrDigit(Char()))
                {
                    char c = Char();

                    int digit = c switch
                    {
                        >= '0' and <= '9' => c - '0',
                        >= 'a' and <= 'f' => c - 'a' + 10,
                        >= 'A' and <= 'F' => c - 'A' + 10,
                        _ => -1
                    };

                    if (digit < 0 || digit >= numberBase)
                        throw new Error("Invalid digit in number", CurrentPos);

                    value = value * numberBase + digit;
                    hasDigits = true;
                    Next();
                }

                if (!hasDigits)
                    throw new Error("Expected digits after number prefeix", position);

                return new Token(TokenType.Number, value.ToString(), position);
            }
        }

        StringBuilder sb = new StringBuilder();
        bool hasDecimal = false;

        while (NotAtEnd() && (char.IsDigit(Char()) || Char() == '.'))
        {
            if (Char() == '.')
            {
                if (hasDecimal)
                    throw new Error("Number already contains a decimal", CurrentPos);

                if (!NotAtEnd(1) || !char.IsDigit(Char(1)))
                    throw new Error("Expected number after decimal", CurrentPos);

                hasDecimal = true;
            }
            sb.Append(Char());
            Next();
        }

        return new Token(TokenType.Number, sb.ToString(), position);
    }

    char LexEscape()
    {
        Position escapePos = CurrentPos;

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
            CurrentPos.Line++;
            CurrentPos.Column = 1;
        }
        else
            CurrentPos.Column++;
        _i++;
    }
}
