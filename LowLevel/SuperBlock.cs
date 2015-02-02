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
        public List<int> FreeBlocks { get; set; }
        public DateTime LastModified { get; set; }

        public int InodeCount { get { return BlocksForInodes * FileSystem.BLOCK_SIZE / Inode.INODE_SIZE; } }

        public SuperBlock()
        {
            FreeBlocks = new List<int>();
        }

        public static SuperBlock Create(BinaryReader stream)
        {
            var sb = new SuperBlock();

            sb.BlocksForInodes = stream.ReadInt16(); //isize
            sb.VolumeSizeInBlocks = stream.ReadInt16(); //fsize

            int nfree = stream.ReadInt16(); //nfree
            for (int i = 0; i < nfree; i++)
                sb.FreeBlocks.Add(stream.ReadInt16()); //free[100]

            //there are always 100 blocks on disk, some might be invalid, skip those
            stream.BaseStream.Seek((100 - nfree) * 2, SeekOrigin.Current);

            stream.ReadInt16(); //ninode
            for (int i = 0; i < 100; i++)
                stream.ReadInt16(); //inode[100], ignored

            stream.ReadByte(); //flock
            stream.ReadByte(); //ilock
            stream.ReadByte(); //fmod
            stream.ReadByte(); //ronly

            int time = stream.ReadInt32(); //time[2]
            sb.LastModified = new DateTime(1970, 1, 1).AddSeconds(time);

            //pad[50], ignored
            stream.BaseStream.Seek(50 * 2, SeekOrigin.Current);

            return sb;
        }
    }
}
