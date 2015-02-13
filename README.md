UnixV6FsTools
=============

These are some tools to work with UNIX V6 disk images (*.dsk),
as used by some PDP11 emulators like [SimH](http://simh.trailing-edge.com/).

Currently unpacking to a directory is supported. Just start the programm to
unpack all images under /Images to directories.

Re-packing the images from the unpacked files and maybe support for FUSE are
in consideration.

The images contained here are the original UNIX V6 images with
a few modifications (like support for years > 1999) as used
in a great seminar at [Uni Hannover](http://www.uni-hannover.de).

Notes
-----

The main program should give you an idea of the workings.
The main entry point is FileSystem.Create() which reads things
like the super block and all Inodes from the images, and then
starts creating `File` instances from these.

All meta-data is correctly unpacked into the `File` instances,
but currently not written to the unpacked files. Also things like
links are currently not implemented.

Please note that the `File` instances have no `Name` property,
as a file may have many names in UNIX (due to hardlinks). Use
the name from the directoy entry that pointed to that files, but
be aware that a single file might be contained in multiple directories.

Please note that this is not a implementation of a file system
in that sense, that it only redirects reads/writes to the correct
place in the image, instead the whole image is read into memory
and the dissected from theres. Since RK05 disks as supported by
the PDP11/UNIX have a maximum capacity of about 5MB this is not
a huge problem.

Feel free to contribute, Patches/PRs welcome!

License
-------
MIT