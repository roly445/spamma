using System.Runtime.CompilerServices;
using ResultMonad;

namespace Spamma.Modules.Common;

public interface IInternalQueryStore
{
    Result StoreQueryRef(object obj);

    bool IsQueryStored(object obj);
}

public class InternalQueryStore : IInternalQueryStore
{
    private readonly ConditionalWeakTable<object, BoolWrapper> _table = new();

    public Result StoreQueryRef(object obj)
    {
        this._table.AddOrUpdate(obj, new BoolWrapper());
        return Result.Ok();
    }

    public bool IsQueryStored(object obj)
    {
        return this._table.TryGetValue(obj, out var d) && d.Value;
    }

    private sealed class BoolWrapper
    {
        public bool Value { get; } = true;
    }
}