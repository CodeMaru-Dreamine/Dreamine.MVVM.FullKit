using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// \if KO
    /// <para>PopupNotice.xaml에 대한 상호 작용 논리</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates popup notice functionality and related state.</para>
    /// \endif
    /// </summary>
    public partial class PopupNotice : Window
    {
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="PopupNotice"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="PopupNotice"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        public PopupNotice()
        {
            InitializeComponent();
        }
    }
}
