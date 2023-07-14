using IconHelper;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SharpPDFDecrypter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static RoutedCommand OpenFileCmd = new RoutedCommand();
        public static RoutedCommand OpenFolderCmd = new RoutedCommand();
        public static RoutedCommand RemoveItemCmd = new RoutedCommand();
        public static RoutedCommand ClearListCmd = new RoutedCommand();
        public static RoutedCommand RunDecryptionCmd = new RoutedCommand();


        public ObservableCollection<TaskInfo> Tasks { get; } = new ObservableCollection<TaskInfo>();

        public MainWindow()
        {
            InitializeComponent();
            AddFileIcon.Source = IconReader.GetFileIcon(".pdf", IconSize.Large, false).ToImageSource();
            AddFolderIcon.Source = IconReader.GetFolderIcon(IconSize.Large).ToImageSource();
            RemoveItemIcon.Source = IconReader.ExtractIcon("%windir%\\system32\\shell32.dll", 131).ToImageSource();
            ClearListIcon.Source = IconReader.ExtractIcon("%windir%\\system32\\shell32.dll", 31).ToImageSource();
            RunDecryptionIcon.Source = IconReader.ExtractIcon("%windir%\\system32\\shell32.dll", 137).ToImageSource();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            foreach (string path in paths)
            {
                Util.AddFile(path, Tasks);
            }
        }

        private void OpenFile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("A");
        }

        private void OpenFile_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("B");
        }

        private void OpenFolder_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
    }
}
