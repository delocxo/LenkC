internal enum TokenType
{
    String, Number, Identifier, Char,

    True, False, Null, End, If, Else,
    While, Break, Continue, Function,
    Return, Use, Record, Enum, Import,
    For, In, Elif, Let, Infer, Native,
    GlobalNative, Cast, CName, Fmt,
    Struct, Type, Numeric, TypeIs,
    Union, Extern,

    Add, Sub, Mul, Div, Mod,
    IsEqual, NotEqual, Less, Greater,
    LessEq, GreaterEq, And, Or, Bang,
    BitwiseNot, BitwiseLeftShift, BitwiseRightShift,
    BitwiseAnd, BitwiseOr, BitwiseXor,

    Equal, Semicolon, LeftBracket, RightBracket,
    LeftBrace, RightBrace, LeftParen, RightParen,
    Comma, Period, Hash, Arrow, At, BackTick,
    Variadic, DoubleColon, TripleQuotes,

    Eof,
}

internal record Token(TokenType TokenType, string Lexeme, Position Position);