using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace ZBrad.FabricLib.Utilities
{
    /// <summary>
    /// safely enumerate thru files and subfolders
    /// </summary>
    public static class SafeEnumerator
    {
        /// <summary>
        /// get safe file enumeration
        /// </summary>
        /// <param name="root">root path</param>
        /// <param name="pattern">pattern to match</param>
        /// <returns>enumerator of files</returns>
        public static IEnumerator<string> GetFileEnumerator(string root, string pattern)
        {
            return new FileEnumerator(root, pattern);
        }

        /// <summary>
        /// gets a safe enumerator of directories
        /// </summary>
        /// <param name="root">root path</param>
        /// <param name="pattern">pattern to match</param>
        /// <returns>enumerator of directories</returns>
        public static IEnumerator<string> GetDirectoryEnumerator(string root, string pattern)
        {
            return new DirectoryEnumerator(root, pattern);
        }

        private sealed class FileEnumerator : IEnumerator<string>, IDisposable
        {
            private IEnumerator<string> files;
            private IEnumerator<string> dirs;
            private string root;
            private string pattern;
            private bool isDisposed = false;

            public FileEnumerator(string root, string pattern)
            {
                int pos = root.IndexOf('\\');
                if (pos + 1 < root.Length && root[pos + 1] == '$')
                {
                    throw new ApplicationException("No scanning $ folders");
                }

                this.root = root;
                this.pattern = pattern;
                this.files = Directory.EnumerateFiles(root, pattern).GetEnumerator();
                this.dirs = Directory.EnumerateDirectories(root).GetEnumerator();
            }

            ~FileEnumerator()
            {
                this.Dispose(false);
            }

            public string Current
            {
                get
                {
                    if (this.files == null)
                    {
                        throw new ObjectDisposedException("FileEnumerator");
                    }

                    return this.files.Current;
                }
            }

            /// <summary>
            /// gets current element
            /// </summary>
            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            /// move to next element
            /// </summary>
            /// <returns>true if has element</returns>
            public bool MoveNext()
            {
                while (this.files != null)
                {
                    try
                    {
                        if (this.files.MoveNext())
                            return true;
                    }
                    catch
                    {
                        // ok, this enumerator is done
                    }

                    this.files.Dispose();
                    this.files = null;

                    if (this.dirs != null)
                    {
                        try
                        {
                            if (this.dirs.MoveNext())
                            {
                                this.files = SafeEnumerator.GetFileEnumerator(this.dirs.Current, this.pattern);
                                continue;
                            }
                        }
                        catch
                        {
                            // no action
                        }

                        this.dirs.Dispose();
                        this.dirs = null;
                    }
                }

                return false;
            }

            /// <summary>
            /// reset enumeration
            /// </summary>
            public void Reset()
            {
                // clean up any previous
                if (this.files != null)
                    this.files.Dispose();

                if (this.dirs != null)
                    this.dirs.Dispose();

                // get new enumerators
                this.files = Directory.EnumerateFiles(this.root, this.pattern).GetEnumerator();
                this.dirs = Directory.EnumerateDirectories(this.root).GetEnumerator();
            }

            /// <summary>
            /// dispose this enumeration
            /// </summary>
            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool isDisposing)
            {
                if (this.isDisposed)
                    return;

                if (this.files != null)
                    this.files.Dispose();

                if (this.dirs != null)
                    this.dirs.Dispose();

                this.isDisposed = true;
            }
        }

        private sealed class DirectoryEnumerator : IEnumerator<string>, IDisposable
        {
            private IEnumerator<string> dirs;
            private IEnumerator<string> subdirs;

            private string root;
            private string pattern;
            private bool isDisposed = false;

            public DirectoryEnumerator(string root, string pattern)
            {
                int pos = root.IndexOf('\\');
                if (pos + 1 < root.Length && root[pos + 1] == '$')
                {
                    throw new ApplicationException("No scanning $ folders");
                }

                this.root = root;
                this.pattern = pattern;
                this.dirs = Directory.EnumerateDirectories(this.root, this.pattern).GetEnumerator();
                this.subdirs = Directory.EnumerateDirectories(this.root).GetEnumerator();
            }

            ~DirectoryEnumerator()
            {
                this.Dispose(false);
            }

            public string Current
            {
                get
                {
                    if (this.dirs.Current == null)
                    {
                        throw new ObjectDisposedException("DirectoryEnumerator");
                    }

                    return this.dirs.Current;
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                while (this.dirs != null)
                {
                    try
                    {
                        if (this.dirs.MoveNext())
                            return true;
                    }
                    catch
                    {
                        // no action
                    }

                    this.dirs.Dispose();
                    this.dirs = null;

                    if (this.subdirs != null)
                    {
                        try
                        {
                            if (this.subdirs.MoveNext())
                            {
                                this.dirs = SafeEnumerator.GetDirectoryEnumerator(this.subdirs.Current, this.pattern);
                                continue;
                            }
                        }
                        catch
                        {
                            // no action
                        }

                        this.subdirs.Dispose();
                        this.subdirs = null;
                    }
                }

                return false;
            }

            public void Reset()
            {
                // clean up any previous
                if (this.dirs != null)
                    this.dirs.Dispose();
                if (this.subdirs != null)
                    this.subdirs.Dispose();

                // get new enumerators
                this.dirs = Directory.EnumerateDirectories(this.root, this.pattern).GetEnumerator();
                this.subdirs = Directory.EnumerateDirectories(this.root).GetEnumerator();
            }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool isDisposing)
            {
                if (this.isDisposed)
                    return;

                if (this.dirs != null)
                    this.dirs.Dispose();

                if (this.subdirs != null)
                    this.subdirs.Dispose();

                this.isDisposed = true;
            }
        }
    }
}
