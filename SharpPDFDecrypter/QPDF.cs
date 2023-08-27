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
using System.Runtime.InteropServices;
using System.Text;

namespace SharpPDFDecrypter
{
    public class QPDF : IDisposable
    {
        /*
         * Initialize
         */
        [DllImport("qpdf.dll", EntryPoint = "qpdf_init", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Init();
        /*
         * Read
         */
        [DllImport("qpdf.dll", EntryPoint = "qpdf_read", CallingConvention = CallingConvention.Cdecl)]
        public static extern QPDF_ERROR_CODE Read(IntPtr qpdf,
                                                  [MarshalAs(UnmanagedType.LPUTF8Str)] string filename,
                                                  [MarshalAs(UnmanagedType.LPUTF8Str)] string password);
        [DllImport("qpdf.dll", EntryPoint = "qpdf_read_memory", CallingConvention = CallingConvention.Cdecl)]
        public static extern QPDF_ERROR_CODE ReadMemory(IntPtr qpdf,
                                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string description,
                                                        byte[] buffer,
                                                        long size,
                                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string password);
        [DllImport("qpdf.dll", EntryPoint = "qpdf_empty_pdf", CallingConvention = CallingConvention.Cdecl)]
        public static extern QPDF_ERROR_CODE EmptyPdf(IntPtr qpdf);
        /*
         * Write
         */
        [DllImport("qpdf.dll", EntryPoint = "qpdf_init_write", CallingConvention = CallingConvention.Cdecl)]
        public static extern QPDF_ERROR_CODE InitWrite(IntPtr qpdf, [MarshalAs(UnmanagedType.LPUTF8Str)] string filename);
        [DllImport("qpdf.dll", EntryPoint = "qpdf_init_write_memory", CallingConvention = CallingConvention.Cdecl)]
        public static extern QPDF_ERROR_CODE InitWriteMemory(IntPtr qpdf);
        [DllImport("qpdf.dll", EntryPoint = "qpdf_write", CallingConvention = CallingConvention.Cdecl)]
        public static extern QPDF_ERROR_CODE Write(IntPtr qpdf);
        [DllImport("qpdf.dll", EntryPoint = "qpdf_get_buffer_length", CallingConvention = CallingConvention.Cdecl)]
        public static extern UIntPtr GetBufferLength(IntPtr qpdf);
        [DllImport("qpdf.dll", EntryPoint = "qpdf_get_buffer", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetBuffer(IntPtr qpdf);
        /*
         * Cleanup
         */
        [DllImport("qpdf.dll", EntryPoint = "qpdf_cleanup", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Cleanup(IntPtr qpdf);
        /*
         * Error
         */
        [DllImport("qpdf.dll", EntryPoint = "qpdf_has_error", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool HasError(IntPtr qpdf);
        [DllImport("qpdf.dll", EntryPoint = "qpdf_get_error", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetError(IntPtr qpdf);
        /*
         * Other
         */
        [DllImport("qpdf.dll", EntryPoint = "qpdf_register_progress_reporter", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterProgressReporter(IntPtr qpdf, ReportProgressCallbackDelegate report_progress, IntPtr data);
        [DllImport("qpdf.dll", EntryPoint = "qpdf_set_preserve_encryption", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetPreserveEncryption(IntPtr qpdf, bool value);
        [DllImport("qpdf.dll", EntryPoint = "qpdf_is_encrypted", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsEncrypted(IntPtr qpdf);


        [DllImport("wrapper.dll", EntryPoint = "wrapper_qpdfexc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WrapperQPDFExc(IntPtr qpdf_error);
        [DllImport("wrapper.dll", EntryPoint = "free_qpdfexc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeQPDFExc(IntPtr qpdfexc);

        private readonly ReportProgressCallbackDelegate _callbackReference;
        public event ReportProgressCallbackDelegate ReportProgressCallback;

        public readonly IntPtr Qpdf;
        private readonly GCHandle gcHandleOfQpdfPtr;
        private bool disposed;

        public QPDF_ERROR_CODE ErrorCode { get; private set; }

        public bool PreserveEncryption { get; set; } = true;

        public QPDF()
        {
            _callbackReference = OnReportProgressCallback;
            Qpdf = Init();
            gcHandleOfQpdfPtr = GCHandle.Alloc(Qpdf, GCHandleType.Pinned);
        }

        public void ReadFile(string filename, string password)
        {
            ErrorCode = Read(Qpdf, filename, password);
            CheckError();
        }

        public void ReadMemory(string description, byte[] buffer, string password)
        {
            ErrorCode = ReadMemory(Qpdf, description, buffer, buffer.LongLength, password);
            CheckError();
        }

        public void WriteToFile(string filename)
        {
            ErrorCode = InitWrite(Qpdf, filename);
            CheckError();
            PrepareWrite();
            RegisterProgressReporter(Qpdf, _callbackReference, Qpdf);
            ErrorCode = Write(Qpdf);
            CheckError();
        }

        public byte[] WriteToMemory()
        {
            ErrorCode = InitWriteMemory(Qpdf);
            CheckError();
            PrepareWrite();
            RegisterProgressReporter(Qpdf, _callbackReference, Qpdf);
            ErrorCode = Write(Qpdf);
            CheckError();
            ulong size = GetBufferLength(Qpdf).ToUInt64();
            if (size > int.MaxValue)
            {
                throw new NotSupportedException("Buffer size should not be larger than 2GB");
            }
            IntPtr buffer = GetBuffer(Qpdf);
            byte[] result = new byte[size];
            Marshal.Copy(buffer, result, 0, (int)size);
            return result;
        }

        protected void PrepareWrite()
        {
            SetPreserveEncryption(Qpdf, PreserveEncryption);
        }
        protected void CheckError()
        {
            if (HasError(Qpdf))
            {
                IntPtr p = WrapperQPDFExc(GetError(Qpdf));
                QPDFExc error = new QPDFExc();
                Marshal.PtrToStructure(p, error);
                FreeQPDFExc(p);
                throw new QPDFException(error);
            }
        }
        protected void OnReportProgressCallback(int percent, IntPtr data)
        {
            ReportProgressCallback?.Invoke(percent, data);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Cleanup(gcHandleOfQpdfPtr.AddrOfPinnedObject());
                gcHandleOfQpdfPtr.Free();
                disposed = true;
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReportProgressCallbackDelegate(int percent, IntPtr data);

    public enum QPDF_ERROR_CODE
    {
        QPDF_SUCCESS = 0,
        QPDF_WARNINGS = 1 << 0,
        QPDF_ERRORS = 1 << 1
    }

    public enum QPDF_ERROR_CODE_E
    {
        /// <summary>success</summary>
        QPDF_E_SUCCESS,
        /// <summary>logic/programming error -- indicates bug</summary>
        QPDF_E_INTERNAL,
        /// <summary>I/O error, memory error, etc.g</summary>
        QPDF_E_SYSTEM,
        /// <summary>PDF feature not (yet) supported by qpdf</summary>
        QPDF_E_UNSUPPORTED,
        /// <summary>incorrect password for encrypted file</summary>
        QPDF_E_PASSWORD,
        /// <summary>syntax errors or other damage in PDF</summary>
        QPDF_E_DAMAGED_PDF,
        /// <summary>erroneous or unsupported pages structure</summary>
        QPDF_E_PAGES,
        /// <summary>type/bounds errors accessing objects</summary>
        QPDF_E_OBJECT,
        /// <summary>error in qpdf JSON</summary>
        QPDF_E_JSON,
        /// <summary>linearization warning</summary>
        QPDF_E_LINEARIZATION,
    };

    public static class QPDF_ERROR_CODE_E_Extension
    {
        public static string GetString(this QPDF_ERROR_CODE_E code)
        {
            switch (code)
            {
                case QPDF_ERROR_CODE_E.QPDF_E_SUCCESS:
                    return "成功";
                case QPDF_ERROR_CODE_E.QPDF_E_INTERNAL:
                    return "逻辑/编程错误——表示 bug";
                case QPDF_ERROR_CODE_E.QPDF_E_SYSTEM:
                    return "I/O错误、内存错误等";
                case QPDF_ERROR_CODE_E.QPDF_E_UNSUPPORTED:
                    return "qpdf 尚不支持的 PDF 功能";
                case QPDF_ERROR_CODE_E.QPDF_E_PASSWORD:
                    return "加密文件口令错误";
                case QPDF_ERROR_CODE_E.QPDF_E_DAMAGED_PDF:
                    return "PDF 中的语法错误或其他损坏";
                case QPDF_ERROR_CODE_E.QPDF_E_PAGES:
                    return "错误或不支持的页面结构";
                case QPDF_ERROR_CODE_E.QPDF_E_OBJECT:
                    return "访问对象的类型/边界错误";
                case QPDF_ERROR_CODE_E.QPDF_E_JSON:
                    return "qpdf JSON 错误";
                case QPDF_ERROR_CODE_E.QPDF_E_LINEARIZATION:
                    return "线性化警告";
                default:
                    return "[未知代码]";
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class QPDFExc
    {
        public QPDF_ERROR_CODE_E ErrorCode;
        public IntPtr Filename;
        public IntPtr Object;
        public long Offset;
        public IntPtr Message;
    }

    public class QPDFException : Exception
    {
        public QPDF_ERROR_CODE_E ErrorCode { get; private set; }
        public string Filename { get; private set; }
        public string Object { get; private set; }
        public long Offset { get; private set; }

        public QPDFException(QPDFExc error) : base($"（{error.ErrorCode.GetString()}）{MarshalEx.PtrToStringUTF8(error.Message)}")
        {
            ErrorCode = error.ErrorCode;
            Filename = MarshalEx.PtrToStringUTF8(error.Filename);
            Object = MarshalEx.PtrToStringUTF8(error.Object);
            Offset = error.Offset;
        }
        public QPDFException(QPDF_ERROR_CODE_E errorCode) : base($"（{errorCode.GetString()}）")
        {
            ErrorCode = errorCode;
        }
    }

    public static class MarshalEx
    {
        [DllImport("wrapper.dll", EntryPoint = "wrapper_strlen", CallingConvention = CallingConvention.Cdecl)]
        public static extern long StrLen(IntPtr str);
        public unsafe static string PtrToStringUTF8(IntPtr ptr)
        {
            int len = (int)StrLen(ptr);
            return Encoding.UTF8.GetString((byte*)ptr, len);
        }
    }
}
