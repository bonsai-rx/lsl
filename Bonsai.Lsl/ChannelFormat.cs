namespace Bonsai.Lsl
{
    /// <summary>
    /// Specifies the data format of a LSL channel.
    /// </summary>
    public enum ChannelFormat
    {
        /// <summary>
        /// Used for up to 24-bit precision measurements in the appropriate physical unit
        /// (e.g. microvolts). Integers from -16777216 to 16777216 are represented accurately.
        /// </summary>
        Float32 = 1,

        /// <summary>
        /// Used for universal numeric data as long as network and disk constraints permit.
        /// The largest representable integer is 53-bit.
        /// </summary>
        Double64 = 2,

        /// <summary>
        /// Used for variable-length ASCII strings or data blobs, such as video frames,
        /// complex event descriptions, etc.
        /// </summary>
        String = 3,

        /// <summary>
        /// Used for high sampling rate digitized formats that require 32-bit precision.
        /// Depends critically on meta-data to represent meaningful units. Also useful for
        /// application event codes or other coded data.
        /// </summary>
        Int32 = 4,

        /// <summary>
        /// Used for very high sampling rate signals (40kHz+) or consumer-grade audio.
        /// </summary>
        Int16 = 5,

        /// <summary>
        /// Used for binary signals or other coded data. Not recommended for encoding
        /// string data.
        /// </summary>
        Int8 = 6,

        /// <summary>
        /// For future compatibility only. Support for this type is not yet exposed in all
        /// languages. Some clients will not be able to send or receive data of this type.
        /// </summary>
        Int64 = 7,

        /// <summary>
        /// Specifies that the data can not be transmitted.
        /// </summary>
        Undefined = 0
    }
}
