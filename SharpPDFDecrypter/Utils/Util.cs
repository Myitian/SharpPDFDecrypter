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

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace SharpPDFDecrypter.Utils
{
    public static class Util
    {
        public static async Task AddFile(string path,
                                         ICollection<TaskInfo> tasks,
                                         Dictionary<IntPtr, TaskInfo> taskMap,
                                         ReportProgressCallbackDelegate reportProgressCallback)
        {
            try
            {
                path = Path.GetFullPath(path);
                if (Directory.Exists(path))
                {
                    foreach (string file in Directory.EnumerateFiles(path, "*.pdf", SearchOption.AllDirectories))
                    {
                        await AddFile(file, tasks, taskMap, reportProgressCallback);
                    }
                }
                else if (File.Exists(path))
                {
                    TaskInfo info = null;
                    await Task.Run(
                        () => info = new TaskInfo(path, GetDecryptedFileName(path)) { ReportProgressCallback = reportProgressCallback });
                    if (tasks.AddIfNotExist(info))
                    {
                        taskMap[info.QPDFObject.Qpdf] = info;
                    }
                    else
                    {
                        info.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        public static async Task AddFiles(string[] paths,
                                          ICollection<TaskInfo> tasks,
                                          Dictionary<IntPtr, TaskInfo> taskMap,
                                          ReportProgressCallbackDelegate reportProgressCallback)
        {
            foreach (string path in paths)
            {
                await AddFile(path, tasks, taskMap, reportProgressCallback);
            }
        }

        public static string GetDecryptedFileName(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_DECRYPTED.pdf");
        }

        public static bool AddIfNotExist<T>(this ICollection<T> collection, T item)
        {
            bool c = !collection.Contains(item);
            if (c)
            {
                collection.Add(item);
            }
            return c;
        }
    }
}
