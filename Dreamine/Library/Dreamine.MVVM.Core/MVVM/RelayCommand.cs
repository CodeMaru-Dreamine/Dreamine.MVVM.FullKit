using System;
using System.Windows.Input;

namespace Dreamine.MVVM.Core
{
    /// <summary>
    /// 매개변수가 없는 기본 RelayCommand 구현입니다.
    /// ViewModel에서 ICommand 바인딩용으로 사용됩니다.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// RelayCommand 생성자
        /// </summary>
        /// <param name="execute">실행 메서드</param>
        /// <param name="canExecute">실행 가능 여부 판단 메서드 (선택)</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

		/// <summary>
		/// 현재 명령이 실행 가능한지를 나타냅니다.
		/// </summary>
		/// <param name="parameter">명령 실행에 사용될 파라미터 (사용하지 않음)</param>
		/// <returns>명령이 실행 가능한 경우 true, 그렇지 않으면 false</returns>
		public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

		/// <summary>
		/// 명령을 실행합니다.
		/// </summary>
		/// <param name="parameter">명령 실행에 사용될 파라미터 (사용하지 않음)</param>
		public void Execute(object parameter) => _execute();

		/// <summary>
		/// 명령의 실행 가능 상태가 변경될 때 발생하는 이벤트입니다.
		/// </summary>
		public event EventHandler CanExecuteChanged;


		/// <summary>
		/// CanExecute 상태를 수동으로 갱신합니다.
		/// </summary>
		public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 제네릭 RelayCommand 구현입니다. 매개변수를 사용하는 경우 사용됩니다.
    /// </summary>
    /// <typeparam name="T">매개변수 타입</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        /// <summary>
        /// RelayCommand 생성자
        /// </summary>
        /// <param name="execute">실행 메서드</param>
        /// <param name="canExecute">실행 가능 여부 판단 메서드 (선택)</param>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

		/// <summary>
		/// 명령이 현재 실행 가능한지를 결정합니다.
		/// </summary>
		/// <param name="parameter">명령 실행에 전달된 파라미터. <typeparamref name="T"/>로 캐스팅됩니다.</param>
		/// <returns>명령이 실행 가능하면 true, 그렇지 않으면 false</returns>
		public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

		/// <summary>
		/// 명령을 실행합니다.
		/// </summary>
		/// <param name="parameter">명령 실행에 전달된 파라미터. <typeparamref name="T"/>로 캐스팅됩니다.</param>
		public void Execute(object parameter) => _execute((T)parameter);

		/// <summary>
		/// 명령의 실행 가능 상태가 변경되었음을 알리는 이벤트입니다.
		/// UI 바인딩 요소는 이 이벤트를 통해 CanExecute 상태를 다시 평가합니다.
		/// </summary>
		public event EventHandler CanExecuteChanged;


		/// <summary>
		/// CanExecute 상태를 수동으로 갱신합니다.
		/// </summary>
		public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}