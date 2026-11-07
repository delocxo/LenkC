using System.Diagnostics;

partial class Semantics
{
    Stmt CheckStmt(Stmt stmt)
    {
        if (stmt is UnboundLetStmt unboundLetStmt)
        {
            var type = ResolveSymbol(unboundLetStmt.PSymbol)
                .RejectVoid("Cannot use void as a variable type", unboundLetStmt.Position);

            if (unboundLetStmt.Expr == null)
            {
                Scopes.Declare(unboundLetStmt.Name, type, unboundLetStmt.Position);

                return new BoundLetStmt(
                    type,
                    unboundLetStmt.Name,
                    null,
                    unboundLetStmt.Position
                );

            }
            var resolvedExpr = CheckExpr(unboundLetStmt.Expr);
            if (resolvedExpr.TypeSymbol is NullSymbol && type is not PointerSymbol)
                throw new Error("Cannont assign null to a non-pointer variable", unboundLetStmt.Position);

            resolvedExpr.TypeSymbol.RejectVoid("Expression cannot be void", unboundLetStmt.Position);

            Scopes.Declare(unboundLetStmt.Name, type, unboundLetStmt.Position);

            return new BoundLetStmt(
                type,
                unboundLetStmt.Name,
                resolvedExpr,
                unboundLetStmt.Position
            );
        }
        else if (stmt is InferStmt inferStmt)
        {
            var resolvedExpr = CheckExpr(inferStmt.Expr);
            resolvedExpr.TypeSymbol.RejectVoid("Expression cannot be void", inferStmt.Position);
            if (resolvedExpr.TypeSymbol is NullSymbol)
                throw new Error("Cannot infer a variable from null", inferStmt.Position);
            Scopes.Declare(inferStmt.Name, resolvedExpr.TypeSymbol, inferStmt.Position);
            return new BoundLetStmt(
                resolvedExpr.TypeSymbol,
                inferStmt.Name,
                resolvedExpr,
                inferStmt.Position
            );
        }
        else if (stmt is ExprStmt exprStmt)
        {
            if (exprStmt.Expr is not (
                AssignExpr or
                PointerAssignExpr or
                UnboundNativeExpr or
                CallExpr or
                IndexAssignExpr or
                MemberAssignExpr
            ))
                throw new Error($"Expression statement can only be an assignment or call: {exprStmt.Expr.GetType().Name}", exprStmt.Position);
            return new ExprStmt(
                CheckExpr(exprStmt.Expr).Expr,
                exprStmt.Position
            );
        }
        else if (stmt is IfStmt ifStmt)
        {
            bool compTime = ifStmt.CompTime;
            List<IfBranch> branches = [];

            foreach (var branch in ifStmt.IfBranches)
            {
                if (compTime && branch.Expr is not (TypeIsExpr or HasTagExpr))
                    throw new Error("Compile time branches conditions can only be typeis or hastag for now", ifStmt.Position);

                var expr = CheckExpr(branch.Expr).Expr;

                if (compTime)
                {
                    if (expr is not BoolExpr b)
                        throw new Error("Expected compile-time boolean", branch.Expr.Position);

                    if (!b.Value)
                        continue;
                }

                Scopes.BeginScope();
                var stmts = Check(branch.Stmts);
                Scopes.EndScope();

                branches.Add(new IfBranch(expr, stmts));

                if (compTime)
                    break;
            }

            List<Stmt>? elseBranch = null;
            if (ifStmt.ElseBranch != null && (!compTime || branches.Count == 0))
            {
                Scopes.BeginScope();
                var elseBranch2 = Check(ifStmt.ElseBranch);
                Scopes.EndScope();
                if (compTime && branches.Count == 0)
                    branches.Add(new(new BoolExpr(true, ifStmt.Position), elseBranch2));
                else
                    elseBranch = elseBranch2;
            }

            return new IfStmt(branches, elseBranch, ifStmt.Position);
        }
        else if (stmt is WhileStmt whileStmt)
        {
            var branch = whileStmt.Branch;
            var expr = CheckExpr(branch.Expr).Expr;
            Scopes.BeginScope();
            _loopDepth++;
            var stmts = Check(branch.Stmts);
            _loopDepth--;
            Scopes.EndScope();
            return new WhileStmt(new IfBranch(expr, stmts), whileStmt.Position);
        }
        else if (stmt is BreakStmt breakStmt)
        {
            if (_loopDepth <= 0)
                throw new Error("Cannont use break outside a loop", breakStmt.Position);
            return breakStmt;
        }
        else if (stmt is ContinueStmt continueStmt)
        {
            if (_loopDepth <= 0)
                throw new Error("Cannot use continue outside a loop", continueStmt.Position);
            return continueStmt;
        }
        else if (stmt is UnboundFunctionStmt unboundFunctionStmt)
        {
            if (!Scopes.AtGlobalLevel)
                throw new Error($"Function '{unboundFunctionStmt.Name}' can only be declared at the top-level", unboundFunctionStmt.Position);
            return unboundFunctionStmt;
        }
        else if (stmt is ReturnStmt returnStmt)
        {
            if (Scopes.FunctionLevel == 0)
                throw new Error("Cannot use a return outside a function", returnStmt.Position);
            if (returnStmt.Expr == null)
                return returnStmt;
            return new ReturnStmt(
                CheckExpr(returnStmt.Expr),
                returnStmt.Position
            );
        }
        else if (stmt is UnboundStructStmt structStmt)
        {
            if (!Scopes.AtGlobalLevel)
                throw new Error($"Struct '{structStmt.Name}' can only be declared at the top-level", structStmt.Position);
            return structStmt;
        }
        else if (stmt is TypeStmt unboundTypeStmt)
        {
            if (!Scopes.AtGlobalLevel)
                throw new Error($"Type '{unboundTypeStmt.Name}' can only be defined at the top-level", unboundTypeStmt.Position);
            return unboundTypeStmt;
        }
        else if (stmt is UnboundEnumStmt unboundEnumStmt)
        {
            if (!Scopes.AtGlobalLevel)
                throw new Error($"Enum '{unboundEnumStmt.Name}' can only be defined at the top-level", unboundEnumStmt.Position);
            return unboundEnumStmt;
        }
        else if (stmt is UnboundExternFunctionStmt unboundExternFunctionStmt)
        {
            if (!Scopes.AtGlobalLevel)
                throw new Error($"Extern '{unboundExternFunctionStmt.Name}' can only be defined at the top-level", unboundExternFunctionStmt.Position);
            return unboundExternFunctionStmt;
        }
        else if (stmt is TagStmt tagStmt)
        {
            if (!Scopes.AtGlobalLevel)
                throw new Error($"Tags can only be defined at the top-level", tagStmt.Position);
            return tagStmt;
        }

        throw new UnreachableException($"Invalid statement: {stmt.GetType().Name}");
    }
}