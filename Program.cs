using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace UnixV6FsTools
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("UNIX V6 Filesystem Tools by Niklas Rother");

            if (args.Length != 3)
            {
                Console.WriteLine("Usage: unixv6fstools.exe MODE image path");
                Console.WriteLine();
                Console.WriteLine("Where MODE is:");
                Console.WriteLine("\tunpack - unpack the image to the specified path");
                Console.WriteLine("\tpack - pack a image from the specified path");
            }

            switch (args[1])
            {
                case "unpack":
                    unpack(args[2], args[3]);
                    break;
                case "pack":
                    Console.WriteLine("Not yet implemented...");
                    break;
                default:
                    Console.WriteLine("Unknown mode.");
                    break;
            }
        }

        static void unpack(string imageFile, string targetPath)
        {
            Console.WriteLine("Unpacking filesystem...");

            //read the complete file system to memory
            var fs = FileSystem.Create(new FileStream(imageFile, FileMode.Open));

            //...
            DirectoryInfo targetDir = new DirectoryInfo(targetPath);
            if (targetDir.Exists) //kill directory if exists
                targetDir.Delete(true);

            targetDir.Create();
        }
    }
}
