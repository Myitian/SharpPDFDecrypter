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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SharpPDFDecrypter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isRunning;
        private readonly LicenseWindow license = new LicenseWindow();
        internal static readonly Dictionary<IntPtr, TaskInfo> TaskMap = new Dictionary<IntPtr, TaskInfo>();
        internal static readonly PasswordWindow Password = new PasswordWindow();
        internal static readonly ManualResetEvent IsPasswordWindowClosed = new ManualResetEvent(true);

        public ObservableCollection<TaskInfo> Tasks { get; } = new ObservableCollection<TaskInfo>();
        public bool IsRunning
        {
            get => isRunning;
            private set
            {
                isRunning = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }
        public bool IsCanceled { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            AddFileIcon.Source = IconHelper.GetFileIcon(".pdf").ToImageSource();
            AddFolderIcon.Source = IconHelper.GetFolderIcon().ToImageSource();
            RemoveItemIcon.Source = IconHelper.ExtractIcon("%windir%\\system32\\shell32.dll", 131).ToImageSource();
            ClearListIcon.Source = IconHelper.ExtractIcon("%windir%\\system32\\shell32.dll", 31).ToImageSource();

            // 强制初始化 TaskInfoBrushes 中的
            _ = TaskInfoBrushes.UnprocessedBrush;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            int prevCount = Tasks.Count;
            string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            await Util.AddFiles(paths, Tasks, TaskMap, OnReportProgressCallback);
            ShowText($"添加了 {Tasks.Count - prevCount} 个文件");
        }

        private async void OpenFile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int prevCount = Tasks.Count;
            if (OpenFileHelper.ShowOpenFileDialog("PDF文件|*.pdf|全部文件|*.*", out string[] files))
            {
                await Util.AddFiles(files, Tasks, TaskMap, OnReportProgressCallback);
            }
            ShowText($"添加了 {Tasks.Count - prevCount} 个文件");
        }
        private void OpenFile_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void OpenFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int prevCount = Tasks.Count;
            if (OpenFileHelper.ShowOpenFolderDialog(out string folder))
            {
                await Util.AddFile(folder, Tasks, TaskMap, OnReportProgressCallback);
            }
            ShowText($"添加了 {Tasks.Count - prevCount} 个文件");
        }
        private void OpenFolder_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void RemoveItem_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int prevCount = Tasks.Count;
            if (!(TaskView?.SelectedItems is null))
            {
                IList selected = TaskView.SelectedItems;
                for (int i = selected.Count; i-- > 0;)
                {
                    TaskInfo info = selected[i] as TaskInfo;
                    TaskState state = info.State;
                    if (state != TaskState.WaitingForStart && state != TaskState.ReadingFile && state != TaskState.Running)
                    {
                        Tasks.Remove(info);
                        TaskMap.Remove(info.QPDFObject.Qpdf);
                        info.Dispose();
                    }
                }
            }
            ShowText($"移除了 {prevCount - Tasks.Count} 个文件");
        }
        private void RemoveItem_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsRunning;
        }

        private void ClearList_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int prevCount = Tasks.Count;
            foreach (TaskInfo info in Tasks)
            {
                info.Dispose();
            }
            Tasks.Clear();
            TaskMap.Clear();
            ShowText($"移除了 {prevCount - Tasks.Count} 个文件");
        }
        private void ClearList_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsRunning;
        }

        private async void RunDecryption_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowText("准备执行");
            IsRunning = true;
            IsCanceled = false;
            int total = 0;
            for (int i = 0; i < Tasks.Count; i++)
            {
                TaskInfo info = Tasks[i];
                TaskState state = info.State;
                if (state == TaskState.Unprocessed || state == TaskState.Canceled || state == TaskState.Error)
                {
                    info.State = TaskState.WaitingForStart;
                    total++;
                }
            }
            if (total > 0)
            {
                int taskcount = Math.Min(5, total);
                int freeTaskIndex = 0;
                int count = 0;
                int completedTaskCount = 0;
                Task[] tasks = new Task[taskcount];
                TaskInfo[] infox = new TaskInfo[taskcount];
                for (int i = 0; i < Tasks.Count; i++)
                {
                    TaskInfo info = Tasks[i];
                    if (IsCanceled)
                    {
                        for (int j = 0; j < Tasks.Count; j++)
                        {
                            TaskInfo info2 = Tasks[j];
                            if (info2.State == TaskState.WaitingForStart)
                            {
                                info2.State = TaskState.Canceled;
                            }
                        }
                        break;
                    }
                    if (info.State == TaskState.WaitingForStart)
                    {
                        infox[freeTaskIndex] = info;
                        tasks[freeTaskIndex] = Task.Run(() =>
                        {
                            info.Init();
                            info.State = TaskState.Running;
                            info.QPDFObject.PreserveEncryption = false;
                            info.QPDFObject.WriteToFile(info.DestinationFile);
                        })
                            .ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                {
                                    info.State = TaskState.Error;
                                    info.Error = (task.Exception.InnerException ?? task.Exception).Message;
                                }
                                else
                                {
                                    info.State = TaskState.Done;
                                }
                                completedTaskCount++;
                                info.Dispose();
                            });
                        ShowText($"开始执行：{++count}/{taskcount}");
                        if (count >= taskcount)
                        {
                            freeTaskIndex = Array.IndexOf(tasks, await Task.WhenAny(tasks));
                        }
                        else
                        {
                            freeTaskIndex++;
                        }
                    }
                }
                await Task.WhenAll(tasks);
            }
            IsRunning = false;
            ShowText("执行完成");
        }
        private void RunDecryption_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsRunning;
        }

        private void StopDecryption_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowText("正在中止");
            IsCanceled = true;
        }
        private void StopDecryption_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsRunning;
        }

        private void License_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            license.Show();
            license.Activate();
        }
        private void License_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ProjectInfo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = assembly.GetName();
            AssemblyCompanyAttribute company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
            string projectinfo = string.Format(Properties.Resources.ProjectInfo,
                                               assemblyName.Name,
                                               assemblyName.Version,
                                               company?.Company,
                                               App.AuthorPageAddress,
                                               App.ProjectAddress,
                                               App.License);
            if (MessageBox.Show(projectinfo, "项目信息", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                Process.Start(App.ProjectAddress);
            }
        }
        private void ProjectInfo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            license.Close();
            Password.Close(true);
            IsPasswordWindowClosed.Dispose();
        }

        private void OnReportProgressCallback(int percent, IntPtr qpdf)
        {
            TaskInfo info = TaskMap[qpdf];
            info.Progress = percent;
        }

        private void ShowText(string text)
        {
            State.Content = text;
        }
    }
}
