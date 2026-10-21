using System.Diagnostics;
using System.Globalization;
using System.Numerics;

class Parser
{
    List<Token> _tokens;
    int _i = 0;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public List<Stmt> Parse()
    {
        List<Stmt> stmts = new List<Stmt>();

        while (NotAtEnd())
        {
            stmts.Add(ParseStmt());
        }

        return stmts;
    }

    Stmt ParseStmt()
    {
        if (Check(TokenType.Let))
            return ParseLet();
        if (Check(TokenType.Infer))
            return ParseInfer();
        return ParseExprStmt();
    }

    ExprStmt ParseExprStmt()
    {
        Expr expr = ParseExpr();
        Expect(TokenType.Semicolon);
        return new(expr, expr.Position);
    }

    UnboundLetStmt ParseLet()
    {
        Position position = PosNext();

        var symbol = ParseSymbol();

        string name = ParseName("Expected variable name");

        Expect(TokenType.Equal);

        var expr = ParseExpr();

        Expect(TokenType.Semicolon);

        return new(symbol, name, expr, position);
    }

    InferStmt ParseInfer()
    {
        Position position = PosNext();

        string name = ParseName("Expected variable name");

        Expect(TokenType.Equal);

        var expr = ParseExpr();

        Expect(TokenType.Semicolon);

        return new(name, expr, position);
    }

    Error ThrowUnexpected()
    {
        Token token = Current();
        string? keyword = Lexer.GetKeywordFromType(token.TokenType);
        string? symbol = Lexer.GetSymbolFromType(token.TokenType);
        if (keyword != null)
            throw new Error($"Unexpected keyword '{keyword}'", token.Position);
        else if (symbol != null)
            throw new Error($"Unexpected symbol '{symbol}'", token.Position);
        else
            throw new Error($"Unexpected token '{token.TokenType}'", token.Position);
    }

    string ParseName()
    {
        string name = Current().Lexeme;
        Eat("Expected name", TokenType.Identifier);
        return name;
    }

    string ParseName(string message)
    {
        string name = Current().Lexeme;
        Eat(message, TokenType.Identifier);
        return name;
    }

    // List<Stmt> ParseBody()
    // {
    //     List<Stmt> stmts = new List<Stmt>();

    //     while (NotAtEnd() && !Check(TokenType.End))
    //         stmts.Add(ParseStmt());

    //     Expect(TokenType.End);

    //     return stmts;
    // }

    List<string> ParseNames(TokenType end)
    {
        if (Match(end))
            return new List<string>();

        List<string> names = new List<string>()
            {
                ParseName()
            };

        while (Match(TokenType.Comma))
            names.Add(ParseName());

        Expect(end);

        return names;
    }

    List<string> ParseNames(TokenType start, TokenType end)
    {
        Expect(start);
        return ParseNames(end);
    }

    List<Expr> ParseArgs(TokenType start, TokenType end)
    {
        Expect(start);

        if (Match(end))
            return new List<Expr>();

        List<Expr> args = new List<Expr>()
            {
                ParseExpr()
            };

        while (Match(TokenType.Comma))
            args.Add(ParseExpr());

        Expect(end);

        return args;
    }

    PSymbol ParseSymbol()
    {
        Position position = Current().Position;

        // if (Match(TokenType.LeftBracket))
        // {
        //     var innerSymbol = ParseSymbol();
        //     Expect(TokenType.RightBracket);
        //     return new ArrayPSymbol(innerSymbol, position);
        // }

        string name = ParseName("Expected type name");

        PSymbol symbol = new NamePSymbol(name, position);

        while (Match(TokenType.BackTick))
            symbol = new PointerPSymbol(symbol, position);

        return symbol;
    }

    bool Check(params TokenType[] types)
    {
        for (int i = 0; i < types.Length; i++)
            if (Current().TokenType == types[i])
                return true;
        return false;
    }

    Token Current() => _tokens[_i];
    Token Peek() => _tokens[_i + 1];
    bool NotAtEnd() => !Check(TokenType.Eof);
    bool PeekNotAtEnd() => _i + 1 < _tokens.Count;
    bool AtEnd() => Check(TokenType.Eof);
    void Next() => _i++;

    Position PosNext()
    {
        Position pos = Current().Position;
        Next();
        return pos;
    }

    void Eat(string message, params TokenType[] types)
    {
        if (Check(types))
        {
            Next();
            return;
        }
        throw new Error(message, Current().Position);
    }

    bool Match(params TokenType[] types)
    {
        if (Check(types))
        {
            Next();
            return true;
        }
        return false;
    }

    void Expect(TokenType type)
    {
        if (Check(type))
        {
            Next();
            return;
        }
        string? keyword = Lexer.GetKeywordFromType(type);
        string? symbol = Lexer.GetSymbolFromType(type);
        if (keyword != null)
            throw new Error($"Expected keyword '{keyword}'", Current().Position);
        else if (symbol != null)
            throw new Error($"Expected symbol '{symbol}'", Current().Position);
        else
            throw new Error($"Expected token '{type}'", Current().Position);
    }

