using System.ComponentModel.Composition;
using DevToys.Api;
using Microsoft.Extensions.Logging;
using static DevToys.Api.GUI;

namespace DevToys.Base64Url;

internal enum EncodingConversion
{
    Encode,
    Decode
}

[Export(typeof(IGuiTool))]
[Name("Base64UrlEncoderDecoder")]
[ToolDisplayInformation(
    IconFontName = "DevToys-Tools-Icons",
    IconGlyph = '\u0100',
    GroupName = PredefinedCommonToolGroupNames.EncodersDecoders,
    ResourceManagerAssemblyIdentifier = nameof(Base64UrlResourceAssemblyIdentifier),
    ResourceManagerBaseName = "DevToys.Base64Url.Base64Url",
    ShortDisplayTitleResourceName = nameof(Base64Url.ShortDisplayTitle),
    LongDisplayTitleResourceName = nameof(Base64Url.LongDisplayTitle),
    DescriptionResourceName = nameof(Base64Url.Description),
    AccessibleNameResourceName = nameof(Base64Url.AccessibleName),
    SearchKeywordsResourceName = nameof(Base64Url.SearchKeywords))]
[AcceptedDataTypeName(PredefinedDataTypeNames.Base64UrlText)]
[AcceptedDataTypeName(PredefinedCommonDataTypeNames.Text)]
internal sealed partial class Base64UrlEncoderDecoderGuiTool : IGuiTool, IDisposable
{
    private static readonly SettingDefinition<EncodingConversion> ConversionMode = new(
        name: $"{nameof(Base64UrlEncoderDecoderGuiTool)}.{nameof(ConversionMode)}",
        defaultValue: EncodingConversion.Encode);

    private static readonly SettingDefinition<Base64Encoding> Encoder = new(
        name: $"{nameof(Base64UrlEncoderDecoderGuiTool)}.{nameof(Encoder)}",
        defaultValue: DefaultEncoding);

    private static readonly SettingDefinition<bool> MultilineMode = new(
        name: $"{nameof(Base64UrlEncoderDecoderGuiTool)}.{nameof(MultilineMode)}",
        defaultValue: false);

    private enum GridRows
    {
        Settings,
        Input,
        Output
    }

    private enum GridColumns
    {
        Stretch
    }

    private const Base64Encoding DefaultEncoding = Base64Encoding.Utf8;

    private readonly DisposableSemaphore _semaphore = new();
    private readonly ILogger _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IUISwitch _conversionModeSwitch = Switch("base64url-text-conversion-mode-switch");
    private readonly IUISwitch _multilineModeSwitch = Switch("base64url-text-multiline-mode-switch");
    private readonly IUIMultiLineTextInput _inputText = MultiLineTextInput("base64url-text-input-box");
    private readonly IUIMultiLineTextInput _outputText = MultiLineTextInput("base64url-text-output-box");

    private CancellationTokenSource? _cancellationTokenSource;

    [ImportingConstructor]
    public Base64UrlEncoderDecoderGuiTool(ISettingsProvider settingsProvider)
    {
        _logger = this.Log();
        _settingsProvider = settingsProvider;

        if (_settingsProvider.GetSetting(MultilineMode))
        {
            _multilineModeSwitch.On();
        }
        else
        {
            _multilineModeSwitch.Off();
        }

        switch (_settingsProvider.GetSetting(ConversionMode))
        {
            case EncodingConversion.Encode:
                _conversionModeSwitch.On();
                _inputText.AutoWrap();
                _outputText.AlwaysWrap();
                break;

            case EncodingConversion.Decode:
                _conversionModeSwitch.Off();
                _inputText.AlwaysWrap();
                _outputText.AutoWrap();
                break;

            default:
                throw new NotSupportedException();
        }
    }

    internal Task? WorkTask { get; private set; }

