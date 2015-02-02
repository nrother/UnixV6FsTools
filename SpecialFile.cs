using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnixV6FsTools
{
    enum SpecialFileType
    {
        Char,
        Block
    }

    class SpecialFile : File
    {
        public byte MajorDeviceId { get; set; }
        public byte MinorDeviceId { get; set; }
        public SpecialFileType Type { get; set; }
        public override bool IsSpecial { get { return true; } }
    }
}
