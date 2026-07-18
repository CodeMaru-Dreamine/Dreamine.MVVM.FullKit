using System.Globalization;
using System.Windows.Data;

namespace FamiliesAutoWriter.Converters;

/// <summary>
/// \if KO
/// <para>Enum To Bool Converter 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates enum to bool converter functionality and related state.</para>
/// \endif
/// </summary>
public sealed class EnumToBoolConverter : IValueConverter
{
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
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString() == parameter?.ToString();

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
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? Enum.Parse(targetType, parameter!.ToString()!) : Binding.DoNothing;
}
