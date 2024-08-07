﻿namespace Utils
{
    using System;

    // http://stackoverflow.com/questions/1633028/calculating-the-path-relative-to-some-root-the-inverse-of-path-combine
    // Used without permission
    public class PathUtils
    {
        static public string NormalizeFilepath(string filepath)
        {
            string result = System.IO.Path.GetFullPath(filepath).ToLowerInvariant();

            result = result.TrimEnd(new[] { '\\' });

            return result;
        }

        public static string GetRelativePath(string rootPath, string fullPath)
        {
            rootPath = NormalizeFilepath(rootPath);
            fullPath = NormalizeFilepath(fullPath);

            if (!fullPath.StartsWith(rootPath))
                throw new Exception("Could not find rootPath in fullPath when calculating relative path.");

            return "." + fullPath.Substring(rootPath.Length);
        }
    }
}