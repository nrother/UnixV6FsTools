using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IO = System.IO;
using System.CodeDom.Compiler;

namespace UnixV6FsTools
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var file in IO.Directory.GetFiles("Images", "*.dsk"))
            {
                Console.WriteLine("Unpacking {0}...", file);
                unpack(file, file + "_unpack");
            }
        }

        static void unpack(string imageFile, string targetPath)
        {
            Console.WriteLine("Unpacking filesystem...");

            //read the complete file system to memory
            var fs = FileSystem.Create(new FileStream(imageFile, FileMode.Open));
            PrintTree(fs.RootDirectory, new IndentedTextWriter(Console.Out));


            DirectoryInfo targetDir = new DirectoryInfo(targetPath);
            if (targetDir.Exists) //kill directory if exists
                targetDir.Delete(true);

            targetDir.Create();

            IO.File.WriteAllBytes(Path.Combine(targetDir.FullName, "block0.bin"), fs.BootBlock);
            unpackDir(fs.RootDirectory, targetDir);
        }

        static void unpackDir(Directory dir, DirectoryInfo target)
        {
            foreach (var entry in dir.Entries)
            {
                if (entry.File.IsDirectory)
                {
                    var newDir = target.CreateSubdirectory(entry.Name);
                    unpackDir((Directory)entry.File, newDir);
                }
                else if (entry.File.IsSpecial)
                {
                    var special = (SpecialFile)entry.File;
                    string specialType;
                    switch (special.Type)
                    {
                        case SpecialFileType.Char:
                            specialType = "CharSpecial";
                            break;
                        case SpecialFileType.Block:
                            specialType = "BlockSpecial";
                            break;
                        default:
                            specialType = "???";
                            break;
                    }
                    IO.File.WriteAllText(Path.Combine(target.FullName, entry.Name),
                        string.Format("{0} {1}/{2}", specialType, special.MajorDeviceId, special.MinorDeviceId));
                }
                else
                {
                    IO.File.WriteAllBytes(Path.Combine(target.FullName, entry.Name), entry.File.Content);
                }
            }
        }

        static void PrintTree(Directory dir, IndentedTextWriter writer)
        {
            foreach (var entry in dir.Entries)
            {
                writer.WriteLine(entry.Name);
                writer.Indent++;
                if (entry.File.IsDirectory)
                {
                    PrintTree((Directory)entry.File, writer);
                }
                else if (entry.File.IsSpecial)
                {
                    var special = (SpecialFile)entry.File;
                    string specialType;
                    switch (special.Type)
                    {
                        case SpecialFileType.Char:
                            specialType = "CharSpecial";
                            break;
                        case SpecialFileType.Block:
                            specialType = "BlockSpecial";
                            break;
                        default:
                            specialType = "???";
                            break;
                    }
                    writer.WriteLine("{0} {1}/{2}", specialType, special.MajorDeviceId, special.MinorDeviceId);
                }
                else
                {
                    //nothing, name is enough
                }
                writer.Indent--;
            }
        }
    }
}
