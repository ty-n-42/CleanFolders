


using System;
using System.Linq;

namespace CleanFolders
{
    internal class Program
    {
        private static int CompareByPathDepth(string path1, string path2)
        {
            Func<char, bool> IsDirectorySeparator = c => ((c == System.IO.Path.DirectorySeparatorChar) || (c == System.IO.Path.AltDirectorySeparatorChar));

            int path1Count = -1;
            if (string.IsNullOrEmpty(path1) == false)
            {
                path1Count = path1.Where(IsDirectorySeparator).Count(); // count where each element of path1 is a directory separator
            }

            int path2Count = -1;
            if (string.IsNullOrEmpty(path2) == false)
            {
                path2Count = path2.Where(IsDirectorySeparator).Count();
            }

            if (path1Count > path2Count) { return 1; }
            if (path1Count < path2Count) { return -1; }
            return 0;
        }

        /// <summary>
        /// Remove empty subfolders of folderPath
        /// </summary>
        /// <param name="folderPath">parent folder for subfolders being processed</param>
        /// <returns>list of paths, for deleted subfolders</returns>
        public static System.Collections.Generic.List<string> CleanFolders(string folderPath)
        {
            System.Console.WriteLine(" cleaning directories for " + folderPath);

            System.Collections.Generic.List<string> deletedFolders = new System.Collections.Generic.List<string>();

            System.Collections.Generic.List<string> subfolderPaths = new System.Collections.Generic.List<string>(System.IO.Directory.GetDirectories(folderPath, "*.*", System.IO.SearchOption.AllDirectories));

            // sort by longest depth first
            subfolderPaths.Sort(CompareByPathDepth);
            subfolderPaths.Reverse();

            foreach (string subfolderPath in subfolderPaths)
            {
                if (subfolderPath.ToLower().EndsWith("cache") || 
                    subfolderPath.ToLower().EndsWith("thumbnails") ||
                    subfolderPath.ToLower().EndsWith("stabilization") ||
                    subfolderPath.ToLower().EndsWith("waveforms") ||
                    subfolderPath.ToLower().EndsWith("peaks data") ||
                    subfolderPath.ToLower().EndsWith("thumbnail media") ||
                    subfolderPath.ToLower().EndsWith("quicklook"))
                {
                    System.Console.WriteLine("  deleting [name] " + subfolderPath);
                    System.IO.Directory.Delete(subfolderPath, true); // deletes contents of directory and subdirectories
                    deletedFolders.Add(subfolderPath);
                    continue;
                }

                System.Collections.Generic.IEnumerable<string> entries = System.IO.Directory.EnumerateFileSystemEntries(subfolderPath, "*.*", System.IO.SearchOption.TopDirectoryOnly);
                if (entries.Any() == false)
                { // nothing in the folder
                    System.Console.WriteLine("  deleting [empty] " + subfolderPath);
                    System.IO.Directory.Delete(subfolderPath); // error if the folder contains anything
                    deletedFolders.Add(subfolderPath);
                    continue;
                }
            }

            return deletedFolders;
        }

        public static System.Collections.Generic.List<string> CleanFiles(string folderPath)
        {
            System.Console.WriteLine(" cleaning files for " + folderPath);

            System.Collections.Concurrent.ConcurrentStack<string> deletedFiles = new System.Collections.Concurrent.ConcurrentStack<string>();
            System.Collections.Concurrent.ConcurrentDictionary<string, int> fileExtensions = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();

            System.Collections.Generic.IEnumerable<string> subfolderPathsEnumeration = System.IO.Directory.EnumerateDirectories(folderPath, "*.*", System.IO.SearchOption.AllDirectories);
            foreach (string subfolderPath in subfolderPathsEnumeration)
            {
                string[] files = System.IO.Directory.GetFiles(subfolderPath, "*.*", System.IO.SearchOption.TopDirectoryOnly);
                const long minFileLength =  50 * 1024; // 50 KB
                foreach (string file in files)
                {
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(file);
                    
                    // hidden files
                    System.IO.FileAttributes fileAttributes = System.IO.File.GetAttributes(file);
                    if ((fileAttributes & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden)
                    {
                        System.Console.WriteLine("  deleting [hidden] " + file);
                        System.IO.File.Delete(file); // delete hidden files
                        deletedFiles.Push(file);
                        continue;
                    }

                    // file name extensions
                    switch (fileInfo.Extension.ToLower())
                    {
                        case ".m4v":
                        case ".m4a":
                        case ".mov":
                        case ".jpeg":
                        case ".jpg":
                            break;
                        default:
                            System.Console.WriteLine("  deleting [extension] " + file);
                            System.IO.File.Delete(file); // delete unwanted file types
                            deletedFiles.Push(file);
                            continue;
                    }
                    
                    // file length
                    if (fileInfo.Length < minFileLength)
                    {
                        System.Console.WriteLine("  candidate [{0,9:N0} B] {1}", fileInfo.Length, file);
                        System.Console.Write("  press y to delete ");
                        System.ConsoleKeyInfo confirmation = System.Console.ReadKey();
                        System.Console.WriteLine();

                        if (confirmation.KeyChar == 'y')
                        {
                            System.Console.WriteLine("  deleting [{0,9:N0} B] {1}", fileInfo.Length, file);
                            System.IO.File.Delete(file); // delete small files
                            System.Console.WriteLine();
                            deletedFiles.Push(file);
                        }
                    }
                }
            }

            return new System.Collections.Generic.List<string>(deletedFiles);
        }

        static void Main(string[] args)
        {
            System.Diagnostics.Debug.Assert(args.Length == 1, "FAIL, expected only 1 argument");
            System.Diagnostics.Debug.Assert(System.IO.Directory.Exists(args[0]), "FAIL, directory does not exist: " + args[0]);
            System.Diagnostics.Debug.Assert(args[0].EndsWith("" + System.IO.Path.DirectorySeparatorChar) == false, "FAIL, directory path ends with a directory seperator character: " + args[0]);

            string inputPath = args[0];
            System.Diagnostics.Debug.Assert(string.IsNullOrEmpty(inputPath) == false, "FAIL, input path is empty");

            System.Console.WriteLine("** starting " + inputPath + " **");
            System.Collections.Generic.List<string> deletedFolders1 = CleanFolders(inputPath);
            System.Collections.Generic.List<string> deletedFiles = CleanFiles(inputPath);
            System.Collections.Generic.List<string> deletedFolders2 = CleanFolders(inputPath);
            System.Console.WriteLine("** finished " + inputPath + " **");

            System.Console.WriteLine("Finished, press ENTER to quit");
            System.Console.ReadLine();
        }
    }
}
