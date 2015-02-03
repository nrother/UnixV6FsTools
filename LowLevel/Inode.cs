using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnixV6FsTools.LowLevel
{
    [Flags]
    enum Permissions
    {
        SetUserId = 04000,
        SetGroupId = 02000,
        UserRead = 0400,
        UserWrite = 0200,
        UserExecute = 0100,
        GroupRead = 0040,
        GroupWrite = 0020,
        GroupExecute = 0010,
        OthersRead = 0004,
        OthersWrite = 0002,
        OthersExecute = 0001,
    }

    enum FileType
    {
        //please note that these types overlap and are not really "flags" in that sense
        Plain = 0,
        Directory = 040000,
        CharSpecial = 020000,
        BlockSpecial = 060000,
    }
    
    class Inode
    {
        public const int INODE_SIZE = 32; //32byte per Inode
        public const int BLOCK_COUNT = 8; //8 direct blocks per Inode
        public const int INDIRECT_BLOCK_COUNT = FileSystem.BLOCK_SIZE / 2; //16bit per indirect block
        
        private const int LARGE_FLAG = 010000;
        private const int ALLOCATED_FLAG = 0100000;

        public bool IsAllocated { get; set; }
        public bool IsLarge { get; set; }
        public Permissions Permissions { get; set; }
        public FileType FileType { get; set; }
        public int LinkCount { get; set; }
        public int UserId { get; set; }
        public int GroupId { get; set; }
        public int Size { get; set; }
        public int[] Blocks { get; set; }
        public DateTime AccessTime { get; set; }
        public DateTime LastModified { get; set; }

        public Inode()
        {
            Blocks = new int[BLOCK_COUNT];
        }

        public static Inode Create(BinaryReader stream)
        {
            var inode = new Inode();
            
            int mode = stream.ReadInt16(); //mode

            inode.IsAllocated = (mode & ALLOCATED_FLAG) != 0;
            inode.Permissions = (Permissions)(mode & 0777); //mask out non-permission bits
            inode.FileType = (FileType)(mode & 060000); //s.a.
            inode.IsLarge = (mode & LARGE_FLAG) != 0;
            inode.LinkCount = stream.ReadByte(); //nlink
            inode.UserId = stream.ReadByte(); //uid
            inode.GroupId = stream.ReadByte(); //gid
            inode.Size = stream.ReadByte() << 16 | stream.ReadUInt16(); //yeah, 24-bit unsigned integer

            for (int i = 0; i < BLOCK_COUNT; i++)
                inode.Blocks[i] = stream.ReadInt16(); //addr[8]

            int actime = stream.ReadInt32(); //actime[2]
            int modtime = stream.ReadInt32(); //modtime[2]
            inode.AccessTime = new DateTime(1970, 1, 1).AddSeconds(actime);
            inode.LastModified = new DateTime(1970, 1, 1).AddSeconds(mode);

            return inode;
        }
    }
}
