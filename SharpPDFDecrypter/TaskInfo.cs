/*
 * Copyright 2023 Myitian
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using SharpPDFDecrypter.Utils;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace SharpPDFDecrypter
{
    public class TaskInfoBrushes
    {
        internal static readonly Brush UnprocessedBrush = new SolidColorBrush(Colors.SlateGray);
        internal static readonly Brush WaitingBrush = new SolidColorBrush(Colors.MediumBlue);
        internal static readonly Brush RunningBrush = new SolidColorBrush(Colors.DarkBlue);
        internal static readonly Brush DoneBrush = new SolidColorBrush(Colors.Green);
        internal static readonly Brush CanceledBrush = new SolidColorBrush(Colors.Gold);
        internal static readonly Brush ErrorBrush = new SolidColorBrush(Colors.Red);
    }

    public class TaskInfo : INotifyPropertyChanged, IDisposable
    {
        private delegate void EnterPasswordDelegate(QPDFException e);

        private string sourceFile;
        private string destinationFile;
        private int progress;
        private TaskState state;
        private string error;
        private string stateString;
        private Brush stateBrush;
        private QPDF qpdfObject;
        private ReportProgressCallbackDelegate reportProgressCallback;

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
        public QPDF QPDFObject { get => qpdfObject; }
        public ReportProgressCallbackDelegate ReportProgressCallback
        {
            get => reportProgressCallback;
            set
            {
                ApplyDelegate(value);
                reportProgressCallback = value;
            }
        }

        public TaskInfo(string src, string dest, int prog = 0, TaskState st = TaskState.Unprocessed)
        {
            qpdfObject = new QPDF();
            sourceFile = src;
            destinationFile = dest;
            progress = prog;
            state = st;
            UpdateStateInfo();
        }

        public void ApplyDelegate(ReportProgressCallbackDelegate newValue)
        {
            if (!(qpdfObject is null))
            {
                if (!(reportProgressCallback is null))
                {
                    qpdfObject.ReportProgressCallback -= reportProgressCallback;
                }
                if (!(newValue is null))
                {
                    qpdfObject.ReportProgressCallback += newValue;
                }
            }
        }

        public void Init()
        {
            bool isRetrying = false;
            string password = null;
            while (true)
            {
                try
                {
                    if (error != null)
                    {
                        qpdfObject.Dispose();
                        MainWindow.TaskMap.ChangeKey(qpdfObject.Qpdf, (qpdfObject = new QPDF()).Qpdf);
                        ApplyDelegate(reportProgressCallback);
                    }
                    state = TaskState.ReadingFile;
                    qpdfObject.ReadFile(sourceFile, password);
                    break;
                }
                catch (QPDFException e) when (e.ErrorCode == QPDF_ERROR_CODE_E.QPDF_E_PASSWORD)
                {
                    state = TaskState.NeedPassword;
                    password = EnterPassword(isRetrying);
                    if (password == null)
                    {
                        throw e;
                    }
                    isRetrying = true;
                    error = null;
                    qpdfObject.Dispose();
                    MainWindow.TaskMap.ChangeKey(qpdfObject.Qpdf, (qpdfObject = new QPDF()).Qpdf);
                    ApplyDelegate(reportProgressCallback);
                }
            }
        }

        protected string EnterPassword(bool isRetrying)
        {
            MainWindow.IsPasswordWindowClosed.WaitOne();
            MainWindow.IsPasswordWindowClosed.Reset();
            Application.Current.Dispatcher.Invoke(() => MainWindow.Password.FileName = sourceFile);
            if (isRetrying)
            {
                Application.Current.Dispatcher.Invoke(MainWindow.Password.InitRetry);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(MainWindow.Password.Init);
            }
            Application.Current.Dispatcher.Invoke(MainWindow.Password.ShowDialog);
            string password = Application.Current.Dispatcher.Invoke(MainWindow.Password.GetPassword);
            MainWindow.IsPasswordWindowClosed.Set();
            return password;
        }

        public void UpdateStateInfo()
        {
            switch (state)
            {
                case TaskState.Unprocessed:
                    stateString = "未处理";
                    stateBrush = TaskInfoBrushes.UnprocessedBrush;
                    break;
                case TaskState.WaitingForStart:
                    stateString = "待处理";
                    stateBrush = TaskInfoBrushes.WaitingBrush;
                    break;
                case TaskState.ReadingFile:
                    stateString = "读取文件中";
                    stateBrush = TaskInfoBrushes.RunningBrush;
                    break;
                case TaskState.Running:
                    stateString = "处理中：" + progress + "%";
                    stateBrush = TaskInfoBrushes.RunningBrush;
                    break;
                case TaskState.Done:
                    stateString = "已完成";
                    stateBrush = TaskInfoBrushes.DoneBrush;
                    break;
                case TaskState.NeedPassword:
                    stateString = "需要打开口令";
                    stateBrush = TaskInfoBrushes.ErrorBrush;
                    break;
                case TaskState.Canceled:
                    stateString = "被取消";
                    stateBrush = TaskInfoBrushes.CanceledBrush;
                    break;
                default:
                    stateString = "错误：" + Error;
                    stateBrush = TaskInfoBrushes.ErrorBrush;
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

        public void Dispose()
        {
            qpdfObject.Dispose();
        }
    }

    public enum TaskState
    {
        Unprocessed,
        WaitingForStart,
        ReadingFile,
        Running,
        Done,
        NeedPassword,
        Canceled,
        Error,
    }
}
