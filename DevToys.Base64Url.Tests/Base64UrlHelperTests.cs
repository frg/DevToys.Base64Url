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
    [InlineData("SGVs bG9y")]   // embedded space is not a valid Base64Url character
    [InlineData("SGVs\nbG9y")]  // embedded newline is not a valid Base64Url character
    public void IsBase64UrlDataStrict_False(string input)
    {
        var actual = Base64UrlHelper.IsBase64UrlDataStrict(input);
        Assert.False(actual);
    }

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ")]   // length % 4 == 3
    [InlineData("RGV2VG95cw")]        // length % 4 == 2
    [InlineData("AAAA")]              // length % 4 == 0, no padding needed
    [InlineData("AAA")]               // length % 4 == 3
    public void IsBase64UrlDataStrict_True(string input)
    {
        var actual = Base64UrlHelper.IsBase64UrlDataStrict(input);
        Assert.True(actual);
    }

    [Theory]
    [InlineData("A")]   // length % 4 == 1 is never valid in any Base64 encoding
    [InlineData("AAAAA")] // length % 4 == 1
    public void IsBase64UrlDataStrict_LengthMod4IsOne_ReturnsFalse(string input)
    {
        var actual = Base64UrlHelper.IsBase64UrlDataStrict(input);
        Assert.False(actual);
    }

    [Theory]
    [InlineData("Hello", "SGVsbG8")]
    [InlineData("DevToys", "RGV2VG95cw")]
    public void FromTextToBase64Url_Ascii_ShouldEncodeCorrectly(string input, string expected)
    {
        var actual = Base64UrlHelper.FromTextToBase64Url(input, Base64Encoding.Ascii, _logger, CancellationToken.None);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromTextToBase64Url_Ascii_NonAsciiChars_AreReplacedWithQuestionMark()
    {
        // Encoding.ASCII silently replaces non ASCII characters with '?' (0x3F)
        const string input = "caf\u00e9"; // 'é' is outside ASCII range
        var expected = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(input))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var actual = Base64UrlHelper.FromTextToBase64Url(input, Base64Encoding.Ascii, _logger, CancellationToken.None);

        Assert.Equal(expected, actual); // 'é' → '?' in output
    }

    [Theory]
    [InlineData("SGVsbG8", "Hello")]
    [InlineData("w6k", "??")] // bytes [0xC3, 0xA9] are UTF8 for 'é'. ASCII replaces them with '?'
    public void FromBase64UrlToText_Ascii_ShouldDecodeCorrectly(string input, string expected)
    {
        var actual = Base64UrlHelper.FromBase64UrlToText(input, Base64Encoding.Ascii, _logger, CancellationToken.None);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("!!!!invalid")]
    [InlineData("not-base64!!!")]
    public void FromBase64UrlToText_InvalidInput_ReturnsInvalidBase64UrlSentinel(string input)
    {
        var actual = Base64UrlHelper.FromBase64UrlToText(input, Base64Encoding.Utf8, _logger, CancellationToken.None);
        Assert.Equal(Base64Url.InvalidBase64Url, actual);
    }
}
