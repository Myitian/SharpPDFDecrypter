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

using Microsoft.Win32;

namespace SharpPDFDecrypter.Utils
{
    public static class OpenFileHelper
    {
        public static bool ShowOpenFileDialog(string filter, out string[] files)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = filter,
                CheckFileExists = true,
                DereferenceLinks = true,
                Multiselect = true
            };
            if (ofd.ShowDialog() == true)
            {
                files = ofd.FileNames;
                return true;
            }
            else
            {
                files = null;
                return false;
            }
        }
        public static bool ShowOpenFolderDialog(out string folder)
        {
            OpenFolderDialog ofd = new OpenFolderDialog();
            if (ofd.ShowDialog())
            {
                folder = ofd.Folder;
                return true;
            }
            else
            {
                folder = null;
                return false;
            }
        }
    }
}
