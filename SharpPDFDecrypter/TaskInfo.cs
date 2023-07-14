using System.ComponentModel;
using System.Windows.Media;

namespace SharpPDFDecrypter
{
    public class TaskInfo : INotifyPropertyChanged
    {
        private static readonly Brush pendingBrush = new SolidColorBrush(Colors.SlateGray);
        private static readonly Brush runningBrush = new SolidColorBrush(Colors.DarkBlue);
        private static readonly Brush doneBrush = new SolidColorBrush(Colors.Green);
        private static readonly Brush errorBrush = new SolidColorBrush(Colors.Red);




        private string sourceFile;
        private string destinationFile;
        private int progress;
        private TaskState state;
        private string error;
        private string stateString;
        private Brush stateBrush;

        public event PropertyChangedEventHandler PropertyChanged;

        public string SourceFile
        {
            get => sourceFile;
            set
            {
                sourceFile = value;
                OnPropertyChanged(nameof(SourceFile));
            }
        }
        public string DestinationFile
        {
            get => destinationFile;
            set
            {
                destinationFile = value;
                OnPropertyChanged(nameof(DestinationFile));
            }
        }
        public int Progress
        {
            get => progress;
            set
            {
                progress = value;
                OnPropertyChanged(nameof(Progress));
                UpdateStateInfo();
            }
        }

        public TaskState State
        {
            get => state;
            set
            {
                state = value;
                UpdateStateInfo();
            }
        }

        public string Error
        {
            get => error;
            set
            {
                error = value;
                UpdateStateInfo();
            }
        }

        public string StateString { get => stateString; }
        public Brush StateBrush { get => stateBrush; }

        public TaskInfo()
        {
            UpdateStateInfo();
        }
        public TaskInfo(string src, string dest, int prog = 0, TaskState st = TaskState.Pending)
        {
            sourceFile = src;
            destinationFile = dest;
            progress = prog;
            state = st;
            UpdateStateInfo();
        }

        public void UpdateStateInfo()
        {
            switch (state)
            {
                case TaskState.Pending:
                    stateString = "待处理";
                    stateBrush = pendingBrush;
                    break;
                case TaskState.Running:
                    stateString = "处理中：" + progress + "%";
                    stateBrush = runningBrush;
                    break;
                case TaskState.Done:
                    stateString = "已完成";
                    stateBrush = doneBrush;
                    break;
                default:
                    stateString = "错误：" + Error;
                    stateBrush = errorBrush;
                    break;
            }
            OnPropertyChanged(nameof(StateString));
            OnPropertyChanged(nameof(StateBrush));
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool Equals(object obj)
        {
            return obj is TaskInfo ti && ti.sourceFile == sourceFile && ti.destinationFile == destinationFile;
        }

        public override int GetHashCode()
        {
            return sourceFile.GetHashCode() ^ destinationFile.GetHashCode();
        }
    }

    public enum TaskState
    {
        Pending,
        Running,
        Done,
        Error
    }
}
