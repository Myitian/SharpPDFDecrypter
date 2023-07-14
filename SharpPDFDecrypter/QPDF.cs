using System;
using System.Runtime.InteropServices;

namespace QPDFGUI
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
        public static extern QPDF_ERROR_CODE InitWrite(IntPtr qpdf,
                                                             [MarshalAs(UnmanagedType.LPUTF8Str)] string filename);
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
         * Other
         */
        [DllImport("qpdf.dll", EntryPoint = "qpdf_register_progress_reporter", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterProgressReporter(IntPtr qpdf, ReportProgressCallbackDelegate report_progress, IntPtr data);
        [DllImport("qpdf.dll", EntryPoint = "qpdf_set_preserve_encryption", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetPreserveEncryption(IntPtr qpdf, bool value);


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ReportProgressCallbackDelegate(int percent, IntPtr data);

        public event ReportProgressCallbackDelegate ReportProgressCallback;

        private readonly IntPtr qpdf;

        public QPDF_ERROR_CODE ErrorCode { get; private set; }

        public bool PreserveEncryption { get; set; } = true;

        public QPDF()
        {
            qpdf = Init();
            ErrorCode = EmptyPdf(qpdf);
        }

        public QPDF(string filename, string password)
        {
            qpdf = Init();
            ErrorCode = Read(qpdf, filename, password);
        }

        public QPDF(string description, byte[] buffer, string password)
        {
            qpdf = Init();
            ErrorCode = ReadMemory(qpdf, description, buffer, buffer.LongLength, password);
        }

        public void WriteToFile(string filename)
        {
            ErrorCode = InitWrite(qpdf, filename);
            PrepareWrite();
            RegisterProgressReporter(qpdf, OnReportProgressCallback, qpdf);
            ErrorCode = Write(qpdf);
        }

        public byte[] WriteToMemory()
        {
            ErrorCode = InitWriteMemory(qpdf);
            PrepareWrite();
            RegisterProgressReporter(qpdf, OnReportProgressCallback, qpdf);
            ErrorCode = Write(qpdf);
            ulong size = GetBufferLength(qpdf).ToUInt64();
            if (size > int.MaxValue)
            {
                throw new NotSupportedException("Buffer size should not be larger than 2GB");
            }
            IntPtr buffer = GetBuffer(qpdf);
            byte[] result = new byte[size];
            Marshal.Copy(buffer, result, 0, (int)size);
            return result;
        }

        public void Dispose()
        {
            Cleanup(qpdf);
        }

        protected void PrepareWrite()
        {
            SetPreserveEncryption(qpdf, PreserveEncryption);
        }
        protected void OnReportProgressCallback(int percent, IntPtr data)
        {
            ReportProgressCallback?.Invoke(percent, data);
        }
    }

    public enum QPDF_ERROR_CODE
    {
        QPDF_SUCCESS = 0,
        QPDF_WARNINGS = 1 << 0,
        QPDF_ERRORS = 1 << 1
    }
}
