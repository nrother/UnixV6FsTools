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
        SetUserId = 0x800, //04000
        SetGroupId = 0x400, //02000
        UserRead = 0x100, //0400
        UserWrite = 0x80, //0200
        UserExecute = 0x40, //0100
        GroupRead = 0x20, //0040
        GroupWrite = 0x10, //0020
        GroupExecute = 0x8, //0010
        OthersRead = 0x4, //0004
        OthersWrite = 0x2, //0002
        OthersExecute = 0x1, //0001
    }

    enum FileType
    {
        //please note that these types overlap and are not really "flags" in that sense
        Plain = 0x0, //0
        Directory = 0x4000, //040000
        CharSpecial = 0x2000, //020000
        BlockSpecial = 0x6000, //060000
    }
    
    class Inode
    {
        public const int INODE_SIZE = 32; //32byte per Inode
        public const int BLOCK_COUNT = 8; //8 direct blocks per Inode
        public const int INDIRECT_BLOCK_COUNT = FileSystem.BLOCK_SIZE / 2; //16bit per indirect block

        private const int LARGE_FLAG = 0x1000; //010000
        private const int ALLOCATED_FLAG = 0x8000; //0100000

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
            inode.Permissions = (Permissions)(mode & 0x1FF /*0777*/); //mask out non-permission bits
            inode.FileType = (FileType)(mode & 0x6000 /*060000*/); //s.a.
            inode.IsLarge = (mode & LARGE_FLAG) != 0;
            inode.LinkCount = stream.ReadByte(); //nlink
            inode.UserId = stream.ReadByte(); //uid
            inode.GroupId = stream.ReadByte(); //gid
            inode.Size = stream.ReadByte() << 16 | stream.ReadUInt16(); //yeah, 24-bit unsigned integer

            for (int i = 0; i < BLOCK_COUNT; i++)
                inode.Blocks[i] = stream.ReadInt16(); //addr[8]

            uint actime = stream.ReadUInt32(); //actime[2]
            uint modtime = stream.ReadUInt32(); //modtime[2]
            inode.AccessTime = new DateTime(1970, 1, 1).AddSeconds(actime);
            inode.LastModified = new DateTime(1970, 1, 1).AddSeconds(mode);

            return inode;
        }
    }
}
