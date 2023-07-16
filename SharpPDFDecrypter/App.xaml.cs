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
            if (Environment.Is64BitProcess)
            {
                File.WriteAllBytes("qpdf.dll", SharpPDFDecrypter.Properties.Resources.QPDF_64);
                File.WriteAllBytes("wrapper.dll", SharpPDFDecrypter.Properties.Resources.Wrapper_64);
            }
            else
            {
                File.WriteAllBytes("qpdf.dll", SharpPDFDecrypter.Properties.Resources.QPDF_32);
                File.WriteAllBytes("wrapper.dll", SharpPDFDecrypter.Properties.Resources.Wrapper_32);
            }

            base.OnStartup(e);
        }
    }
}
