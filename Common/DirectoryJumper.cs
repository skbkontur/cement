using System;
using System.IO;

namespace Common
{
    public sealed class DirectoryJumper : IDisposable
    {
        private readonly string oldCurrentDirectory;

        public DirectoryJumper(string path)
        {
            oldCurrentDirectory = Directory.GetCurrentDirectory();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Directory.SetCurrentDirectory(path);
        }

        public void Dispose()
        {
            Directory.SetCurrentDirectory(oldCurrentDirectory);
        }
    }
}
