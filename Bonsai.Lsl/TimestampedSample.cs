using System;

namespace Bonsai.Lsl
{
    /// <summary>
    /// Represents a timestamped sample.
    /// </summary>
    /// <typeparam name="T">The type of the data samples.</typeparam>
    public readonly struct TimestampedSample<T> : IEquatable<TimestampedSample<T>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimestampedSample{T}"/> structure
        /// with the specified data samples and corresponding timestamp.
        /// </summary>
        /// <param name="data">The sample values for each channel of the timestamped sample.</param>
        /// <param name="timestamp">The timestamp of the data sample, in fractional seconds.</param>
        public TimestampedSample(T[] data, double timestamp)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Timestamp = timestamp;
        }

        /// <summary>
        /// Gets the timestamp of the data sample, in fractional seconds.
        /// </summary>
        public double Timestamp { get; }

        /// <summary>
        /// Gets the sample values for each channel of the timestamped sample.
        /// </summary>
        public T[] Data { get; }

        /// <summary>
        /// Deconstructs the components of a timestamped sample into separate variables.
        /// </summary>
        /// <param name="data">The sample values for each channel of the timestamped sample.</param>
        /// <param name="timestamp">The timestamp of the data sample, in fractional seconds.</param>
        public void Deconstruct(out T[] data, out double timestamp)
        {
            data = Data;
            timestamp = Timestamp;
        }

        /// <summary>
        /// Returns a value indicating whether this instance has the same data and timestamp
        /// as a specified <see cref="TimestampedSample{T}"/> structure.
        /// </summary>
        /// <param name="other">The <see cref="TimestampedSample{T}"/> structure to compare to this instance.</param>
        /// <returns>
        /// <b>true</b> if <paramref name="other"/> has the same data and timestamp as this
        /// instance; otherwise, <b>false</b>.
        /// </returns>
        public bool Equals(TimestampedSample<T> other)
        {
            return Timestamp == other.Timestamp && Data == other.Data;
        }

        /// <summary>
        /// Tests to see whether the specified object is a <see cref="TimestampedSample{T}"/> structure
        /// with the same data and timestamp as this <see cref="TimestampedSample{T}"/> structure.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to test.</param>
        /// <returns>
        /// <b>true</b> if <paramref name="obj"/> is a <see cref="TimestampedSample{T}"/> and has the
        /// same data and timestamp as this <see cref="TimestampedSample{T}"/>; otherwise, <b>false</b>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is TimestampedSample<T> timestamped && Equals(timestamped);
        }

        /// <summary>
        /// Returns a hash code for this <see cref="TimestampedSample{T}"/> structure.
        /// </summary>
        /// <returns>An integer value that specifies a hash value for this <see cref="TimestampedSample{T}"/> structure.</returns>
        public override int GetHashCode()
        {
            return Timestamp.GetHashCode() ^ Data.GetHashCode();
        }

        /// <summary>
        /// Creates a <see cref="string"/> representation of this <see cref="TimestampedSample{T}"/>
        /// structure.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> containing the <see cref="Data"/> and <see cref="Timestamp"/>
        /// properties of this <see cref="TimestampedSample{T}"/> structure.
        /// </returns>
        public override string ToString()
        {
            return $"{Data}@{Timestamp}";
        }

        /// <summary>
        /// Tests whether two <see cref="TimestampedSample{T}"/> structures are equal.
        /// </summary>
        /// <param name="left">The <see cref="TimestampedSample{T}"/> structure on the left of the equality operator.</param>
        /// <param name="right">The <see cref="TimestampedSample{T}"/> structure on the right of the equality operator.</param>
        /// <returns>
        /// <b>true</b> if <paramref name="left"/> and <paramref name="right"/> have the same data and timestamp;
        /// otherwise, <b>false</b>.
        /// </returns>
        public static bool operator ==(TimestampedSample<T> left, TimestampedSample<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests whether two <see cref="TimestampedSample{T}"/> structures are different.
        /// </summary>
        /// <param name="left">The <see cref="TimestampedSample{T}"/> structure on the left of the inequality operator.</param>
        /// <param name="right">The <see cref="TimestampedSample{T}"/> structure on the right of the inequality operator.</param>
        /// <returns>
        /// <b>true</b> if <paramref name="left"/> and <paramref name="right"/> differ either in data or timestamp;
        /// <b>false</b> if <paramref name="left"/> and <paramref name="right"/> are equal.
        /// </returns>
        public static bool operator !=(TimestampedSample<T> left, TimestampedSample<T> right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Provides static methods for creating timestamped sample objects.
    /// </summary>
    public static class TimestampedSample
    {
        /// <summary>
        /// Creates a new timestamped sample.
        /// </summary>
        /// <typeparam name="T">The type of the data samples.</typeparam>
        /// <param name="data">The sample values for each channel of the timestamped sample.</param>
        /// <param name="timestamp">The timestamp of the data sample, in fractional seconds.</param>
        /// <returns>
        /// A new instance of the <see cref="TimestampedSample{T}"/> class with the specified
        /// data samples and corresponding timestamp.
        /// </returns>
        public static TimestampedSample<T> Create<T>(T[] data, double timestamp)
        {
            return new TimestampedSample<T>(data, timestamp);
        }
    }
}
