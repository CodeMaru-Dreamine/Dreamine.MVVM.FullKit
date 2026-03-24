using SampleEnterprise.Interfaces;
using System.Windows;
using Dreamine.MVVM.Core;

namespace SampleEnterprise.Events
{
    public class MainWindowEvent
    {
        private readonly IViewManager _viewManager;
        public MainWindowEvent()
        {            
            _viewManager = DMContainer.Resolve<IViewManager>();
        }

        public static void Ok() => MessageBox.Show("확인 클릭됨!");
        public static void Cancel() => MessageBox.Show("취소 클릭됨!");

        public void SubPage()
        {
            var vmType = Type.GetType("SampleEnterprise.ViewModels.PageSub.PageSubViewModel, SampleEnterprise.ViewModels");
            if (vmType != null)
                _viewManager.Show(vmType);
        }


        public static void Minimize()
        {
            var w = GetActiveWindow();
            if (w != null) w.WindowState = WindowState.Minimized;
        }

        public static void Maximize()
        {
            var w = GetActiveWindow();
            if (w != null)
            {
                w.WindowState = w.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
        }

        public static void Close()
        {
            GetActiveWindow()?.Close();
        }

        private static Window? GetActiveWindow()
        {
            return Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive);
        }
    }
}
