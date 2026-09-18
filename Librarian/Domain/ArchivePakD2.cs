using System;

namespace LibrarianTool.Domain
{
    public abstract class ArchivePak
    {
        public virtual String[] FileExtensions { get { return new String[] { "PAK" }; } }


    }
}