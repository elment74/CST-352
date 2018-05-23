// SimpleFS.cs
// Pete Myers
// Spring 2018

// NOTE: Implement the methods and classes in this file

using System;
using System.Collections.Generic;
using System.Linq;


namespace SimpleFileSystem
{
    public class SimpleFS : FileSystem
    {
        #region filesystem

        //
        // File System
        //

        private const char PATH_SEPARATOR = FSConstants.PATH_SEPARATOR;
        private const int MAX_FILE_NAME = FSConstants.MAX_FILENAME;
        private const int BLOCK_SIZE = 500;     // 500 bytes... 2 sectors of 256 bytes each (minus sector overhead)

        private VirtualFS virtualFileSystem;

        public SimpleFS()
        {
            virtualFileSystem = new VirtualFS();
        }

        public void Mount(DiskDriver disk, string mountPoint)
        {
            virtualFileSystem.Mount(disk, mountPoint);
        }

        public void Unmount(string mountPoint)
        {
            virtualFileSystem.Unmount(mountPoint);
        }

        public void Format(DiskDriver disk)
        {
            virtualFileSystem.Format(disk);
        }

        public Directory GetRootDirectory()
        {
            // SimpleFS.GetRootDirectory()
            return new SimpleDirectory(virtualFileSystem.RootNode);
        }

        public FSEntry Find(string path)
        {
            // good:  /foo/bar, /foo/bar/
            // bad:  foo, foo/bar, //foo/bar, /foo//bar, /foo/../foo/bar

            // TODO: SimpleFS.Find()
            return null;
        }

        public char PathSeparator { get { return PATH_SEPARATOR; } }
        public int MaxNameLength { get { return MAX_FILE_NAME; } }

        #endregion

        #region implementation

        //
        // FSEntry
        //

        abstract private class SimpleEntry : FSEntry
        {
            protected VirtualNode node;

            protected SimpleEntry(VirtualNode node)
            {
                this.node = node;
            }

            public string Name => node.Name;
            public Directory Parent => node.Parent == null ? null : new SimpleDirectory(node.Parent);

            public string FullPathName
            {
                get
                {
                    // TODO: SimpleEntry.FullPathName.get
                    return null;
                }
            }

            // override in derived classes
            public virtual bool IsDirectory => node.IsDirectory;
            public virtual bool IsFile => node.IsFile;

            public void Rename(string name)
            {
                // TODO: SimpleEntry.Rename()
            }

            public void Move(Directory destination)
            {
                // TODO: SimpleEntry.Move()
            }

            public void Delete()
            {
                // TODO: SimpleEntry.Delete()
            }
        }

        //
        // Directory
        //

        private class SimpleDirectory : SimpleEntry, Directory
        {
            public SimpleDirectory(VirtualNode node) : base(node)
            {
            }

            public IEnumerable<Directory> GetSubDirectories()
            {
                // TODO: SimpleDirectory.GetSubDirectories()
                return null;
            }

            public IEnumerable<File> GetFiles()
            {
                // TODO: SimpleDirectory.GetFiles()
                return null;
            }

            public Directory CreateDirectory(string name)
            {
                // SimpleDirectory.CreateDirectory()
				return new SimpleDirectory(node.CreateDirectoryNode(name));
            }

            public File CreateFile(string name)
            {
                // TODO: SimpleDirectory.CreateFile()
                return null;
            }
        }

        //
        // File
        //

        private class SimpleFile : SimpleEntry, File
        {
            public SimpleFile(VirtualNode node) : base(node)
            {
            }

            public int Length => node.FileLength;

            public FileStream Open()
            {
                // TODO: SimpleFile.Open()
                return null;
            }

        }

        //
        // FileStream
        //

        private class SimpleStream : FileStream
        {
            private VirtualNode node;

            public SimpleStream(VirtualNode node)
            {
                this.node = node;
            }

            public void Close()
            {
                // TODO: SimpleStream.Close()
            }

            public byte[] Read(int index, int length)
            {
                // TODO: SimpleStream.Read()
                return null;
            }

            public void Write(int index, byte[] data)
            {
                // TODO: SimpleStream.Write()
            }
        }

        #endregion
    }
}
