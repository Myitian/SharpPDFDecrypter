using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace SharpPDFDecrypter
{
    /// <summary>
    /// EnterPasswordWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PasswordWindow : Window, INotifyPropertyChanged
    {
        private bool isRetrying;
        private bool isCancaled;
        private bool isForceClosing;

        public event PropertyChangedEventHandler PropertyChanged;

        public string FileName { get; set; }
        public string Info => isRetrying ?
            $"口令错误。\r\n文件 \"{FileName}\" 包含打开口令，请于下方重新输入打开口令（点击取消来跳过该文件）" :
            $"文件 \"{FileName}\" 包含打开口令，请于下方输入打开口令（点击取消来跳过该文件）";

        public PasswordWindow()
        {
            InitializeComponent();
        }

        public void Init()
        {
            isCancaled = true;
            isRetrying = false;
            OnPropertyChanged(nameof(Info));
            Password.Password = string.Empty;
        }
        public void InitRetry()
        {
            isCancaled = true;
            isRetrying = true;
            OnPropertyChanged(nameof(Info));
            Password.Password = string.Empty;
        }

        public void Close(bool force)
        {
            isForceClosing = force;
            Close();
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal string GetPassword()
        {
            return isCancaled ? null : Password.Password;
        }

        private void Confirm_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            isCancaled = false;
            Close(false);
        }
        private void Confirm_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Cancel_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            isCancaled = true;
            Close(false);
        }
        private void Cancel_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !isForceClosing;
            Visibility = Visibility.Hidden;
        }
    }
}
