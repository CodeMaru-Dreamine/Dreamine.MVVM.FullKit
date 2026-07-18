using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SampleEnterprise.Views
{
    /// <summary>
    /// \if KO
    /// <para>Main Window 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Interaction logic for MainWindow.xaml</para>
    /// \endif
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="MainWindow"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="MainWindow"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}