using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnixV6FsTools.LowLevel;

namespace UnixV6FsTools
{
    class Directory : File
    {
        private static int MAX_NAME_LENGTH = 14; //14bytes name
        private static int DIRECTORY_ENTRY_SIZE = 2 + MAX_NAME_LENGTH; //2byte inode + name

        public override bool IsDirectory { get { return true; } }
        public List<DirectoryEntry> Entries { get; set; }

        public Directory()
        {
            Entries = new List<DirectoryEntry>();
        }

        /// <summary>
        /// Recursivly read all files and directories in the current directory.
        /// </summary>
        /// <param name="stream">A Stream used to read the content of the files. Must be readable and seekable.</param>
        /// <param name="inodes">An array of all inodes in the current file system.</param>
        public void ReadFiles(Stream stream, Inode[] inodes)
        {
            Entries.Clear();
            foreach (var entry in ReadDirEntries())
            {
                if (entry.Item2 == "." || entry.Item2 == "..")
                    continue; //we don't want recursive links in here

                Entries.Add(new DirectoryEntry()
                {
                    File = File.Create(stream, inodes, entry.Item1),
                    Name = entry.Item2,
                });
            }
        }

        private IEnumerable<Tuple<int, string>> ReadDirEntries()
        {
            for (int i = 0; i < Size; i += DIRECTORY_ENTRY_SIZE)
            {
                int inode = (Content[i + 1] << 8) | Content[i];
                if (inode == 0)
                    continue;
                StringBuilder name = new StringBuilder(MAX_NAME_LENGTH);
                for (int j = 0; j < MAX_NAME_LENGTH; j++) //read the name, might be terminated by \0
                {
                    int c = Content[i + 2 + j];
                    if (c == 0)
                        break;
                    name.Append((char)c);
                }

                yield return Tuple.Create(inode, name.ToString());
            }
        }
    }

    class DirectoryEntry
    {
        public string Name { get; set; }
        public File File { get; set; }
    }
}
