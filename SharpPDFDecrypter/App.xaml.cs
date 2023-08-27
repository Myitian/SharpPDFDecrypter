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
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace SharpPDFDecrypter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly string AuthorPageAddress = "https://github.com/Myitian";
        public static readonly string ProjectAddress = SharpPDFDecrypter.Properties.Resources.ProjectAddress;
        public static readonly string License = SharpPDFDecrypter.Properties.Resources.LicenseIdentifier;

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!Enum.TryParse<UnmanagedType>("LPUTF8Str", out _))
            {
                MessageBox.Show("请安装 .NET Framework 4.7 或更高版本！");
                Environment.Exit(-1);
            }
            try
            {
                const string qpdfDll = "qpdf.dll";
                const string wrapperDll = "wrapper.dll";
                if (Environment.Is64BitProcess)
                {
                    File.WriteAllBytes(qpdfDll, SharpPDFDecrypter.Properties.Resources.QPDF_64);
                    File.WriteAllBytes(wrapperDll, SharpPDFDecrypter.Properties.Resources.Wrapper_64);
                }
                else
                {
                    File.WriteAllBytes(qpdfDll, SharpPDFDecrypter.Properties.Resources.QPDF_32);
                    File.WriteAllBytes(wrapperDll, SharpPDFDecrypter.Properties.Resources.Wrapper_32);
                }
                File.Open(qpdfDll, FileMode.Open, FileAccess.Read, FileShare.Read).Close();
                File.Open(wrapperDll, FileMode.Open, FileAccess.Read, FileShare.Read).Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            base.OnStartup(e);
        }
    }
}
