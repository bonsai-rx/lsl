using Bonsai.Expressions;
using Bonsai.Lsl.Native;
using OpenCV.Net;
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

        /// <summary>
        /// Gets or sets a value specifying the postprocessing time synchronization options to use on the LSL stream.
        /// </summary>
        [Description("Specifies the postprocessing time synchronization options to use on the LSL stream.")]
        public ProcessingOptions ProcessingOptions { get; set; } = ProcessingOptions.All;

        /// <inheritdoc/>
        public override Expression Build(IEnumerable<Expression> arguments)
        {
            var chunkSize = ChunkSize;
            var channelCount = ChannelCount;
            var combinator = Expression.Constant(this);
            if (chunkSize.HasValue)
            {
                var chunkReader = GetChunkReader(ChannelFormat, out Depth channelDepth);
                return Expression.Call(
                    combinator,
                    nameof(Generate),
                    null,
                    Expression.Constant(chunkSize.Value),
                    Expression.Constant(channelCount),
                    Expression.Constant(channelDepth),
                    chunkReader);
            }
            else
            {
                var sampleReader = GetSampleReader(ChannelFormat);
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

        static LambdaExpression GetSampleReader(ChannelFormat format)
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

        delegate uint lsl_pull_chunk(
            IntPtr obj,
            IntPtr data,
            IntPtr timestamps,
            uint dataLength,
            uint timestampLength,
            double timeout,
            ref int ec);

        static int PullChunk(Native.StreamInlet inlet, Mat buffer, Mat timestamps, double timeout, lsl_pull_chunk pull_chunk)
        {
            int ec = 0;
            var res = pull_chunk(
                inlet.obj,
                buffer.Data,
                timestamps.Data,
                (uint)(buffer.Rows * buffer.Cols),
                (uint)timestamps.Cols,
                timeout,
                ref ec);
            LSL.check_error(ec);
            return (int)res / buffer.Cols;
        }

        static LambdaExpression GetChunkReader(ChannelFormat channelFormat, out Depth channelDepth)
        {
            switch (channelFormat)
            {
                case ChannelFormat.Float32:
                    Expression<Func<Native.StreamInlet, Mat, Mat, int>> floatReader;
                    floatReader = (inlet, chunk, timestamp) => PullChunk(inlet, chunk, timestamp, LSL.FOREVER, dll.lsl_pull_chunk_f);
                    channelDepth = Depth.F32;
                    return floatReader;
                case ChannelFormat.Double64:
                    Expression<Func<Native.StreamInlet, Mat, Mat, int>> doubleReader;
                    doubleReader = (inlet, chunk, timestamp) => PullChunk(inlet, chunk, timestamp, LSL.FOREVER, dll.lsl_pull_chunk_d);
                    channelDepth = Depth.F64;
                    return doubleReader;
                case ChannelFormat.Int32:
                    Expression<Func<Native.StreamInlet, Mat, Mat, int>> intReader;
                    intReader = (inlet, chunk, timestamp) => PullChunk(inlet, chunk, timestamp, LSL.FOREVER, dll.lsl_pull_chunk_i);
                    channelDepth = Depth.S32;
                    return intReader;
                case ChannelFormat.Int16:
                    Expression<Func<Native.StreamInlet, Mat, Mat, int>> shortReader;
                    shortReader = (inlet, chunk, timestamp) => PullChunk(inlet, chunk, timestamp, LSL.FOREVER, dll.lsl_pull_chunk_s);
                    channelDepth = Depth.S16;
                    return shortReader;
                case ChannelFormat.Int8:
                    Expression<Func<Native.StreamInlet, Mat, Mat, int>> byteReader;
                    byteReader = (inlet, chunk, timestamp) => PullChunk(inlet, chunk, timestamp, LSL.FOREVER, dll.lsl_pull_chunk_c);
                    channelDepth = Depth.U8;
                    return byteReader;
                case ChannelFormat.Int64:
                case ChannelFormat.String:
                case ChannelFormat.Undefined:
                default:
                    throw new ArgumentException("Unsupported chunk channel format.", nameof(channelFormat));
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

        static Native.StreamInlet CreateInlet(string name, string type, ProcessingOptions processingFlags)
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
            return new Native.StreamInlet(streamInfo, postproc_flags: (processing_options_t)processingFlags);
        }

        IObservable<Timestamped<TResult>> Generate<TResult>(Func<Native.StreamInlet, TResult[], double> pull_sample)
        {
            var name = Name;
            var contentType = ContentType;
            return Observable.Create<Timestamped<TResult>>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    using var streamInlet = CreateInlet(name, contentType, ProcessingOptions);
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
                    using var streamInlet = CreateInlet(name, contentType, ProcessingOptions);
                    streamInlet.open_stream();
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var sampleArray = new TResult[channelCount];
                            var sampleTime = pull_sample(streamInlet, sampleArray);
                            observer.OnNext(TimestampedSample.Create(sampleArray, sampleTime));
                        }
                    }
                    finally { streamInlet.close_stream(); }
                });
            });
        }

        IObservable<TimestampedChunkBuffer> Generate(
            int chunkSize,
            int channelCount,
            Depth sampleDepth,
            Func<Native.StreamInlet, Mat, Mat, int> pull_sample)
        {
            var name = Name;
            var contentType = ContentType;
            return Observable.Create<TimestampedChunkBuffer>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    using var streamInlet = CreateInlet(name, contentType, ProcessingOptions);
                    using var buffer = new Mat(chunkSize, channelCount, sampleDepth, 1);
                    streamInlet.open_stream();
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var timestamps = new Mat(1, chunkSize, Depth.F64, 1);
                            var samples = pull_sample(streamInlet, buffer, timestamps);
                            var data = new Mat(channelCount, chunkSize, sampleDepth, 1);
                            CV.Transpose(buffer, data);
                            observer.OnNext(TimestampedChunk.Create(data, timestamps));
                        }
                    }
                    finally { streamInlet.close_stream(); }
                });
            });
        }
    }

    /// <summary>
    /// Specifies options for post-processing of samples for an LSL stream inlet.
    /// </summary>
    [Flags]
    public enum ProcessingOptions
    {
        /// <summary>
        /// No automatic post-processing. Provides ground truth timestamps for manual post-processing.
        /// </summary>
        None = processing_options_t.proc_none,

        /// <summary>
        /// Perform automatic clock synchronization.
        /// </summary>
        Clocksync = processing_options_t.proc_clocksync,

        /// <summary>
        /// Remove random jitter from timestamps using a trend-adjusted smoothing algorithm.
        /// </summary>
        Dejitter = processing_options_t.proc_dejitter,

        /// <summary>
        /// Force timestamps to be monotonically ascending.
        /// </summary>
        Monotonize = processing_options_t.proc_monotonize,

        /// <summary>
        /// Post-processing is thread-safe (same inlet can be read from by multiple threads) at the cost of more CPU usage.
        /// </summary>
        Threadsafe = processing_options_t.proc_threadsafe,

        /// <summary>
        /// Use all available postprocessing options.
        /// </summary>
        All = processing_options_t.proc_ALL
    }
}
