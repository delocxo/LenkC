using System.Diagnostics;
using System.Globalization;
using System.Numerics;

record Macro(string Name, List<string> Parameters, List<Token> Body);

class Parser
{
    public static Dictionary<string, Macro> Macros { get; } = [];

    List<Token> _tokens;
    int _i = 0;
    int _depth = 0;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public List<Stmt> Parse()
    {
        List<Stmt> stmts = new List<Stmt>();

        while (NotAtEnd())
        {
            var stmt = ParseStmt();
            if (stmt != null)
                stmts.Add(stmt);
        }

        return stmts;
    }

    Stmt? ParseStmt()
    {
        if (Check(TokenType.Let))
            return ParseLet();
        if (Check(TokenType.Infer))
            return ParseInfer();
        if (Check(TokenType.If))
            return ParseIf();
        if (Check(TokenType.While))
            return ParseWhile();
        if (Check(TokenType.Break))
            return ParseBreak();
        if (Check(TokenType.Continue))
            return ParseContinue();
        if (Check(TokenType.At))
            return ParseFunction();
        if (Check(TokenType.Return))
            return ParseReturn();
        if (Check(TokenType.Struct))
            return ParseStruct();
        if (Check(TokenType.Type))
            return ParseType();
        if (Check(TokenType.Union))
            return ParseStruct();
        if (Match(TokenType.String))
            return null;
        if (Check(TokenType.Enum))
            return ParseEnum();
        if (Check(TokenType.Extern))
            return ParseExternFunction();
        if (Check(TokenType.Macro))
        {
            if (_depth > 0)
                throw new Error("Macros can only be declared at the top level", Current().Position);
            Next();
            ParseMacro();
            return null;
        }
        if (Check(TokenType.Identifier) && Macros.TryGetValue(Current().Lexeme, out var macro))
        {
            Position position = PosNext();
            ExpandMacro(macro, position);
            return ParseStmt();
        }
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

        if (Match(TokenType.Semicolon))
            return new(symbol, name, null, position);

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

    IfStmt ParseIf()
    {
        Position position = PosNext();

        List<IfBranch> branches = [ParseBranch()];

        while (Check(TokenType.Else) && PeekNotAtEnd() && Peek().TokenType == TokenType.If)
        {
            Next();
            Next();
            branches.Add(ParseBranch());
        }

        List<Stmt>? elseBranch = null;

        if (Match(TokenType.Else))
            elseBranch = ParseBody();

        return new(branches, elseBranch, position);
    }

    WhileStmt ParseWhile()
    {
        Position position = PosNext();

        IfBranch branch = ParseBranch();

        return new(branch, position);
    }

    BreakStmt ParseBreak()
    {
        Position position = PosNext();
        Expect(TokenType.Semicolon);
        return new(position);
    }

    ContinueStmt ParseContinue()
    {
        Position position = PosNext();
        Expect(TokenType.Semicolon);
        return new(position);
    }

    UnboundFunctionStmt ParseFunction()
    {
        Position position = PosNext();

        var returnSymbol = ParseSymbol();

        string name = ParseName("Expected function or parent name");
        string? parent = null;

        if (Match(TokenType.Period))
        {
            string functionName = ParseName("Expected function name");
            parent = name;
            name = functionName;
        }

        List<string> typeParameters = [];

        if (Check(TokenType.Less))
            typeParameters = ParseNames(TokenType.Less, TokenType.Greater);

        Expect(TokenType.LeftParen);

        List<UnboundField> parameters = [];

        if (!Check(TokenType.RightParen))
        {
            parameters.Add(ParseField(true));

            while (Match(TokenType.Comma))
                parameters.Add(ParseField(true));
        }

        Expect(TokenType.RightParen);

        var body = ParseBody();

        return new(returnSymbol, name, typeParameters, parameters, body, position, parent: parent);
    }

    ReturnStmt ParseReturn()
    {
        Position position = PosNext();

        if (Match(TokenType.Semicolon))
            return new ReturnStmt(null, position);

        Expr expr = ParseExpr();

        Expect(TokenType.Semicolon);

        return new(expr, position);
    }

    /*
    ????
    HORRIBLE
    HORRIBLE
    will remake later 
    */
    UnboundStructStmt ParseStruct()
    {
        Position position = Current().Position;

        bool isUnion = Match(TokenType.Union);

        Expect(TokenType.Struct);

        string name = ParseName("Expected struct name");

        List<string> typeParameters = [];

        if (Check(TokenType.Less))
            typeParameters = ParseNames(TokenType.Less, TokenType.Greater);

        Expect(TokenType.LeftBrace);

        List<UnboundField> fields = [];

        while (!Check(TokenType.RightBrace))
        {
            fields.Add(ParseField(false));
            Expect(TokenType.Semicolon);
        }

        Expect(TokenType.RightBrace);

        return new(name, typeParameters, fields, position, isUnion: isUnion);
    }

    TypeStmt ParseType()
    {
        Position position = PosNext();

        string name = ParseName("Expected type name");

        Expect(TokenType.Equal);

        string cname = Current().Lexeme;
        Eat("Expected a string cname", TokenType.String);

        // I COULD check this in semantics but honestly, why not here?
        if (Match(TokenType.Numeric))
        {
            Expect(TokenType.LeftParen);

            Expr bitsExpr = ParseExpr();

            if (bitsExpr is not IntExpr intExpr)
                throw new Error("Expected bits to be an int", bitsExpr.Position);

            if (intExpr.Value < 1 || intExpr.Value > 128)
                throw new Error("Bit count must be between 1 and 128", intExpr.Position);

            Expect(TokenType.Comma);

            Expr isFloatExpr = ParseExpr();

            if (isFloatExpr is not BoolExpr boolExpr)
                throw new Error("Expected isfloat to be a bool", isFloatExpr.Position);

            Expect(TokenType.Comma);

            Expr isSignedExpr = ParseExpr();

            if (isSignedExpr is not BoolExpr sBoolExpr)
                throw new Error("Expected issigned to be a bool", isSignedExpr.Position);

            Expect(TokenType.Comma);

            Expr formatExpr = ParseExpr();

            if (formatExpr is not StringExpr stringExpr)
                throw new Error("Expected format to be a string", isSignedExpr.Position);

            Expect(TokenType.RightParen);

            Expect(TokenType.Semicolon);

            return new TypeStmt(
                name,
                cname,
                position,
                TypeofType.Numeric,
                new(
                    name,
                    cname,
                    (int)intExpr.Value,
                    boolExpr.Value,
                    sBoolExpr.Value,
                    stringExpr.Value
                )
            );
        }
        else if (Check(TokenType.Union) || Match(TokenType.Struct))
        {
            bool isUnion = Match(TokenType.Union);

            if (isUnion)
                Expect(TokenType.Struct);

            Expect(TokenType.LeftBrace);

            List<UnboundField> fields = [];

            while (!Check(TokenType.RightBrace))
            {
                fields.Add(ParseField(false));
                Expect(TokenType.Semicolon);
            }

            Expect(TokenType.RightBrace);

            return new(name, cname, position, TypeofType.Struct, null, new(name, [], fields, position, false, cname, isUnion: isUnion));
        }

        Expect(TokenType.Semicolon);

        return new(name, cname, position, TypeofType.Regular);
    }

    UnboundEnumStmt ParseEnum()
    {
        UnboundEnumValue ParseEnumValue()
        {
            Position position = Current().Position;

            string name = ParseName("Exepcted enum value name");

            if (Match(TokenType.Equal))
            {
                Expr expr = ParseExpr();
                return new(name, expr, position);
            }

            return new(name, null, position);
        }

        Position position = PosNext();

        var symbol = ParseSymbol();

        string name = ParseName("Expected enum name");

        List<UnboundEnumValue> values = [];

        Expect(TokenType.LeftBrace);

        if (!Check(TokenType.RightBrace))
        {
            values.Add(ParseEnumValue());
            while (Match(TokenType.Comma))
                values.Add(ParseEnumValue());
        }

        Expect(TokenType.RightBrace);

        return new(symbol, name, values, position);
    }

    UnboundExternFunctionStmt ParseExternFunction()
    {
        Position position = PosNext();

        string? cname = null;

        if (Match(TokenType.LeftParen))
        {
            cname = Current().Lexeme;
            Eat("Expected extern C name", TokenType.String);
            Expect(TokenType.RightParen);
        }

        Expect(TokenType.At);

        var returnSymbol = ParseSymbol();

        string name = ParseName();
        string? parent = null;

        if (Match(TokenType.Period))
        {
            string functionName = ParseName("Expected function name");
            parent = name;
            name = functionName;
        }

        Expect(TokenType.LeftParen);

        List<UnboundField> parameters = [];

        if (!Check(TokenType.RightParen))
        {
            parameters.Add(ParseField(true));

            while (Match(TokenType.Comma))
                parameters.Add(ParseField(true));
        }

        Expect(TokenType.RightParen);

        Expect(TokenType.Semicolon);

        return new(name, returnSymbol, parameters, position, cname == null ? name : cname, parent);
    }

    void ParseMacro()
    {
        string name = ParseName("Expected macro name");

        if (Macros.ContainsKey(name))
            throw new Error($"'{name}' is an already existing macro", _tokens[_i - 2].Position);

        List<string> parameters = [];

        if (Check(TokenType.LeftParen))
            parameters = ParseNames(TokenType.LeftParen, TokenType.RightParen);

        Expect(TokenType.Equal);

        List<Token> body = [];

        if (!Check(TokenType.LeftParen))
        {
            body.Add(Current());
            Next();
        }
        else
        {
            Expect(TokenType.LeftParen);

            int depth = 1;

            while (depth > 0)
            {
                Token currnet = Current();

                if (currnet.TokenType == TokenType.Eof)
                    throw new Error("Unterminated macro body", currnet.Position);

                if (currnet.TokenType == TokenType.LeftParen)
                    depth++;
                else if (currnet.TokenType == TokenType.RightParen)
                    depth--;

                if (depth > 0)
                    body.Add(currnet);

                Next();
            }
        }

        Expect(TokenType.Semicolon);

        Macros.Add(name, new(name, parameters, body));
    }

    IfBranch ParseBranch()
    {
        Expect(TokenType.LeftParen);

        Expr condition = ParseExpr();

        Expect(TokenType.RightParen);

        var body = ParseBody();

        return new(condition, body);
    }


    UnboundField ParseField(bool isFunciton)
    {
        Position pos = Current().Position;
        if (isFunciton && Match(TokenType.Variadic))
            return new(new PSymbol(pos), "variadic", pos, true);
        var symbol = ParseSymbol();
        string name = ParseName("Expected parameter name");
        return new(symbol, name, pos, false);
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

    List<Stmt> ParseBody()
    {
        _depth++;

        List<Stmt> stmts = new List<Stmt>();

        if (!Check(TokenType.LeftBrace))
        {
            var stmt = ParseStmt();
            if (stmt != null)
                stmts.Add(stmt);
            _depth--;
            return stmts;
        }

        Expect(TokenType.LeftBrace);

        while (NotAtEnd() && !Check(TokenType.RightBrace))
        {
            var stmt = ParseStmt();
            if (stmt != null)
                stmts.Add(stmt);
        }

        _depth--;

        Expect(TokenType.RightBrace);

        return stmts;
    }

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

    List<PSymbol> ParseTypeArguments()
    {
        if (!Match(TokenType.DoubleColon))
            return [];

        Expect(TokenType.Less);

        List<PSymbol> names = [ParseSymbol()];

        while (Match(TokenType.Comma))
            names.Add(ParseSymbol());

        Expect(TokenType.Greater);

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

        PSymbol symbol;

        if (Match(TokenType.LeftParen))
        {
            symbol = ParseSymbol();
            Expect(TokenType.RightParen);
        }
        else if (Match(TokenType.At))
        {
            Expect(TokenType.LeftParen);

            List<FunctionParameterPSymbol> parameters = [];

            if (!Check(TokenType.RightParen))
            {
                do
                {
                    Position pos = Current().Position;
                    if (Match(TokenType.Variadic))
                        parameters.Add(new FunctionParameterPSymbol(new PSymbol(pos), true));
                    else
                        parameters.Add(
                            new FunctionParameterPSymbol(ParseSymbol(), false)
                        );
                }
                while (Match(TokenType.Comma));
            }

            Expect(TokenType.RightParen);

            Expect(TokenType.Arrow);

            PSymbol returnType = ParseSymbol();

            symbol = new FunctionPSymbol(returnType, parameters, position);
        }
        else
        {
            string name = ParseName("Expected type name");
            List<PSymbol> typeArguments = ParseTypeArguments();
            symbol = new NamePSymbol(name, typeArguments, position);
        }

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

    bool PeekMatch(params TokenType[] types)
    {
        if (_i + 1 >= _tokens.Count)
            return false;

        if (types.Contains(_tokens[_i + 1].TokenType))
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
            if (Macros.TryGetValue(token.Lexeme, out var macro))
            {
                ExpandMacro(macro, token.Position);
                return ParseExpr();
            }
            List<PSymbol> typeParameters = ParseTypeArguments();
            return new NameExpr(token.Lexeme, typeParameters, token.Position);
        }
        else if (Match(TokenType.Null))
            return new NullExpr(token.Position);
        else if (Match(TokenType.LeftParen))
        {
            Expr expr = ParseExpr();
            Expect(TokenType.RightParen);
            return expr;
        }
        else if (Match(TokenType.Native))
        {
            Expect(TokenType.LeftParen);

            var symbol = ParseSymbol();

            Expect(TokenType.Comma);

            string text = Current().Lexeme;
            Expect(TokenType.String);

            Expect(TokenType.RightParen);

            return new UnboundNativeExpr(symbol, text, token.Position);
        }
        else if (Match(TokenType.Cast))
        {
            Expect(TokenType.LeftParen);

            var symbol = ParseSymbol();

            Expect(TokenType.Comma);

            var value = ParseExpr();

            Expect(TokenType.RightParen);

            return new UnboundCastExpr(symbol, value, token.Position);
        }
        else if (Match(TokenType.CName))
        {
            Expect(TokenType.LeftParen);

            var symbol = ParseSymbol();

            Expect(TokenType.RightParen);

            return new UnboundCNameExpr(symbol, token.Position);
        }
        else if (Match(TokenType.Fmt))
        {
            Expect(TokenType.LeftParen);

            var symbol = ParseSymbol();

            Expect(TokenType.RightParen);

            return new UnboundFmtExpr(symbol, token.Position);
        }
        else if (Match(TokenType.TypeIs))
        {
            Expect(TokenType.LeftParen);

            var left = ParseSymbol();

            Expect(TokenType.Comma);

            var right = ParseSymbol();

            Expect(TokenType.RightParen);

            return new TypeIsExpr(left, right, token.Position);
        }

        throw ThrowUnexpected();
    }

    Expr ParsePostfix()
    {
        Expr left = ParsePrimary();
        while (Check(TokenType.LeftParen, TokenType.LeftBracket, TokenType.Period, TokenType.LeftBrace))
        {
            if (Check(TokenType.LeftParen))
            {
                Position position = Current().Position;

                List<Expr> args = ParseArgs(TokenType.LeftParen, TokenType.RightParen);

                left = new CallExpr(left, args, position);

                continue;
            }

            if (Check(TokenType.LeftBracket))
            {
                Position position = Current().Position;

                Expect(TokenType.LeftBracket);

                Expr index = ParseExpr();

                Expect(TokenType.RightBracket);

                left = new IndexExpr(left, index, position);

                continue;
            }

            if (Check(TokenType.Period))
            {
                Position position = PosNext();

                Position nameExprPosition = Current().Position;

                string name = ParseName();
                var typeArguments = ParseTypeArguments();

                left = new MemberExpr(left, new NameExpr(name, typeArguments, nameExprPosition), position);

                continue;
            }

            if (Check(TokenType.LeftBrace))
            {
                Position position = Current().Position;

                Next();

                string name = ParseName("Expected member name");

                Expect(TokenType.Equal);

                var expr = ParseExpr();

                Expect(TokenType.RightBrace);

                left = new UnboundInitExpr(left, name, expr, position);

                continue;
            }

            break;
        }
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

        while (true)
        {
            TokenType targetType = TokenType.Eof;

            if (Check(TokenType.Less) && PeekNotAtEnd() && Peek().TokenType == TokenType.Less)
                targetType = TokenType.BitwiseLeftShift;
            else if (Check(TokenType.Greater) && PeekNotAtEnd() && Peek().TokenType == TokenType.Greater)
                targetType = TokenType.BitwiseRightShift;
            else
                break;

            Token op = Current();

            Next();
            Next();

            Expr right = ParseFactor();

            left = new BinaryExpr(left, right, targetType, op.Position);
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

            if (left is IndexExpr indexExpr)
                return new IndexAssignExpr(indexExpr, expr);

            if (left is MemberExpr memberExpr)
            {
                if (memberExpr.Member.TypeArguments.Count > 0)
                    throw new Error($"Member assignment takes 0 type arguments", memberExpr.Position);
                return new MemberAssignExpr(memberExpr.Target, memberExpr.Member.Name, expr, memberExpr.Position);
            }

            throw new Error("Invalid assign target", left.Position);
        }

        return left;
    }

    public Expr ParseExpr() => ParseAssign();

    public static Expr ParseExprText(string text, string source)
    {
        Lexer lexer = new Lexer(text, source);
        var tokens = lexer.Lex();
        tokens.Add(new Token(TokenType.Eof, "End of File", lexer.CurrentPos));
        Parser parser = new Parser(tokens);
        return parser.ParseExpr();
    }

    List<Token> ExpandMacro(Macro macro, Position position)
    {
        if (macro.Parameters.Count == 0)
        {
            _i--;
            _tokens.RemoveAt(_i);
            _tokens.InsertRange(_i, macro.Body);
            return macro.Body;
        }

        int start = _i - 1;

        Expect(TokenType.LeftParen);

        List<List<Token>> arguments = [];

        do
        {
            List<Token> argument = [];
            int depth = 0;

            while (depth > 0 || !Check(TokenType.Comma, TokenType.RightParen))
            {
                Token token = Current();

                if (Check(TokenType.Eof))
                    throw new Error("Unterminated macro arguments", token.Position);

                if (Check(TokenType.LeftParen)) depth++;
                if (Check(TokenType.RightParen)) depth--;

                argument.Add(token);
                Next();
            }

            if (argument.Count == 0)
                throw new Error("Expected macro argument", Current().Position);

            arguments.Add(argument);
        } while (Match(TokenType.Comma));

        Expect(TokenType.RightParen);

        if (arguments.Count != macro.Parameters.Count)
            throw new Error($"Macro '{macro.Name}' expects {macro.Parameters.Count} arguments, got {arguments.Count}", position);

        List<Token> newBody = [];
        var bindings = macro.Parameters
            .Zip(arguments)
            .ToDictionary(x => x.First, x => x.Second);

        foreach (var token in macro.Body)
        {
            if (bindings.TryGetValue(token.Lexeme, out var arg))
                newBody.AddRange(arg);
            else
                newBody.Add(token);
        }

        _tokens.RemoveRange(start, _i - start);
        _tokens.InsertRange(start, newBody);
        _i = start;

        return newBody;
    }
}
