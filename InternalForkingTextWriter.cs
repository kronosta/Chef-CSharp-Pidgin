using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kronosta.ChefCSharpPidgin
{
    /// <summary>
    /// Forks a TextWriter interface to multiple outputs.
    /// This class is accessible because the author believes that encapsulation is folly because it
    /// makes code harder to write when using libraries that employ the practice.
    /// </summary>
    public class InternalForkingTextWriter : TextWriter
    {
        /// <summary>
        /// Gets or sets the encoding of this textWriter
        /// </summary>
        public Encoding InternalEncoding { get; set; }

        /// <summary>
        /// Gets the encoding for this TextWriter,
        /// mainly only here to fulfill the required abstract property of TextWriter.
        /// To set the encoding, use InternalEncoding.
        /// </summary>
        public override Encoding Encoding { get => InternalEncoding; }
        
        /// <summary>
        /// A list of TextWriters to write data to whenever you call a WriteX method on this InternalForkingTextWriter.
        /// </summary>
        public List<TextWriter> Forks { get; set; }

        /// <summary>
        /// Constructs an InternalForkingTextWriter
        /// </summary>
        /// <param name="forks">The list of TextWriters to fork to</param>
        /// <param name="encoding">An encoding</param>
        public InternalForkingTextWriter(List<TextWriter> forks, Encoding encoding)
        {
            InternalEncoding = encoding;
            Forks = forks;
        }

        /// <summary>
        /// Writes a character to all forks. All other WriteX methods call this.
        /// </summary>
        /// <param name="value">The character to write</param>
        public override void Write(char value) => Forks.ForEach(x => x.Write(value));
    }
}
