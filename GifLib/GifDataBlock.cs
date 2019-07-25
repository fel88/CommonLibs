using System.IO;

namespace GifLib
{
    public class GifDataBlock
    {
        public long Shift { get; set; }
        public virtual void Write(Stream ms)
        {

        }
    }
}
