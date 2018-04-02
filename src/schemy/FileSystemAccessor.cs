// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Schemy
{
    using System;
    using System.IO;

    /// <summary>
    /// The only interface that the file system should be exposed within a Schemy interpreter.
    /// </summary>
    /// <remarks>
    /// One could implement this interface in a way such that the interpreter can be used to access "files" in 
    /// any logical virtual file system. For security purposes, one could also choose to not implement, say, 
    /// <see cref="IFileSystemAccessor.OpenWrite"/> if the interpreter is used in a way that write does not need to supported.
    /// The other (higher) level of protection would be to not expose any builtin function for writing to the file system.
    /// </remarks>
    public interface IFileSystemAccessor
    {
        /// <summary>
        /// Opens the path for read
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>the stream to read</returns>
        Stream OpenRead(string path);

        /// <summary>
        /// Opens the path for write
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>the stream to write</returns>
        Stream OpenWrite(string path);
    }

    /// <summary>An implementation of <see cref="IFileSystemAccessor"/> that blocks read/write.</summary>
    /// <remarks>This is the default behavior for an interpreter.</remarks>
    public class DisabledFileSystemAccessor : IFileSystemAccessor
    {
        public Stream OpenRead(string path)
        {
            throw new InvalidOperationException("File system access is blocked by DisabledFileSystemAccessor");
        }

        public Stream OpenWrite(string path)
        {
            throw new InvalidOperationException("File system access is blocked by DisabledFileSystemAccessor");
        }
    }

    /// <summary>An implementation of <see cref="IFileSystemAccessor"/> that grants readonly access to the host file system.</summary>
    public class ReadOnlyFileSystemAccessor : IFileSystemAccessor
    {
        public Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public Stream OpenWrite(string path)
        {
            throw new NotSupportedException("Writing to file system is not supported");
        }
    }
}
