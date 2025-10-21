using System.Diagnostics.CodeAnalysis;

namespace Spamma.Modules.Common.Application.Contracts;

public interface IFileWrapper
{
    void Delete(string path);
}

[ExcludeFromCodeCoverage]
public class FileWrapper : IFileWrapper
{
    public void Delete(string path)
    {
        File.Delete(path);
    }
}