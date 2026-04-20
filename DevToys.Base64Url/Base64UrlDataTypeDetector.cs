using System.ComponentModel.Composition;
using DevToys.Api;
using Microsoft.Extensions.Logging;

namespace DevToys.Base64Url;

[Export(typeof(IDataTypeDetector))]
[DataTypeName(PredefinedDataTypeNames.Base64UrlText, baseName: PredefinedCommonDataTypeNames.Text)]
internal sealed class Base64UrlDataTypeDetector : IDataTypeDetector
{
    private readonly ILogger _logger;

    [ImportingConstructor]
    public Base64UrlDataTypeDetector()
    {
        _logger = this.Log();
    }

    public ValueTask<DataDetectionResult> TryDetectDataAsync(object data, DataDetectionResult? resultFromBaseDetector, CancellationToken cancellationToken)
    {
        if (resultFromBaseDetector?.Data is not string textData)
        {
            return ValueTask.FromResult(DataDetectionResult.Unsuccessful);
        }

        if (string.IsNullOrWhiteSpace(textData))
        {
            return ValueTask.FromResult(DataDetectionResult.Unsuccessful);
        }

        // When auto-detecting, ignore all whitespace characters, as they are often copied by mistake.
        var cleanedTextData = new string(textData.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (Base64UrlHelper.IsBase64UrlDataStrict(cleanedTextData))
        {
            // A detector has 2 seconds to run, or it gets cancelled.
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(new DataDetectionResult(Success: true, Data: cleanedTextData));
        }

        return ValueTask.FromResult(DataDetectionResult.Unsuccessful);
    }
}

internal static class PredefinedDataTypeNames
{
    public const string Base64UrlText = "Base64UrlText";
}
