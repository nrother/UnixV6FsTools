using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnixV6FsTools.LowLevel;

namespace UnixV6FsTools
{
    [Flags]
    enum Permissions
    {
        None = 0x0,
        Read = 0x1,
        Write = 0x2,
        Execute = 0x4,
    }

    [Flags]
    enum SpecialPermissions
    {
        None = 0x0,
        SetGroupId = 0x1,
        SetUserId = 0x2,
    }

    /// <summary>
    /// Represents a single file.
    /// This file might be a directory or a special file, represented by
    /// a subclass. Please note that a file has no canonical location or
    /// name since it might be accessible throght multiple links.
    /// When the file is not a plain file it is not avised to modify the
    /// content directly.
    /// </summary>
    class File
    {
        public int Owner { get; set; }
        public int Group { get; set; }
        public Permissions UserPermissions { get; set; }
        public Permissions GroupPermissions { get; set; }
        public Permissions OtherPermissions { get; set; }
        public SpecialPermissions SpecialPermission { get; set; }
        public int LinkCount { get; set; }
        public DateTime AccessTime { get; set; }
        public DateTime LastModified { get; set; }
        public byte[] Content { get; set; }
        public int Size { get { return Content.Length; } }
        public virtual bool IsSpecial { get { return false; } }
        public virtual bool IsDirectory { get { return false; } }

        /// <summary>
        /// Creates a file for the given Inode, reading the content from the
        /// given stream.
        /// This method may return a instance of a subclass if the inode indicates
        /// that this file is a special file or a directory.
        /// If the inode points at a directory the whole underlying directory tree is read.
        /// </summary>
        /// <param name="stream">The stream to read the content from. Must be seekable and readable.</param>
        /// <param name="inodes">An array of all Inodes for the current filesystem.</param>
        /// <param name="inodeNum">The index of the inode contaning the metadata for the file to create.</param>
        public static File Create(Stream stream, Inode[] inodes, int inodeNum)
        {
            Inode inode = inodes[inodeNum - 1]; //for some strange reasone Inode number start at 1, so fix this here

            if (!inode.IsAllocated)
                throw new Exception("Trying to create file of unallocated inode!");

            File file;

            switch (inode.FileType)
            {
                case FileType.Plain:
                    file = new File();
                    break;
                case FileType.Directory:
                    file = new Directory();
                    break;
                case FileType.CharSpecial:
                case FileType.BlockSpecial:
                    file = new SpecialFile();
                    break;
                default:
                    throw new Exception("Unknown type");
            }

            //Copy metadata
            file.Owner = inode.UserId;
            file.Group = inode.GroupId;
            file.UserPermissions = (Permissions)(((int)inode.Permissions >> 6) & 0x7);
            file.GroupPermissions = (Permissions)((int)inode.Permissions >> 3 & 0x7);
            file.UserPermissions = (Permissions)((int)inode.Permissions >> 0 & 0x7);
            file.SpecialPermission = (SpecialPermissions)(((int)inode.Permissions >> 10) & 0x3);

            if (file.IsSpecial) //special file, content is not relevant, but minor/major device id is in the first block number
            {
                SpecialFile special = (SpecialFile)file;
                special.Content = new byte[0]; //for .Size to work

                special.MajorDeviceId = (byte)(inode.Blocks[0] >> 8); //upper byte
                special.MinorDeviceId = (byte)(inode.Blocks[0]); //lower byte
                special.Type = (((int)inode.Permissions & (int)FileType.BlockSpecial) != 0) ? SpecialFileType.Block : SpecialFileType.Char; //note the encoding in FileType!
            }
            else // regular file or directory, copy content
            {
                file.Content = new byte[inode.Size];

                if (!inode.IsLarge) //a small file, simple
                {
                    for (int copiedSize = 0; copiedSize < inode.Size; copiedSize += FileSystem.BLOCK_SIZE)
                    {
                        int thisBlockSize = Math.Min(FileSystem.BLOCK_SIZE, inode.Size - copiedSize); //either a full block or the remaining part
                        int thisBlockNum = copiedSize / FileSystem.BLOCK_SIZE;
                        CopyBlock(stream, file.Content, inode.Blocks[thisBlockNum], copiedSize, thisBlockSize);
                    }
                }
                else //large file, complicated
                {
                    //TODO: This probably has bugs (I'm really confused about it at the moment)
                    int[] currentIndirectBlock = new int[Inode.INDIRECT_BLOCK_COUNT];
                    int[] doubleIndirectBlock = null; //allocated later, if needed
                    BinaryReader reader = new BinaryReader(stream);
                    for (int copiedSize = 0; copiedSize < inode.Size; copiedSize += FileSystem.BLOCK_SIZE)
                    {
                        if (copiedSize % (Inode.INDIRECT_BLOCK_COUNT * FileSystem.BLOCK_SIZE) == 0) //new indirect block started
                        {
                            int directBlockNum = copiedSize / (Inode.INDIRECT_BLOCK_COUNT * FileSystem.BLOCK_SIZE);
                            if (directBlockNum < Inode.BLOCK_COUNT - 1) //not the last block
                                ReadIndirectBlock(reader, currentIndirectBlock, inode.Blocks[directBlockNum]);
                            else //double indirect block!
                            {
                                //read the double indirect block, if not done yet
                                if (doubleIndirectBlock == null)
                                    doubleIndirectBlock = new int[Inode.INDIRECT_BLOCK_COUNT];
                                ReadIndirectBlock(reader, doubleIndirectBlock, inode.Blocks[Inode.BLOCK_COUNT - 1]);
                                //read the current indirect block
                                int thisDoubleIndirectBlockNum = directBlockNum - (Inode.BLOCK_COUNT - 1);
                                int indirectBlockNum = thisDoubleIndirectBlockNum / Inode.INDIRECT_BLOCK_COUNT;
                                ReadIndirectBlock(reader, currentIndirectBlock, doubleIndirectBlock[indirectBlockNum]);
                            }
                        }

                        int thisBlockSize = Math.Min(FileSystem.BLOCK_SIZE, inode.Size - copiedSize); //either a full block or the remaining part
                        int thisIndirectBlockNum = copiedSize / FileSystem.BLOCK_SIZE % Inode.INDIRECT_BLOCK_COUNT;
                        CopyBlock(stream, file.Content, currentIndirectBlock[thisIndirectBlockNum], copiedSize, thisBlockSize);
                    }
                }
            }

            //when we just read a directory read all containing files
            if (file is Directory)
                ((Directory)file).ReadFiles(stream, inodes);

            return file;
        }

        private static void CopyBlock(Stream source, byte[] target, int sourceBlock, int targetPos, int count)
        {
            if (sourceBlock == 0) //empty block, fill target with zeros
                Array.Clear(target, targetPos, count); //TODO: Is this necessary? Can't we assume the array already contains zeros?

            source.Seek(sourceBlock * FileSystem.BLOCK_SIZE, SeekOrigin.Begin);
            source.Read(target, targetPos, count);
        }

        private static void ReadIndirectBlock(BinaryReader reader, int[] target, int sourceBlock)
        {
            reader.BaseStream.Seek(sourceBlock * FileSystem.BLOCK_SIZE, SeekOrigin.Begin);
            for (int i = 0; i < target.Length; i++)
                target[i] = reader.ReadInt16();
        }
    }
}
