using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZBrad.FabLibs.Utilities
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
            private IEnumerator<string> fileEnumerator;
            private IEnumerator<string> directoryEnumerator;
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
                this.fileEnumerator = System.IO.Directory.EnumerateFiles(root, pattern).GetEnumerator();
                this.directoryEnumerator = System.IO.Directory.EnumerateDirectories(root).GetEnumerator();
            }

            ~FileEnumerator()
            {
                this.Dispose(false);
            }

            public string Current
            {
                get
                {
                    if (this.fileEnumerator == null)
                    {
                        throw new ObjectDisposedException("FileEnumerator");
                    }

                    return this.fileEnumerator.Current;
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
                while (this.fileEnumerator != null)
                {
                    try
                    {
                        if (this.fileEnumerator.MoveNext())
                            return true;
                    }
                    catch
                    {
                        // ok, this enumerator is done
                    }

                    this.fileEnumerator.Dispose();
                    this.fileEnumerator = null;

                    if (this.directoryEnumerator != null)
                    {
                        try
                        {
                            if (this.directoryEnumerator.MoveNext())
                            {
                                this.fileEnumerator = SafeEnumerator.GetFileEnumerator(this.directoryEnumerator.Current, this.pattern);
                                continue;
                            }
                        }
                        catch
                        {
                            // no action
                        }

                        this.directoryEnumerator.Dispose();
                        this.directoryEnumerator = null;
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
                if (this.fileEnumerator != null)
                    this.fileEnumerator.Dispose();
                if (this.directoryEnumerator != null)
                    this.directoryEnumerator.Dispose();

                // get new enumerators
                this.fileEnumerator = System.IO.Directory.EnumerateFiles(this.root, this.pattern).GetEnumerator();
                this.directoryEnumerator = System.IO.Directory.EnumerateDirectories(this.root).GetEnumerator();
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

                if (this.fileEnumerator != null)
                {
                    this.fileEnumerator.Dispose();
                    this.fileEnumerator = null;
                }

                if (this.directoryEnumerator != null)
                {
                    this.directoryEnumerator.Dispose();
                    this.directoryEnumerator = null;
                }

                this.isDisposed = true;
            }
        }

        private sealed class DirectoryEnumerator : IEnumerator<string>, IDisposable
        {
            private IEnumerator<string> directoryEnumerator;
            private IEnumerator<string> subdirEnumerator;

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
                this.directoryEnumerator = System.IO.Directory.EnumerateDirectories(this.root, this.pattern).GetEnumerator();
                this.subdirEnumerator = System.IO.Directory.EnumerateDirectories(this.root).GetEnumerator();
            }

            ~DirectoryEnumerator()
            {
                this.Dispose(false);
            }

            public string Current
            {
                get
                {
                    if (this.directoryEnumerator.Current == null)
                    {
                        throw new ObjectDisposedException("DirectoryEnumerator");
                    }

                    return this.directoryEnumerator.Current;
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                while (this.directoryEnumerator != null)
                {
                    try
                    {
                        if (this.directoryEnumerator.MoveNext())
                            return true;
                    }
                    catch
                    {
                        // no action
                    }

                    this.directoryEnumerator.Dispose();
                    this.directoryEnumerator = null;

                    if (this.subdirEnumerator != null)
                    {
                        try
                        {
                            if (this.subdirEnumerator.MoveNext())
                            {
                                this.directoryEnumerator = SafeEnumerator.GetDirectoryEnumerator(this.subdirEnumerator.Current, this.pattern);
                                continue;
                            }
                        }
                        catch
                        {
                            // no action
                        }

                        this.subdirEnumerator.Dispose();
                        this.subdirEnumerator = null;
                    }
                }

                return false;
            }

            public void Reset()
            {
                // clean up any previous
                if (this.directoryEnumerator != null)
                    this.directoryEnumerator.Dispose();
                if (this.subdirEnumerator != null)
                    this.subdirEnumerator.Dispose();

                // get new enumerators
                this.directoryEnumerator = System.IO.Directory.EnumerateDirectories(this.root, this.pattern).GetEnumerator();
                this.subdirEnumerator = System.IO.Directory.EnumerateDirectories(this.root).GetEnumerator();
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

                if (this.directoryEnumerator != null)
                {
                    this.directoryEnumerator.Dispose();
                    this.directoryEnumerator = null;
                }

                if (this.subdirEnumerator != null)
                {
                    this.subdirEnumerator.Dispose();
                    this.subdirEnumerator = null;
                }

                this.isDisposed = true;
            }
        }
    }
}
