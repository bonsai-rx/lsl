using System;

namespace Bonsai.Lsl
{
    /// <summary>
    /// Represents a timestamped chunk.
    /// </summary>
    /// <typeparam name="T">The type of the data samples.</typeparam>
    public readonly struct TimestampedChunk<T> : IEquatable<TimestampedChunk<T>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimestampedChunk{T}"/> structure
        /// with the specified data samples and corresponding timestamps.
        /// </summary>
        /// <param name="data">The data samples of the timestamped chunk.</param>
        /// <param name="timestamps">The timestamps for each data sample, in fractional seconds.</param>
        public TimestampedChunk(T[,] data, double[] timestamps)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Timestamps = timestamps ?? throw new ArgumentNullException(nameof(timestamps));
            if (data.Length != timestamps.Length)
            {
                throw new ArgumentException("The number of timestamps must match the number of samples.", nameof(timestamps));
            }
        }

        /// <summary>
        /// Gets the timestamps for each data sample, in fractional seconds.
        /// </summary>
        public double[] Timestamps { get; }

        /// <summary>
        /// Gets the data samples of the timestamped chunk.
        /// </summary>
        public T[,] Data { get; }

        /// <summary>
        /// Deconstructs the components of a timestamped chunk into separate variables.
        /// </summary>
        /// <param name="data">The data samples of the timestamped chunk.</param>
        /// <param name="timestamps">The timestamps for each data sample, in fractional seconds.</param>
        public void Deconstruct(out T[,] data, out double[] timestamps)
        {
            data = Data;
            timestamps = Timestamps;
        }

        /// <summary>
        /// Returns a value indicating whether this instance has the same data and timestamps
        /// as a specified <see cref="TimestampedChunk{T}"/> structure.
        /// </summary>
        /// <param name="other">The <see cref="TimestampedChunk{T}"/> structure to compare to this instance.</param>
        /// <returns>
        /// <b>true</b> if <paramref name="other"/> has the same data and timestamps as this
        /// instance; otherwise, <b>false</b>.
        /// </returns>
        public bool Equals(TimestampedChunk<T> other)
        {
            return Timestamps == other.Timestamps && Data == other.Data;
        }

        /// <summary>
        /// Tests to see whether the specified object is a <see cref="TimestampedChunk{T}"/> structure
        /// with the same data and timestamps as this <see cref="TimestampedChunk{T}"/> structure.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to test.</param>
        /// <returns>
        /// <b>true</b> if <paramref name="obj"/> is a <see cref="TimestampedChunk{T}"/> and has the
        /// same data and timestamps as this <see cref="TimestampedChunk{T}"/>; otherwise, <b>false</b>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is TimestampedChunk<T> timestamped && Equals(timestamped);
        }

        /// <summary>
        /// Returns a hash code for this <see cref="TimestampedChunk{T}"/> structure.
        /// </summary>
        /// <returns>An integer value that specifies a hash value for this <see cref="TimestampedChunk{T}"/> structure.</returns>
        public override int GetHashCode()
        {
            return Timestamps.GetHashCode() ^ Data.GetHashCode();
        }

        /// <summary>
        /// Creates a <see cref="string"/> representation of this <see cref="TimestampedChunk{T}"/>
        /// structure.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> containing the <see cref="Data"/> and <see cref="Timestamps"/>
        /// properties of this <see cref="TimestampedChunk{T}"/> structure.
        /// </returns>
        public override string ToString()
        {
            return $"{Data}@{Timestamps}";
        }

        /// <summary>
        /// Tests whether two <see cref="TimestampedChunk{T}"/> structures are equal.
        /// </summary>
        /// <param name="left">The <see cref="TimestampedChunk{T}"/> structure on the left of the equality operator.</param>
        /// <param name="right">The <see cref="TimestampedChunk{T}"/> structure on the right of the equality operator.</param>
        /// <returns>
        /// <b>true</b> if <paramref name="left"/> and <paramref name="right"/> have the same data and timestamps;
        /// otherwise, <b>false</b>.
        /// </returns>
        public static bool operator ==(TimestampedChunk<T> left, TimestampedChunk<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests whether two <see cref="TimestampedChunk{T}"/> structures are different.
        /// </summary>
        /// <param name="left">The <see cref="TimestampedChunk{T}"/> structure on the left of the inequality operator.</param>
        /// <param name="right">The <see cref="TimestampedChunk{T}"/> structure on the right of the inequality operator.</param>
        /// <returns>
        /// <b>true</b> if <paramref name="left"/> and <paramref name="right"/> differ either in data or timestamps;
        /// <b>false</b> if <paramref name="left"/> and <paramref name="right"/> are equal.
        /// </returns>
        public static bool operator !=(TimestampedChunk<T> left, TimestampedChunk<T> right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Provides static methods for creating timestamped chunk objects.
    /// </summary>
    public static class TimestampedChunk
    {
        /// <summary>
        /// Creates a new timestamped chunk value.
        /// </summary>
        /// <typeparam name="T">The type of the data samples in the timestamped payload.</typeparam>
        /// <param name="data">The data samples of the timestamped chunk.</param>
        /// <param name="seconds">The timestamps for each data sample, in fractional seconds.</param>
        /// <returns>
        /// A new instance of the <see cref="TimestampedChunk{T}"/> class with the specified
        /// array of samples and corresponding timestamps.
        /// </returns>
        public static TimestampedChunk<T> Create<T>(T[,] data, double[] seconds)
        {
            return new TimestampedChunk<T>(data, seconds);
        }
    }
}
