using Bonsai.Expressions;
using Bonsai.Lsl.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Bonsai.Lsl
{
    /// <summary>
    /// Represents an operator that pulls data buffers from the specified LSL stream
    /// into an observable sequence.
    /// </summary>
    [Combinator]
    [DefaultProperty(nameof(Name))]
    [WorkflowElementCategory(ElementCategory.Source)]
    [Description("Pulls data buffers from the specified LSL stream into an observable sequence.")]
    public class StreamInlet : ZeroArgumentExpressionBuilder, INamedElement
    {
        /// <summary>
        /// Gets or sets the name of the LSL stream from which to pull data.
        /// If no name is specified, the first stream with the specified
        /// content type will be used.
        /// </summary>
        [Description("The name of the LSL stream from which to pull data.")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the content type of the LSL stream.
        /// If no content type is specified, the first stream of any type with
        /// the specified name will be used.
        /// </summary>
        [Description("Specifies the content type of the LSL stream.")]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the number of channels in the LSL stream.
        /// </summary>
        [Description("Specifies the number of channels in the LSL stream.")]
        public int ChannelCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value specifying the number of samples in each chunk.
        /// If no value is specified, the output will be a single sample for each channel.
        /// </summary>
        [Description("Specifies the number of samples in each chunk.")]
        public int? ChunkSize { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the data format of each channel in the
        /// LSL stream.
        /// </summary>
        [Description("Specifies the data format of each channel in the LSL stream.")]
        public ChannelFormat ChannelFormat { get; set; } = ChannelFormat.Float32;

        /// <inheritdoc/>
        public override Expression Build(IEnumerable<Expression> arguments)
        {
            var chunkSize = ChunkSize;
            var channelCount = ChannelCount;
            var combinator = Expression.Constant(this);
            if (chunkSize.HasValue)
            {
                var chunkReader = GetChannelChunkReader(ChannelFormat);
                var sampleType = new[] { chunkReader.Parameters[1].Type.GetElementType() };
                return Expression.Call(
                    combinator,
                    nameof(Generate),
                    sampleType,
                    Expression.Constant(chunkSize.Value),
                    Expression.Constant(channelCount),
                    chunkReader);
            }
            else
            {
                var sampleReader = GetChannelSampleReader(ChannelFormat);
                var sampleType = new[] { sampleReader.Parameters[1].Type.GetElementType() };
                if (channelCount == 1)
                {
                    return Expression.Call(
                        combinator,
                        nameof(Generate),
                        sampleType,
                        sampleReader);
                }

                return Expression.Call(
                    combinator,
                    nameof(Generate),
                    sampleType,
                    Expression.Constant(channelCount),
                    sampleReader);
            }
        }

        static LambdaExpression GetChannelSampleReader(ChannelFormat format)
        {
            switch (format)
            {
                case ChannelFormat.Float32:
                    Expression<Func<Native.StreamInlet, float[], double>> floatReader;
                    floatReader = (inlet, data) => inlet.pull_sample(data, LSL.FOREVER);
                    return floatReader;
                case ChannelFormat.Double64:
                    Expression<Func<Native.StreamInlet, double[], double>> doubleReader;
                    doubleReader = (inlet, data) => inlet.pull_sample(data, LSL.FOREVER);
                    return doubleReader;
                case ChannelFormat.String:
                    Expression<Func<Native.StreamInlet, string[], double>> stringReader;
                    stringReader = (inlet, data) => inlet.pull_sample(data, LSL.FOREVER);
                    return stringReader;
                case ChannelFormat.Int32:
                    Expression<Func<Native.StreamInlet, int[], double>> intReader;
                    intReader = (inlet, data) => inlet.pull_sample(data, LSL.FOREVER);
                    return intReader;
                case ChannelFormat.Int16:
                    Expression<Func<Native.StreamInlet, short[], double>> shortReader;
                    shortReader = (inlet, data) => inlet.pull_sample(data, LSL.FOREVER);
                    return shortReader;
                case ChannelFormat.Int8:
                    Expression<Func<Native.StreamInlet, byte[], double>> byteReader;
                    byteReader = (inlet, data) => inlet.pull_sample(data, LSL.FOREVER);
                    return byteReader;
                case ChannelFormat.Int64:
                case ChannelFormat.Undefined:
                default:
                    throw new ArgumentException("Unsupported channel format.", nameof(format));
            }
        }

        static LambdaExpression GetChannelChunkReader(ChannelFormat format)
        {
            switch (format)
            {
                case ChannelFormat.Float32:
                    Expression<Func<Native.StreamInlet, float[,], double[], int>> floatReader;
                    floatReader = (inlet, data, timestamp) => inlet.pull_chunk(data, timestamp, LSL.FOREVER);
                    return floatReader;
                case ChannelFormat.Double64:
                    Expression<Func<Native.StreamInlet, double[,], double[], int>> doubleReader;
                    doubleReader = (inlet, data, timestamp) => inlet.pull_chunk(data, timestamp, LSL.FOREVER);
                    return doubleReader;
                case ChannelFormat.String:
                    Expression<Func<Native.StreamInlet, string[,], double[], int>> stringReader;
                    stringReader = (inlet, data, timestamp) => inlet.pull_chunk(data, timestamp, LSL.FOREVER);
                    return stringReader;
                case ChannelFormat.Int32:
                    Expression<Func<Native.StreamInlet, int[,], double[], int>> intReader;
                    intReader = (inlet, data, timestamp) => inlet.pull_chunk(data, timestamp, LSL.FOREVER);
                    return intReader;
                case ChannelFormat.Int16:
                    Expression<Func<Native.StreamInlet, short[,], double[], int>> shortReader;
                    shortReader = (inlet, data, timestamp) => inlet.pull_chunk(data, timestamp, LSL.FOREVER);
                    return shortReader;
                case ChannelFormat.Int8:
                    Expression<Func<Native.StreamInlet, byte[,], double[], int>> byteReader;
                    byteReader = (inlet, data, timestamp) => inlet.pull_chunk(data, timestamp, LSL.FOREVER);
                    return byteReader;
                case ChannelFormat.Int64:
                case ChannelFormat.Undefined:
                default:
                    throw new ArgumentException("Unsupported channel format.", nameof(format));
            }
        }

        static StreamInfo ResolveStream(string prop, string value)
        {
            var streamInfo = LSL.resolve_stream(prop, value).FirstOrDefault();
            if (streamInfo == null)
            {
                throw new InvalidOperationException($"No LSL stream matching the specified {prop} was found.");
            }

            return streamInfo;
        }

        static StreamInfo ResolveStream(string pred)
        {
            var streamInfo = LSL.resolve_stream(pred).FirstOrDefault();
            if (streamInfo == null)
            {
                throw new InvalidOperationException($"No LSL stream matching the specified name or type was found.");
            }

            return streamInfo;
        }

        static Native.StreamInlet CreateInlet(string name, string type)
        {
            StreamInfo streamInfo;
            if (name is null && type is null)
            {
                throw new InvalidOperationException("A valid LSL stream name or type must be specified.");
            }
            else if (name is null)
            {
                streamInfo = ResolveStream("type", type);
            }
            else if (type is null)
            {
                streamInfo = ResolveStream("name", name);
            }
            else streamInfo = ResolveStream($"name='{name}' and type='{type}'");
            return new Native.StreamInlet(streamInfo);
        }

        IObservable<Timestamped<TResult>> Generate<TResult>(Func<Native.StreamInlet, TResult[], double> pull_sample)
        {
            var name = Name;
            var contentType = ContentType;
            return Observable.Create<Timestamped<TResult>>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    using var streamInlet = CreateInlet(name, contentType);
                    var sampleArray = new TResult[1];
                    streamInlet.open_stream();
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var sampleTime = pull_sample(streamInlet, sampleArray);
                            observer.OnNext(Timestamped.Create(sampleArray[0], sampleTime));
                        }
                    }
                    finally { streamInlet.close_stream(); }
                });
            });
        }

        IObservable<TimestampedSample<TResult>> Generate<TResult>(
            int channelCount,
            Func<Native.StreamInlet, TResult[], double> pull_sample)
        {
            var name = Name;
            var contentType = ContentType;
            return Observable.Create<TimestampedSample<TResult>>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    using var streamInlet = CreateInlet(name, contentType);
                    var sampleArray = new TResult[channelCount];
                    streamInlet.open_stream();
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var sampleTime = pull_sample(streamInlet, sampleArray);
                            observer.OnNext(TimestampedSample.Create(sampleArray, sampleTime));
                        }
                    }
                    finally { streamInlet.close_stream(); }
                });
            });
        }

        IObservable<TimestampedChunk<TResult>> Generate<TResult>(
            int chunkSize,
            int channelCount,
            Func<Native.StreamInlet, TResult[,], double[], int> pull_sample)
        {
            var name = Name;
            var contentType = ContentType;
            return Observable.Create<TimestampedChunk<TResult>>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    using var streamInlet = CreateInlet(name, contentType);
                    var data = new TResult[chunkSize, channelCount];
                    var timestamps = new double[chunkSize];
                    streamInlet.open_stream();
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var samples = pull_sample(streamInlet, data, timestamps);
                            observer.OnNext(TimestampedChunk.Create(data, timestamps));
                        }
                    }
                    finally { streamInlet.close_stream(); }
                });
            });
        }
    }
}
