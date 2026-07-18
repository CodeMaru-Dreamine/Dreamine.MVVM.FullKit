using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DreamineVMS.Views;

/// <summary>
/// \if KO
/// <para>Bool To Brush Converter 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates bool to brush converter functionality and related state.</para>
/// \endif
/// </summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    /// <summary>
    /// \if KO
    /// <para>Instance 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the instance value.</para>
    /// \endif
    /// </summary>
    public static readonly BoolToBrushConverter Instance = new();

    /// <summary>
    /// \if KO
    /// <para>Convert 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the convert operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <param name="targetType">
    /// \if KO
    /// <para>target Type에 사용할 <c>Type</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Type</c> value used for target type.</para>
    /// \endif
    /// </param>
    /// <param name="parameter">
    /// \if KO
    /// <para>parameter에 사용할 <c>object</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object</c> value used for parameter.</para>
    /// \endif
    /// </param>
    /// <param name="culture">
    /// \if KO
    /// <para>culture에 사용할 <c>CultureInfo</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CultureInfo</c> value used for culture.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Convert 작업에서 생성한 <c>object</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object</c> result produced by the convert operation.</para>
    /// \endif
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Brushes.ForestGreen : Brushes.Gray;

    /// <summary>
    /// \if KO
    /// <para>Convert Back 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the convert back operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <param name="targetType">
    /// \if KO
    /// <para>target Type에 사용할 <c>Type</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Type</c> value used for target type.</para>
    /// \endif
    /// </param>
    /// <param name="parameter">
    /// \if KO
    /// <para>parameter에 사용할 <c>object</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object</c> value used for parameter.</para>
    /// \endif
    /// </param>
    /// <param name="culture">
    /// \if KO
    /// <para>culture에 사용할 <c>CultureInfo</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CultureInfo</c> value used for culture.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Convert Back 작업에서 생성한 <c>object</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object</c> result produced by the convert back operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// \if KO
    /// <para>요청한 작업이 지원되지 않는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the requested operation is not supported.</para>
    /// \endif
    /// </exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// \if KO
/// <para>Bool To Status Text Converter 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates bool to status text converter functionality and related state.</para>
/// \endif
/// </summary>
public sealed class BoolToStatusTextConverter : IValueConverter
{
    /// <summary>
    /// \if KO
    /// <para>Instance 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the instance value.</para>
    /// \endif
    /// </summary>
    public static readonly BoolToStatusTextConverter Instance = new();

    /// <summary>
    /// \if KO
    /// <para>Convert 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the convert operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <param name="targetType">
    /// \if KO
    /// <para>target Type에 사용할 <c>Type</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Type</c> value used for target type.</para>
    /// \endif
    /// </param>
    /// <param name="parameter">
    /// \if KO
    /// <para>parameter에 사용할 <c>object</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object</c> value used for parameter.</para>
    /// \endif
    /// </param>
    /// <param name="culture">
    /// \if KO
    /// <para>culture에 사용할 <c>CultureInfo</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CultureInfo</c> value used for culture.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Convert 작업에서 생성한 <c>object</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object</c> result produced by the convert operation.</para>
    /// \endif
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "● 연결됨" : "○ 미연결";

    /// <summary>
    /// \if KO
    /// <para>Convert Back 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the convert back operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <param name="targetType">
    /// \if KO
    /// <para>target Type에 사용할 <c>Type</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Type</c> value used for target type.</para>
    /// \endif
    /// </param>
    /// <param name="parameter">
    /// \if KO
    /// <para>parameter에 사용할 <c>object</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object</c> value used for parameter.</para>
    /// \endif
    /// </param>
    /// <param name="culture">
    /// \if KO
    /// <para>culture에 사용할 <c>CultureInfo</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CultureInfo</c> value used for culture.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Convert Back 작업에서 생성한 <c>object</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object</c> result produced by the convert back operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// \if KO
    /// <para>요청한 작업이 지원되지 않는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the requested operation is not supported.</para>
    /// \endif
    /// </exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
