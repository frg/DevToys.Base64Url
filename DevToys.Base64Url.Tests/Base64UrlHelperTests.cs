using Microsoft.Extensions.Logging.Abstractions;

namespace DevToys.Base64Url.Tests;

public class Base64UrlHelperTests
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger = NullLogger.Instance;

    [Theory]
    [InlineData("Hello World", "SGVsbG8gV29ybGQ")]
    [InlineData("DevToys", "RGV2VG95cw")]
    [InlineData("Base64Url encode >> ?", "QmFzZTY0VXJsIGVuY29kZSA-PiA_")]
    public void FromTextToBase64Url_Utf8_ShouldEncodeCorrectly(string input, string expected)
    {
        var actual = Base64UrlHelper.FromTextToBase64Url(input, Base64Encoding.Utf8, _logger, CancellationToken.None);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromTextToBase64Url_WithSpecialChars_ShouldTranslateCorrectly()
    {
        const string input = "\xFB\xEF\xFF\xFA";
        var standardBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input));
        var expectedBase64Url = standardBase64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        
        var actual = Base64UrlHelper.FromTextToBase64Url(input, Base64Encoding.Utf8, _logger, CancellationToken.None);
        
        Assert.Equal(expectedBase64Url, actual);
        Assert.DoesNotContain("+", actual);
        Assert.DoesNotContain("/", actual);
        Assert.DoesNotContain("=", actual);
    }

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ", "Hello World")]
    [InlineData("RGV2VG95cw", "DevToys")]
    public void FromBase64UrlToText_Utf8_ShouldDecodeCorrectly(string input, string expected)
    {
        var actual = Base64UrlHelper.FromBase64UrlToText(input, Base64Encoding.Utf8, _logger, CancellationToken.None);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ=")]
    [InlineData("RGV2VG95cw==")]
    [InlineData("Hello+World/")]
    public void IsBase64UrlDataStrict_False(string input)
    {
        var actual = Base64UrlHelper.IsBase64UrlDataStrict(input);
        Assert.False(actual);
    }

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ")]
    [InlineData("RGV2VG95cw")]
    public void IsBase64UrlDataStrict_True(string input)
    {
        var actual = Base64UrlHelper.IsBase64UrlDataStrict(input);
        Assert.True(actual);
    }
}
