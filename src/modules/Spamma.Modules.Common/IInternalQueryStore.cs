using System.Runtime.CompilerServices;
using ResultMonad;

namespace Spamma.Modules.Common;

public interface IInternalQueryStore
{
    Result AddReferenceForObject(object obj);

    bool IsStoringReferenceForObject(object obj);
}

public class InternalQueryStore : IInternalQueryStore
{
    private ConditionalWeakTable<object, BoolWrapper> _table = new();

    public Result AddReferenceForObject(object obj)
    {
        this._table.AddOrUpdate(obj, new BoolWrapper());;
        return Result.Ok();
    }

    public bool IsStoringReferenceForObject(object obj)
    {
        return this._table.TryGetValue(obj, out var d) && d.Value;
    }
    
    private class BoolWrapper
    {
        public bool Value { get; } = true;
    }
}