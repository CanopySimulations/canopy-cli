namespace Canopy.Cli.Executable
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public static class FileNameUtilities
    {
        public static string Sanitize(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '-');
            }

            return fileName;
        }
    }
}
