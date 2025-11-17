namespace Spamma.Tools.EmailLoadTester;

internal class SimpleArgs
{
    private readonly Dictionary<string, string?> _map = new();

    public SimpleArgs(string[] args)
    {
        var i = 0;
        while (i < args.Length)
        {
            var a = args[i];
            if (a.StartsWith("--"))
            {
                var key = a[2..];
                string? val = null;
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    val = args[i + 1];
                    i++;
                }

                this._map[key] = val;
            }

            i++;
        }
    }

    public string? Get(string key) => this._map.GetValueOrDefault(key);
}