    Expr ParsePrimary()
    {
        Token token = Current();

        if (Match(TokenType.Number))
        {
            if (BigInteger.TryParse(token.Lexeme, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bigIntResult))
                return new IntExpr(bigIntResult, token.Position);

            else if (double.TryParse(token.Lexeme, CultureInfo.InvariantCulture, out double doubleResult))
                return new FloatExpr(doubleResult, token.Position);

            throw new UnreachableException();
        }
        else if (Match(TokenType.String))
            return new StringExpr(token.Lexeme, token.Position);
        else if (Match(TokenType.Char))
            return new CharExpr(token.Lexeme[0], token.Position);
        else if (Match(TokenType.True))
            return new BoolExpr(true, token.Position);
        else if (Match(TokenType.False))
            return new BoolExpr(false, token.Position);
        else if (Match(TokenType.Identifier))
        {
            return new NameExpr(token.Lexeme, token.Position);
        }
        // else if (Match(TokenType.Null))
        //     return new NullExpr(token.Position);
        else if (Match(TokenType.LeftParen))
        {
            Expr expr = ParseExpr();
            Expect(TokenType.RightParen);
            return expr;
        }

        throw ThrowUnexpected();
    }

    Expr ParsePostfix()
    {
        Expr left = ParsePrimary();
        // while (Check(TokenType.LeftParen, TokenType.LeftBracket, TokenType.Period))
        // {
        //     if (Check(TokenType.LeftParen))
        //     {
        //         Position position = Current().Position;

        //         List<Expr> args = ParseArgs(TokenType.LeftParen, TokenType.RightParen);

        //         left = new CallExpr(left, args, position);

        //         continue;
        //     }

        //     if (Check(TokenType.LeftBracket))
        //     {
        //         Position position = Current().Position;

        //         Expect(TokenType.LeftBracket);

        //         Expr index = ParseExpr();

        //         Expect(TokenType.RightBracket);

        //         left = new IndexExpr(left, index, position);

        //         continue;
        //     }

        //     if (Check(TokenType.Period))
        //     {
        //         Position position = Current().Position;

        //         Next();

        //         string name = ParseName();

        //         left = new MemberExpr(left, name, position);

        //         continue;
        //     }

        //     break;
        // }
        return left;
    }

    Expr ParseUnary()
    {
        if (Check(TokenType.Sub, TokenType.Bang, TokenType.BitwiseNot, TokenType.BitwiseAnd, TokenType.BackTick /* TokenType.At */))
        {
            Token op = Current();

            Next();

            Expr right = ParseUnary();

            return new UnaryExpr(right, op.TokenType, op.Position);
        }
        return ParsePostfix();
    }

    Expr ParseTerm()
    {
        Expr left = ParseUnary();

        while (Check(TokenType.Mul, TokenType.Div, TokenType.Mod))
        {
            Token op = Current();

            Next();

            Expr right = ParseUnary();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseFactor()
    {
        Expr left = ParseTerm();

        while (Check(TokenType.Add, TokenType.Sub))
        {
            Token op = Current();

            Next();

            Expr right = ParseTerm();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseBitwiseShift()
    {
        Expr left = ParseFactor();

        while (Check(TokenType.BitwiseLeftShift, TokenType.BitwiseRightShift))
        {
            Token op = Current();

            Next();

            Expr right = ParseFactor();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseBitwiseAnd()
    {
        Expr left = ParseBitwiseShift();

        while (Check(TokenType.BitwiseAnd))
        {
            Token op = Current();

            Next();

            Expr right = ParseBitwiseShift();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseBitwiseXor()
    {
        Expr left = ParseBitwiseAnd();

        while (Check(TokenType.BitwiseXor))
        {
            Token op = Current();

            Next();

            Expr right = ParseBitwiseAnd();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseBitwiseOr()
    {
        Expr left = ParseBitwiseXor();

        while (Check(TokenType.BitwiseOr))
        {
            Token op = Current();

            Next();

            Expr right = ParseBitwiseXor();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseComparison()
    {
        Expr left = ParseBitwiseOr();

        while (Check(TokenType.Less, TokenType.Greater, TokenType.LessEq, TokenType.GreaterEq))
        {
            Token op = Current();

            Next();

            Expr right = ParseBitwiseOr();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseEquality()
    {
        Expr left = ParseComparison();

        while (Check(TokenType.NotEqual, TokenType.IsEqual))
        {
            Token op = Current();

            Next();

            Expr right = ParseComparison();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseAnd()
    {
        Expr left = ParseEquality();

        while (Check(TokenType.And))
        {
            Token op = Current();

            Next();

            Expr right = ParseEquality();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseOr()
    {
        Expr left = ParseAnd();

        while (Check(TokenType.Or))
        {
            Token op = Current();

            Next();

            Expr right = ParseAnd();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseAssign()
    {
        Expr left = ParseOr();

        if (Match(TokenType.Equal))
        {
            Expr expr = ParseAssign();

            if (left is NameExpr nameExpr)
                return new AssignExpr(nameExpr.Name, expr, nameExpr.Position);

            if (left is UnaryExpr unaryExpr && unaryExpr.Op is TokenType.BackTick)
                return new PointerAssignExpr(unaryExpr, expr, unaryExpr.Position);

            throw new Error("Invalid assign target", left.Position);
        }

        return left;
    }

    Expr ParseExpr() => ParseAssign();
}