    public UIToolView View
        => new(
            isScrollable: true,
            Grid()
                .RowLargeSpacing()

                .Rows(
                    (GridRows.Settings, Auto),
                    (GridRows.Input, new UIGridLength(1, UIGridUnitType.Fraction)),
                    (GridRows.Output, new UIGridLength(1, UIGridUnitType.Fraction)))

                .Columns(
                    (GridColumns.Stretch, new UIGridLength(1, UIGridUnitType.Fraction)))

                .Cells(
                    Cell(
                        GridRows.Settings,
                        GridColumns.Stretch,
                        Stack()
                            .Vertical()
                            .SmallSpacing()
                            .WithChildren(
                                Label()
                                    .Text(Base64Url.ConfigurationTitle),

                                SettingGroup("base64url-text-conversion-mode-setting")
                                    .Icon("FluentSystemIcons", '\uF18D')
                                    .Title(Base64Url.ConversionTitle)
                                    .Description(Base64Url.ConversionDescription)
                                    .InteractiveElement(
                                        _conversionModeSwitch
                                            .OnText(Base64Url.ConversionEncode)
                                            .OffText(Base64Url.ConversionDecode)
                                            .OnToggle(OnConversionModeChanged))
                                    .WithSettings(
                                        Setting("base64url-text-encoding-setting")
                                            .Title(Base64Url.EncodingTitle)
                                            .Description(Base64Url.EncodingDescription)
                                            .Handle(
                                                _settingsProvider,
                                                Encoder,
                                                onOptionSelected: OnEncodingModeChanged,
                                                Item(Base64Url.Utf8, Base64Encoding.Utf8),
                                                Item(Base64Url.Ascii, Base64Encoding.Ascii)
                                            ),

                                        Setting("base64url-text-multiline-setting")
                                                .Title(Base64Url.MultilineTitle)
                                                .Description(Base64Url.MultilineOptionDescription)
                                                .InteractiveElement(
                                                    _multilineModeSwitch
                                                        .OnToggle(OnMultilineModeChanged)
                                            )
                                    )
                            )
                        ),

                    Cell(
                        GridRows.Input,
                        GridColumns.Stretch,
                        _inputText
                            .Title(Base64Url.InputTitle)
                            .OnTextChanged(OnInputTextChanged)),

                    Cell(
                        GridRows.Output,
                        GridColumns.Stretch,
                        _outputText
                            .Title(Base64Url.OutputTitle)
                            .ReadOnly()
                            .Extendable())));

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        if (dataTypeName == PredefinedDataTypeNames.Base64UrlText && parsedData is string base64Text)
        {
            _conversionModeSwitch.Off();
            _inputText.Text(base64Text);
        }
        else if (dataTypeName == PredefinedCommonDataTypeNames.Text && parsedData is string text)
        {
            _conversionModeSwitch.On();
            _inputText.Text(text);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _semaphore.Dispose();
    }

    private void OnMultilineModeChanged(bool multilineModeIsOn)
    {
        _settingsProvider.SetSetting(Base64UrlEncoderDecoderGuiTool.MultilineMode, multilineModeIsOn);
        StartConvert(_inputText.Text);
    }

    private void OnConversionModeChanged(bool conversionModeIsEncode)
    {
        _settingsProvider.SetSetting(Base64UrlEncoderDecoderGuiTool.ConversionMode, conversionModeIsEncode ? EncodingConversion.Encode : EncodingConversion.Decode);
        _inputText.Text(_outputText.Text);

        switch (_settingsProvider.GetSetting(Base64UrlEncoderDecoderGuiTool.ConversionMode))
        {
            case EncodingConversion.Encode:
                _inputText.AutoWrap();
                _outputText.AlwaysWrap();
                break;

            case EncodingConversion.Decode:
                _inputText.AlwaysWrap();
                _outputText.AutoWrap();
                break;

            default:
                throw new NotSupportedException();
        }
    }

    private void OnEncodingModeChanged(Base64Encoding encodingMode)
    {
        StartConvert(_inputText.Text);
    }

    private void OnInputTextChanged(string text)
    {
        StartConvert(text);
    }

    private void StartConvert(string text)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        WorkTask = ConvertAsync(text, _settingsProvider.GetSetting(Encoder), _cancellationTokenSource.Token);
    }

    private async Task ConvertAsync(string input, Base64Encoding encoderSetting, CancellationToken cancellationToken)
    {
        using (await _semaphore.WaitAsync(cancellationToken))
        {
            await TaskSchedulerAwaiter.SwitchOffMainThreadAsync(cancellationToken);

            string conversionResult;

            if (!_settingsProvider.GetSetting(MultilineMode))
            {
                conversionResult = GetConversionString(input, encoderSetting, cancellationToken);
            }
            else
            {
                List<string> resultLines = [];
                var inputLines = input.Split([Environment.NewLine], StringSplitOptions.None);

                foreach(var line in inputLines)
                {
                    var tempResult = GetConversionString(line, encoderSetting, cancellationToken);
                    resultLines.Add(tempResult);
                }

                conversionResult = string.Join(Environment.NewLine, resultLines);
            }

            cancellationToken.ThrowIfCancellationRequested();
            _outputText.Text(conversionResult);
        }
    }

    private string GetConversionString(string input, Base64Encoding encoderSetting, CancellationToken cancellationToken)
    {
        switch (_settingsProvider.GetSetting(ConversionMode))
        {
            case EncodingConversion.Encode:
                return Base64UrlHelper.FromTextToBase64Url(
                        input,
                        encoderSetting,
                        _logger,
                        cancellationToken);

            case EncodingConversion.Decode:
                if (string.IsNullOrEmpty(input) || Base64UrlHelper.IsBase64UrlDataStrict(input))
                {
                    return Base64UrlHelper.FromBase64UrlToText(
                        input,
                        encoderSetting,
                        _logger,
                        cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                return Base64Url.InvalidBase64Url;

            default:
                throw new NotSupportedException();
        }
    }
}
