using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dreamine.MVVM.Core
{
    /// <summary>
    /// Dreamine MVVM의 모든 ViewModel이 상속하는 기본 클래스입니다.
    /// INotifyPropertyChanged를 구현하며, 속성 변경 알림 기능을 제공합니다.
    /// </summary>
    public abstract partial class ViewModelBase : INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 지정된 속성 이름으로 PropertyChanged 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 백킹 필드를 설정하고 값이 변경되었을 경우 PropertyChanged를 발생시킵니다.
        /// </summary>
        /// <typeparam name="T">속성 타입</typeparam>
        /// <param name="field">백킹 필드</param>
        /// <param name="value">새로운 값</param>
        /// <param name="propertyName">속성 이름</param>
        /// <returns>값이 변경되었는지 여부</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// RelayCommand의 CanExecuteChanged를 수동으로 트리거할 수 있도록 도와주는 확장 포인트입니다.
        /// Source Generator가 필요 시 여기를 호출하도록 생성합니다.
        /// </summary>
        /// <param name="commandName">커맨드 이름</param>
        protected virtual void OnCommandChanged(string commandName)
        {
            // 필요 시 커맨드 이름 기반으로 RaiseCanExecuteChanged 구현 가능
        }
    }
}
