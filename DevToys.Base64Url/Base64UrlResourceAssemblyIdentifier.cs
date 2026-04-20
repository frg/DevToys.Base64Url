using System.ComponentModel.Composition;
using DevToys.Api;

namespace DevToys.Base64Url;

[Export(typeof(IResourceAssemblyIdentifier))]
[Name(nameof(Base64UrlResourceAssemblyIdentifier))]
public sealed class Base64UrlResourceAssemblyIdentifier : IResourceAssemblyIdentifier
{
    public ValueTask<FontDefinition[]> GetFontDefinitionsAsync()
    {
        return ValueTask.FromResult(Array.Empty<FontDefinition>());
    }
}
