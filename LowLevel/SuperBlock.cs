using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnixV6FsTools.LowLevel
{
    class SuperBlock
    {
        public int BlocksForInodes { get; set; }
        public int VolumeSizeInBlocks { get; set; }
        public DateTime LastModified { get; set; }

        public int InodeCount { get { return BlocksForInodes * FileSystem.BLOCK_SIZE / Inode.INODE_SIZE; } }

        public static SuperBlock Create(BinaryReader stream)
        {
            var sb = new SuperBlock();

            sb.BlocksForInodes = stream.ReadInt16(); //isize
            sb.VolumeSizeInBlocks = stream.ReadInt16(); //fsize

            stream.ReadInt16(); //nfree
            for (int i = 0; i < 100; i++)
                stream.ReadInt16(); //free[100], ignored

            stream.ReadInt16(); //ninode
            for (int i = 0; i < 100; i++)
                stream.ReadInt16(); //inode[100], ignored

            stream.ReadByte(); //flock
            stream.ReadByte(); //ilock
            stream.ReadByte(); //fmod
            stream.ReadByte(); //ronly

            int time = stream.ReadInt32(); //time[2]
            sb.LastModified = new DateTime(1970, 1, 1).AddSeconds(time);

            //this is a bit weired: in the original source code, this is pad[50], but this only give a total of
            //466 bytes (which is not a problem in UNIX). We just seek to block 2 to avoid any problems.
            stream.BaseStream.Seek(2 * FileSystem.BLOCK_SIZE, SeekOrigin.Begin);

            return sb;
        }
    }
}
