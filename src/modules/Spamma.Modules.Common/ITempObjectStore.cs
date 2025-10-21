using ResultMonad;

namespace Spamma.Modules.Common;

public interface ITempObjectStore
{
    Result AddReferenceForObject(object obj);

    bool IsStoringReferenceForObject(object obj);
}

public class TempObjectStore : ITempObjectStore
{
    private readonly List<WeakReference<object>> _weakList = new();

    public Result AddReferenceForObject(object obj)
    {
        this._weakList.Add(new WeakReference<object>(obj));
        return Result.Ok();
    }

    public bool IsStoringReferenceForObject(object obj)
    {
        return this._weakList.Any(wr => wr.TryGetTarget(out var target) && ReferenceEquals(target, obj));
    }
}