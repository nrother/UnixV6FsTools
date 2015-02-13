using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnixV6FsTools.LowLevel;

namespace UnixV6FsTools
{
    class FileSystem
    {
        public const int BLOCK_SIZE = 512; //512bytes per block

        private const int ROOT_INODE = 1;

        public SuperBlock SuperBlock { get; set; }
        public byte[] BootBlock { get; set; }
        public Directory RootDirectory { get; set; }

        public FileSystem()
        {
            BootBlock = new byte[BLOCK_SIZE];
        }

        public static FileSystem Create(Stream stream)
        {
            var fs = new FileSystem();
            var reader = new BinaryReader(stream);

            //read bootblock
            stream.Read(fs.BootBlock, 0, BLOCK_SIZE);

            //read superblock
            fs.SuperBlock = SuperBlock.Create(reader);

            //read all inodes
            Inode[] inodes = new Inode[fs.SuperBlock.InodeCount];
            for (int i = 0; i < fs.SuperBlock.InodeCount; i++)
            {
                inodes[i] = Inode.Create(reader);
            }

            //recursivly read all files
            fs.RootDirectory = (Directory)File.Create(stream, inodes, ROOT_INODE);

            return fs;
        }
    }
}
