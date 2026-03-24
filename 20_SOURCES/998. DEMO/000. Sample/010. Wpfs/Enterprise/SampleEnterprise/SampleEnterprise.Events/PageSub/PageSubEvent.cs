using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SampleEnterprise.Events.PageSub
{
    public class PageSubEvent
    {
        public static void Ok() => MessageBox.Show("확인 클릭됨!");
        public static void Cancel() => MessageBox.Show("취소 클릭됨!");
    }
}
