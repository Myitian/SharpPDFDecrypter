using System.Collections.Generic;
using System.IO;

namespace SharpPDFDecrypter
{
    public static class Util
    {
        public static void AddFile(string path, ICollection<TaskInfo> tasks)
        {
            path = Path.GetFullPath(path);
            if (Directory.Exists(path))
            {
                foreach (string file in Directory.EnumerateFiles(path, "*.pdf", SearchOption.AllDirectories))
                {
                    AddFile(file, tasks);
                }
            }
            else if (File.Exists(path))
            {
                TaskInfo info = new TaskInfo(path, GetDecryptedFileName(path));
                tasks.AddIfNotExist(info);
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
