partial class Semantics
{
    (FunctionSymbol Symbol, string Name) BoundFunction(UnboundFunctionStmt unboundFunctionStmt, List<TypeSymbol> typeArguments, Position position)
    {
        if (typeArguments.Count != unboundFunctionStmt.TypeParameters.Count)
            throw new Error($"'{unboundFunctionStmt.Name}' expects {unboundFunctionStmt.TypeParameters.Count} argument(s), got {typeArguments.Count}", unboundFunctionStmt.Position);

        string key = $"function_{unboundFunctionStmt.Name}_" + string.Join("_", typeArguments.Select(TypeKey));

        if (BoundFunctions.TryGetValue(key, out var bound))
            return (bound.Type, key);

        Dictionary<string, TypeSymbol> pairedTypeArguments = unboundFunctionStmt.TypeParameters
            .Zip(typeArguments)
            .ToDictionary(x => x.First, x => x.Second);

        Dictionary<string, TypeSymbol?> previousTypes = [];

        foreach (var pair in pairedTypeArguments)
        {
            TypeRegistery.Registery.TryGetValue(pair.Key, out var previous);
            previousTypes.Add(pair.Key, previous);
            TypeRegistery.Registery[pair.Key] = pair.Value;
        }

        Scopes.BeginScope();

        Semantics semantics = new Semantics();

        var returnType = semantics.ResolveSymbol(unboundFunctionStmt.ReturnType);

        if (returnType is not VoidSymbol &&
            (unboundFunctionStmt.Body.Count == 0 ||
             unboundFunctionStmt.Body[^1] is not ReturnStmt returnStmt ||
             returnStmt.Expr is null))
        {
            throw new Error(
                $"Non-void function '{unboundFunctionStmt.Name}' must have a final return with a value",
                unboundFunctionStmt.Position
            );
        }

        var boundParameters = unboundFunctionStmt.Parameters
            .Select(x =>
            {
                if (x.IsVariadic)
                    return new BoundField(TypeSymbol.Void, x.Name, true);
                var type = semantics.ResolveSymbol(x.Symbol);
                type.RejectVoid("Cannont set a function parameter to null", x.Position);
                return new BoundField(type, x.Name, x.IsVariadic);
            }).ToList();

        foreach (var boundParam in boundParameters.Where(x => !x.IsVariadic))
            Scopes.Declare(boundParam.Name, boundParam.Type, unboundFunctionStmt.Position);

        FunctionSymbol functionSymbol = new(returnType, boundParameters
            .Where(x => !x.IsVariadic)
            .Select(x => x.Type)
            .ToList(),
            boundParameters.Any(x => x.IsVariadic)
        );

        List<Stmt> newBody = [];

        BoundFunctions[key] = (new(
            returnType,
            unboundFunctionStmt.Name,
            boundParameters,
            newBody,
            unboundFunctionStmt.Position
        ), functionSymbol);

        Scopes.FunctionLevel++;

        newBody.AddRange(semantics.Check(unboundFunctionStmt.Body));

        Scopes.FunctionLevel--;

        Scopes.EndScope();

        foreach (var pair in previousTypes)
            if (pair.Value == null)
                TypeRegistery.Registery.Remove(pair.Key);
            else
                TypeRegistery.Registery[pair.Key] = pair.Value;

        return (functionSymbol, key);
    }
}