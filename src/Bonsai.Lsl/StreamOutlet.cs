using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using Bonsai.Expressions;
using Bonsai.Lsl.Native;
using OpenCV.Net;

namespace Bonsai.Lsl
{
    /// <summary>
    /// Represents an operator that pushes data buffers from an observable sequence
    /// into the specified LSL stream.
    /// </summary>
    [Combinator]
    [DefaultProperty(nameof(Name))]
    [WorkflowElementCategory(ElementCategory.Sink)]
    [Description("Pushes a sequence of data buffers into the specified LSL stream.")]
    public class StreamOutlet : SingleArgumentExpressionBuilder, INamedElement
    {
        /// <summary>
        /// Gets or sets the name of the LSL stream to which data will be pushed.
        /// </summary>
        [Description("The name of the LSL stream to which data will be pushed.")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the content type of the LSL stream.
        /// If no content type is specified, the simple name of the input type
        /// will be used.
        /// </summary>
        [Description("Specifies the content type of the LSL stream.")]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the number of channels in the LSL stream.
        /// </summary>
        [Description("Specifies the number of channels in the LSL stream.")]
        public int ChannelCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the inner properties that will be selected when writing each
        /// element of the sequence.
        /// </summary>
        [Description("The inner properties that will be selected when writing each element of the sequence.")]
        [Editor("Bonsai.Design.MultiMemberSelectorEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        public string Selector { get; set; }

        /// <inheritdoc/>
        public override Expression Build(IEnumerable<Expression> arguments)
        {
            var selector = Selector;
            var channelCount = ChannelCount;
            var source = arguments.First();
            var parameterTypes = source.Type.GetGenericArguments();
            var combinator = Expression.Constant(this);
            if (!string.IsNullOrEmpty(selector))
            {
                var selectorSource = parameterTypes[0];
                if (selectorSource.IsGenericType)
                {
                    var sourceTypeDefinition = selectorSource.GetGenericTypeDefinition();
                    if (sourceTypeDefinition == typeof(Timestamped<>) ||
                        sourceTypeDefinition == typeof(TimestampedSample<>) ||
                        sourceTypeDefinition == typeof(TimestampedChunk<>))
                    {
                        parameterTypes = selectorSource.GetGenericArguments();
                    }
                }

                var parameter = Expression.Parameter(parameterTypes[0]);
                var selectedMembers = ExpressionHelper.SelectMembers(parameter, selector).ToArray();
                if (selectedMembers.Length != channelCount)
                {
                    throw new InvalidOperationException("The number of selected members must match the total number of channels.");
                }

                Type channelType = null;
                for (int i = 0; i < selectedMembers.Length; i++)
                {
                    if (channelType == null)
                    {
                        channelType = selectedMembers[i].Type;
                        if (!channelType.IsPrimitive)
                        {
                            throw new InvalidOperationException("All selected members must be primitive types.");
                        }
                    }
                    else if (channelType != selectedMembers[i].Type)
                    {
                        throw new InvalidOperationException("All selected members must have the same type");
                    }
                }

                Expression selectorBody;
                if (selectedMembers.Length == 1)
                {
                    selectorBody = selectedMembers[0];
                }
                else selectorBody = Expression.NewArrayInit(channelType, selectedMembers);
                var selectorLambda = Expression.Lambda(selectorBody, parameter);
                return Expression.Call(combinator, nameof(Process), parameterTypes, source, selectorLambda);
            }

            return Expression.Call(combinator, nameof(Process), null, source);
        }

        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            channel_format_t channelFormat,
            Action<TSource, Native.StreamOutlet> writer)
        {
            var name = Name;
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException("A valid LSL stream name must be specified.");
            }

            var contentType = ContentType ?? typeof(TSource).Name;
            var channelCount = ChannelCount;
            if (channelCount <= 0)
            {
                throw new InvalidOperationException("The number of channels must be a positive integer.");
            }

            return Observable.Using(
                () =>
                {
                    var streamInfo = new StreamInfo(name, contentType, channelCount, LSL.IRREGULAR_RATE, channelFormat);
                    return new Native.StreamOutlet(streamInfo);
                },
                outlet => source.Do(value => writer(value, outlet)));
        }

        #region Byte

        /// <summary>
        /// Pushes an observable sequence of unsigned 8-bit integers into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of unsigned 8-bit integers to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the values to the LSL stream.
        /// </returns>
        public IObservable<byte> Process(IObservable<byte> source)
            => Process(source, channel_format_t.cf_int8, (value, outlet) => outlet.push_sample(new[] { value }));

        /// <summary>
        /// Pushes an observable sequence of unsigned 8-bit samples into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of unsigned 8-bit samples to push to the LSL stream. Each array
        /// must have the same number of values as the total number of channels in the
        /// stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the samples to the LSL stream.
        /// </returns>
        public IObservable<byte[]> Process(IObservable<byte[]> source)
            => Process(source, channel_format_t.cf_int8, (data, outlet) => outlet.push_sample(data));

        /// <summary>
        /// Pushes an observable sequence of unsigned 8-bit chunks into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of unsigned 8-bit chunks to push to the LSL stream. Each array
        /// must have the same number of columns as the total number of channels in the
        /// stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the chunks to the LSL stream.
        /// </returns>
        public IObservable<byte[,]> Process(IObservable<byte[,]> source)
            => Process(source, channel_format_t.cf_int8, (data, outlet) => outlet.push_chunk(data));

        /// <summary>
        /// Pushes an observable sequence of timestamped unsigned 8-bit integers into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped unsigned 8-bit integers to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped values to the
        /// LSL stream.
        /// </returns>
        public IObservable<Timestamped<byte>> Process(IObservable<Timestamped<byte>> source)
            => Process(source, channel_format_t.cf_int8,
                (sample, outlet) => outlet.push_sample(new[] { sample.Value }, sample.Timestamp));

        /// <summary>
        /// Pushes an observable sequence of timestamped unsigned 8-bit samples into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped unsigned 8-bit samples to push to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped samples to the
        /// LSL stream.
        /// </returns>
        public IObservable<TimestampedSample<byte>> Process(IObservable<TimestampedSample<byte>> source)
            => Process(source, channel_format_t.cf_int8,
                (sample, outlet) => outlet.push_sample(sample.Data, sample.Timestamp));

        /// <summary>
        /// Pushes an observable sequence of timestamped unsigned 8-bit chunks into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped unsigned 8-bit chunks to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped chunks to the
        /// LSL stream.
        /// </returns>
        public IObservable<TimestampedChunk<byte>> Process(IObservable<TimestampedChunk<byte>> source)
            => Process(source, channel_format_t.cf_int8, (sample, outlet) => outlet.push_chunk(sample.Data, sample.Timestamps));

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each element into an unsigned 8-bit integer.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The sequence of elements to push to the LSL stream.</param>
        /// <param name="selector">
        /// The transform function used to select which values to write to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the values to the LSL stream.
        /// </returns>
        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, byte> selector)
        {
            return Process(source, channel_format_t.cf_int8,
                (value, outlet) => outlet.push_sample(new[] { selector(value) }));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each element into unsigned 8-bit samples.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The sequence of elements to push to the LSL stream.</param>
        /// <param name="selector">
        /// The transform function used to select which samples to write to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the samples to the LSL stream.
        /// </returns>
        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, byte[]> selector)
        {
            return Process(source, channel_format_t.cf_int8,
                (value, outlet) => outlet.push_sample(selector(value)));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each timestamped element into an unsigned 8-bit integer.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">
        /// The sequence of timestamped elements to push to the LSL stream.
        /// </param>
        /// <param name="selector">
        /// The transform function used to select which values to write to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped values to the
        /// LSL stream.
        /// </returns>
        IObservable<Timestamped<TSource>> Process<TSource>(
            IObservable<Timestamped<TSource>> source,
            Func<TSource, byte> selector)
        {
            return Process(source, channel_format_t.cf_int8,
                (sample, outlet) => outlet.push_sample(new[] { selector(sample.Value) }, sample.Timestamp));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each timestamped element into unsigned 8-bit samples.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">
        /// The sequence of timestamped elements to push to the LSL stream.
        /// </param>
        /// <param name="selector">
        /// The transform function used to select which samples to write to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped samples to the
        /// LSL stream.
        /// </returns>
        IObservable<Timestamped<TSource>> Process<TSource>(
            IObservable<Timestamped<TSource>> source,
            Func<TSource, byte[]> selector)
        {
            return Process(source, channel_format_t.cf_int8,
                (sample, outlet) => outlet.push_sample(selector(sample.Value), sample.Timestamp));
        }

        #endregion

        #region Int16

        /// <summary>
        /// Pushes an observable sequence of signed 16-bit integers into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of signed 16-bit integers to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the values to the LSL stream.
        /// </returns>
        public IObservable<short> Process(IObservable<short> source)
            => Process(source, channel_format_t.cf_int16, (value, outlet) => outlet.push_sample(new[] { value }));

        /// <summary>
        /// Pushes an observable sequence of signed 16-bit samples into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of signed 16-bit samples to push to the LSL stream. Each array
        /// must have the same number of values as the total number of channels in the
        /// stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the samples to the LSL stream.
        /// </returns>
        public IObservable<short[]> Process(IObservable<short[]> source)
            => Process(source, channel_format_t.cf_int16, (data, outlet) => outlet.push_sample(data));

        /// <summary>
        /// Pushes an observable sequence of signed 16-bit chunks into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of signed 16-bit chunks to push to the LSL stream. Each array
        /// must have the same number of columns as the total number of channels in the
        /// stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the chunks to the LSL stream.
        /// </returns>
        public IObservable<short[,]> Process(IObservable<short[,]> source)
            => Process(source, channel_format_t.cf_int16, (data, outlet) => outlet.push_chunk(data));

        /// <summary>
        /// Pushes an observable sequence of timestamped signed 16-bit integers into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped signed 16-bit integers to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped values to the
        /// LSL stream.
        /// </returns>
        public IObservable<Timestamped<short>> Process(IObservable<Timestamped<short>> source)
            => Process(source, channel_format_t.cf_int16,
                (sample, outlet) => outlet.push_sample(new[] { sample.Value }, sample.Timestamp));

        /// <summary>
        /// Pushes an observable sequence of timestamped signed 16-bit samples into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped signed 16-bit samples to push to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped samples to the
        /// LSL stream.
        /// </returns>
        public IObservable<TimestampedSample<short>> Process(IObservable<TimestampedSample<short>> source)
            => Process(source, channel_format_t.cf_int16,
                (sample, outlet) => outlet.push_sample(sample.Data, sample.Timestamp));

        /// <summary>
        /// Pushes an observable sequence of timestamped signed 16-bit chunks into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped signed 16-bit chunks to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped chunks to the
        /// LSL stream.
        /// </returns>
        public IObservable<TimestampedChunk<short>> Process(IObservable<TimestampedChunk<short>> source)
            => Process(source, channel_format_t.cf_int16, (sample, outlet) => outlet.push_chunk(sample.Data, sample.Timestamps));

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each element into a signed 16-bit integer.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The sequence of elements to push to the LSL stream.</param>
        /// <param name="selector">
        /// The transform function used to select which values to write to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the values to the LSL stream.
        /// </returns>
        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, short> selector)
        {
            return Process(source, channel_format_t.cf_int16,
                (value, outlet) => outlet.push_sample(new[] { selector(value) }));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each element into signed 16-bit samples.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The sequence of elements to push to the LSL stream.</param>
        /// <param name="selector">
        /// The transform function used to select which samples to write to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the samples to the LSL stream.
        /// </returns>
        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, short[]> selector)
        {
            return Process(source, channel_format_t.cf_int16,
                (value, outlet) => outlet.push_sample(selector(value)));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each timestamped element into a signed 16-bit integer.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">
        /// The sequence of timestamped elements to push to the LSL stream.
        /// </param>
        /// <param name="selector">
        /// The transform function used to select which values to write to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped values to the
        /// LSL stream.
        /// </returns>
        IObservable<Timestamped<TSource>> Process<TSource>(
            IObservable<Timestamped<TSource>> source,
            Func<TSource, short> selector)
        {
            return Process(source, channel_format_t.cf_int16,
                (sample, outlet) => outlet.push_sample(new[] { selector(sample.Value) }, sample.Timestamp));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each timestamped element into signed 16-bit samples.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">
        /// The sequence of timestamped elements to push to the LSL stream.
        /// </param>
        /// <param name="selector">
        /// The transform function used to select which samples to write to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped samples to the
        /// LSL stream.
        /// </returns>
        IObservable<Timestamped<TSource>> Process<TSource>(
            IObservable<Timestamped<TSource>> source,
            Func<TSource, short[]> selector)
        {
            return Process(source, channel_format_t.cf_int16,
                (sample, outlet) => outlet.push_sample(selector(sample.Value), sample.Timestamp));
        }

        #endregion

        #region Int32

        /// <summary>
        /// Pushes an observable sequence of signed 32-bit integers into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of signed 32-bit integers to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the values to the LSL stream.
        /// </returns>
        public IObservable<int> Process(IObservable<int> source)
            => Process(source, channel_format_t.cf_int32, (value, outlet) => outlet.push_sample(new[] { value }));

        /// <summary>
        /// Pushes an observable sequence of signed 32-bit samples into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of signed 32-bit samples to push to the LSL stream. Each array
        /// must have the same number of values as the total number of channels in the
        /// stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the samples to the LSL stream.
        /// </returns>
        public IObservable<int[]> Process(IObservable<int[]> source)
            => Process(source, channel_format_t.cf_int32, (data, outlet) => outlet.push_sample(data));

        /// <summary>
        /// Pushes an observable sequence of signed 32-bit chunks into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of signed 32-bit chunks to push to the LSL stream. Each array
        /// must have the same number of columns as the total number of channels in the
        /// stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the chunks to the LSL stream.
        /// </returns>
        public IObservable<int[,]> Process(IObservable<int[,]> source)
            => Process(source, channel_format_t.cf_int32, (data, outlet) => outlet.push_chunk(data));

        /// <summary>
        /// Pushes an observable sequence of timestamped signed 32-bit integers into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped signed 32-bit integers to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped values to the
        /// LSL stream.
        /// </returns>
        public IObservable<Timestamped<int>> Process(IObservable<Timestamped<int>> source)
            => Process(source, channel_format_t.cf_int32,
                (sample, outlet) => outlet.push_sample(new[] { sample.Value }, sample.Timestamp));

        /// <summary>
        /// Pushes an observable sequence of timestamped signed 32-bit samples into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped signed 32-bit samples to push to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped samples to the
        /// LSL stream.
        /// </returns>
        public IObservable<TimestampedSample<int>> Process(IObservable<TimestampedSample<int>> source)
            => Process(source, channel_format_t.cf_int32,
                (sample, outlet) => outlet.push_sample(sample.Data, sample.Timestamp));

        /// <summary>
        /// Pushes an observable sequence of timestamped signed 32-bit chunks into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped signed 32-bit chunks to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped chunks to the
        /// LSL stream.
        /// </returns>
        public IObservable<TimestampedChunk<int>> Process(IObservable<TimestampedChunk<int>> source)
            => Process(source, channel_format_t.cf_int32, (sample, outlet) => outlet.push_chunk(sample.Data, sample.Timestamps));

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each element into a signed 32-bit integer.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The sequence of elements to push to the LSL stream.</param>
        /// <param name="selector">
        /// The transform function used to select which values to write to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the values to the LSL stream.
        /// </returns>
        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, int> selector)
        {
            return Process(source, channel_format_t.cf_int32,
                (value, outlet) => outlet.push_sample(new[] { selector(value) }));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each element into signed 32-bit samples.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The sequence of elements to push to the LSL stream.</param>
        /// <param name="selector">
        /// The transform function used to select which samples to write to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the samples to the LSL stream.
        /// </returns>
        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, int[]> selector)
        {
            return Process(source, channel_format_t.cf_int32,
                (value, outlet) => outlet.push_sample(selector(value)));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each timestamped element into a signed 32-bit integer.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">
        /// The sequence of timestamped elements to push to the LSL stream.
        /// </param>
        /// <param name="selector">
        /// The transform function used to select which values to write to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped values to the
        /// LSL stream.
        /// </returns>
        IObservable<Timestamped<TSource>> Process<TSource>(
            IObservable<Timestamped<TSource>> source,
            Func<TSource, int> selector)
        {
            return Process(source, channel_format_t.cf_int32,
                (sample, outlet) => outlet.push_sample(new[] { selector(sample.Value) }, sample.Timestamp));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each timestamped element into signed 32-bit samples.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">
        /// The sequence of timestamped elements to push to the LSL stream.
        /// </param>
        /// <param name="selector">
        /// The transform function used to select which samples to write to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped samples to the
        /// LSL stream.
        /// </returns>
        IObservable<Timestamped<TSource>> Process<TSource>(
            IObservable<Timestamped<TSource>> source,
            Func<TSource, int[]> selector)
        {
            return Process(source, channel_format_t.cf_int32,
                (sample, outlet) => outlet.push_sample(selector(sample.Value), sample.Timestamp));
        }

        #endregion

        #region Single

        /// <summary>
        /// Pushes an observable sequence of 32-bit floating-point values into the
        /// specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of 32-bit floating-point numbers to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the values to the LSL stream.
        /// </returns>
        public IObservable<float> Process(IObservable<float> source)
            => Process(source, channel_format_t.cf_float32, (value, outlet) => outlet.push_sample(new[] { value }));

        /// <summary>
        /// Pushes an observable sequence of 32-bit floating-point samples into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of 32-bit floating-point samples to push to the LSL stream. Each
        /// array must have the same number of values as the total number of channels in
        /// the stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the samples to the LSL stream.
        /// </returns>
        public IObservable<float[]> Process(IObservable<float[]> source)
            => Process(source, channel_format_t.cf_float32, (data, outlet) => outlet.push_sample(data));

        /// <summary>
        /// Pushes an observable sequence of 32-bit floating-point chunks into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of 32-bit floating-point chunks to push to the LSL stream. Each array
        /// must have the same number of columns as the total number of channels in the
        /// stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the chunks to the LSL stream.
        /// </returns>
        public IObservable<float[,]> Process(IObservable<float[,]> source)
            => Process(source, channel_format_t.cf_float32, (data, outlet) => outlet.push_chunk(data));

        /// <summary>
        /// Pushes an observable sequence of timestamped 32-bit floating-point values
        /// into the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped 32-bit floating-point values to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped values to the
        /// LSL stream.
        /// </returns>
        public IObservable<Timestamped<float>> Process(IObservable<Timestamped<float>> source)
            => Process(source, channel_format_t.cf_float32,
                (sample, outlet) => outlet.push_sample(new[] { sample.Value }, sample.Timestamp));

        /// <summary>
        /// Pushes an observable sequence of timestamped 32-bit floating-point samples into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped 32-bit floating-point samples to push to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped samples to the
        /// LSL stream.
        /// </returns>
        public IObservable<TimestampedSample<float>> Process(IObservable<TimestampedSample<float>> source)
            => Process(source, channel_format_t.cf_float32,
                (sample, outlet) => outlet.push_sample(sample.Data, sample.Timestamp));

        /// <summary>
        /// Pushes an observable sequence of timestamped 32-bit floating-point chunks into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped 32-bit floating-point chunks to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped chunks to the
        /// LSL stream.
        /// </returns>
        public IObservable<TimestampedChunk<float>> Process(IObservable<TimestampedChunk<float>> source)
            => Process(source, channel_format_t.cf_float32, (sample, outlet) => outlet.push_chunk(sample.Data, sample.Timestamps));

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each element into a 32-bit floating-point value.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The sequence of elements to push to the LSL stream.</param>
        /// <param name="selector">
        /// The transform function used to select which values to write to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the values to the LSL stream.
        /// </returns>
        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, float> selector)
        {
            return Process(source, channel_format_t.cf_float32,
                (value, outlet) => outlet.push_sample(new[] { selector(value) }));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each element into 32-bit floating-point samples.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The sequence of elements to push to the LSL stream.</param>
        /// <param name="selector">
        /// The transform function used to select which samples to write to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the samples to the LSL stream.
        /// </returns>
        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, float[]> selector)
        {
            return Process(source, channel_format_t.cf_float32,
                (value, outlet) => outlet.push_sample(selector(value)));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each timestamped element into a 32-bit floating-point value.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">
        /// The sequence of timestamped elements to push to the LSL stream.
        /// </param>
        /// <param name="selector">
        /// The transform function used to select which values to write to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped values to the
        /// LSL stream.
        /// </returns>
        IObservable<Timestamped<TSource>> Process<TSource>(
            IObservable<Timestamped<TSource>> source,
            Func<TSource, float> selector)
        {
            return Process(source, channel_format_t.cf_float32,
                (sample, outlet) => outlet.push_sample(new[] { selector(sample.Value) }, sample.Timestamp));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each timestamped element into 32-bit floating-point samples.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">
        /// The sequence of timestamped elements to push to the LSL stream.
        /// </param>
        /// <param name="selector">
        /// The transform function used to select which samples to write to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped samples to the
        /// LSL stream.
        /// </returns>
        IObservable<Timestamped<TSource>> Process<TSource>(
            IObservable<Timestamped<TSource>> source,
            Func<TSource, float[]> selector)
        {
            return Process(source, channel_format_t.cf_float32,
                (sample, outlet) => outlet.push_sample(selector(sample.Value), sample.Timestamp));
        }

        #endregion

        #region Double

        /// <summary>
        /// Pushes an observable sequence of 64-bit floating-point values into the
        /// specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of 64-bit floating-point values to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the values to the LSL stream.
        /// </returns>
        public IObservable<double> Process(IObservable<double> source)
            => Process(source, channel_format_t.cf_double64, (value, outlet) => outlet.push_sample(new[] { value }));

        /// <summary>
        /// Pushes an observable sequence of 64-bit floating-point samples into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of 64-bit floating-point samples to push to the LSL stream. Each
        /// array must have the same number of values as the total number of channels in
        /// the stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the samples to the LSL stream.
        /// </returns>
        public IObservable<double[]> Process(IObservable<double[]> source)
            => Process(source, channel_format_t.cf_double64, (data, outlet) => outlet.push_sample(data));

        /// <summary>
        /// Pushes an observable sequence of 64-bit floating-point chunks into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of 64-bit floating-point chunks to push to the LSL stream. Each array
        /// must have the same number of columns as the total number of channels in the
        /// stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the chunks to the LSL stream.
        /// </returns>
        public IObservable<double[,]> Process(IObservable<double[,]> source)
            => Process(source, channel_format_t.cf_double64, (data, outlet) => outlet.push_chunk(data));

        /// <summary>
        /// Pushes an observable sequence of timestamped 64-bit floating-point values
        /// into the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped 64-bit floating-point values to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped values to the
        /// LSL stream.
        /// </returns>
        public IObservable<Timestamped<double>> Process(IObservable<Timestamped<double>> source)
            => Process(source, channel_format_t.cf_double64,
                (sample, outlet) => outlet.push_sample(new[] { sample.Value }, sample.Timestamp));

        /// <summary>
        /// Pushes an observable sequence of timestamped 64-bit floating-point samples into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped 64-bit floating-point samples to push to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped samples to the
        /// LSL stream.
        /// </returns>
        public IObservable<TimestampedSample<double>> Process(IObservable<TimestampedSample<double>> source)
            => Process(source, channel_format_t.cf_double64,
                (sample, outlet) => outlet.push_sample(sample.Data, sample.Timestamp));

        /// <summary>
        /// Pushes an observable sequence of timestamped 64-bit floating-point chunks into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped 64-bit floating-point chunks to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped chunks to the
        /// LSL stream.
        /// </returns>
        public IObservable<TimestampedChunk<double>> Process(IObservable<TimestampedChunk<double>> source)
            => Process(source, channel_format_t.cf_double64, (sample, outlet) => outlet.push_chunk(sample.Data, sample.Timestamps));

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each element into a 64-bit floating-point value.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The sequence of elements to push to the LSL stream.</param>
        /// <param name="selector">
        /// The transform function used to select which values to write to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the values to the LSL stream.
        /// </returns>
        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, double> selector)
        {
            return Process(source, channel_format_t.cf_double64,
                (value, outlet) => outlet.push_sample(new[] { selector(value) }));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each element into 64-bit floating-point samples.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The sequence of elements to push to the LSL stream.</param>
        /// <param name="selector">
        /// The transform function used to select which samples to write to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the samples to the LSL stream.
        /// </returns>
        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, double[]> selector)
        {
            return Process(source, channel_format_t.cf_double64,
                (value, outlet) => outlet.push_sample(selector(value)));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each timestamped element into a 64-bit floating-point value.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">
        /// The sequence of timestamped elements to push to the LSL stream.
        /// </param>
        /// <param name="selector">
        /// The transform function used to select which values to write to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped values to the
        /// LSL stream.
        /// </returns>
        IObservable<Timestamped<TSource>> Process<TSource>(
            IObservable<Timestamped<TSource>> source,
            Func<TSource, double> selector)
        {
            return Process(source, channel_format_t.cf_double64,
                (sample, outlet) => outlet.push_sample(new[] { selector(sample.Value) }, sample.Timestamp));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each timestamped element into 64-bit floating-point samples.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">
        /// The sequence of timestamped elements to push to the LSL stream.
        /// </param>
        /// <param name="selector">
        /// The transform function used to select which samples to write to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped samples to the
        /// LSL stream.
        /// </returns>
        IObservable<Timestamped<TSource>> Process<TSource>(
            IObservable<Timestamped<TSource>> source,
            Func<TSource, double[]> selector)
        {
            return Process(source, channel_format_t.cf_double64,
                (sample, outlet) => outlet.push_sample(selector(sample.Value), sample.Timestamp));
        }

        #endregion

        #region String

        /// <summary>
        /// Pushes an observable sequence of variable length strings into the
        /// specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of variable length strings to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the values to the LSL stream.
        /// </returns>
        public IObservable<string> Process(IObservable<string> source)
            => Process(source, channel_format_t.cf_string, (value, outlet) => outlet.push_sample(new[] { value }));

        /// <summary>
        /// Pushes an observable sequence of variable length samples into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of variable length samples to push to the LSL stream. Each
        /// array must have the same number of values as the total number of channels in
        /// the stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the samples to the LSL stream.
        /// </returns>
        public IObservable<string[]> Process(IObservable<string[]> source)
            => Process(source, channel_format_t.cf_string, (data, outlet) => outlet.push_sample(data));

        /// <summary>
        /// Pushes an observable sequence of variable length chunks into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of variable length chunks to push to the LSL stream. Each array
        /// must have the same number of columns as the total number of channels in the
        /// stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the chunks to the LSL stream.
        /// </returns>
        public IObservable<string[,]> Process(IObservable<string[,]> source)
            => Process(source, channel_format_t.cf_string, (data, outlet) => outlet.push_chunk(data));

        /// <summary>
        /// Pushes an observable sequence of timestamped variable length strings into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped variable length strings to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped values to the
        /// LSL stream.
        /// </returns>
        public IObservable<Timestamped<string>> Process(IObservable<Timestamped<string>> source)
            => Process(source, channel_format_t.cf_string,
                (sample, outlet) => outlet.push_sample(new[] { sample.Value }, sample.Timestamp));

        /// <summary>
        /// Pushes an observable sequence of timestamped variable length samples into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped variable length samples to push to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped samples to the
        /// LSL stream.
        /// </returns>
        public IObservable<TimestampedSample<string>> Process(IObservable<TimestampedSample<string>> source)
            => Process(source, channel_format_t.cf_string,
                (sample, outlet) => outlet.push_sample(sample.Data, sample.Timestamp));

        /// <summary>
        /// Pushes an observable sequence of timestamped variable length chunks into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped variable length chunks to push to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped chunks to the
        /// LSL stream.
        /// </returns>
        public IObservable<TimestampedChunk<string>> Process(IObservable<TimestampedChunk<string>> source)
            => Process(source, channel_format_t.cf_string, (sample, outlet) => outlet.push_chunk(sample.Data, sample.Timestamps));

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each element into a variable length string.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The sequence of elements to push to the LSL stream.</param>
        /// <param name="selector">
        /// The transform function used to select which values to write to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the values to the LSL stream.
        /// </returns>
        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, string> selector)
        {
            return Process(source, channel_format_t.cf_string,
                (value, outlet) => outlet.push_sample(new[] { selector(value) }));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each element into variable length samples.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">The sequence of elements to push to the LSL stream.</param>
        /// <param name="selector">
        /// The transform function used to select which samples to write to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the samples to the LSL stream.
        /// </returns>
        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, string[]> selector)
        {
            return Process(source, channel_format_t.cf_string,
                (value, outlet) => outlet.push_sample(selector(value)));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each timestamped element into a variable length string.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">
        /// The sequence of timestamped elements to push to the LSL stream.
        /// </param>
        /// <param name="selector">
        /// The transform function used to select which values to write to the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped values to the
        /// LSL stream.
        /// </returns>
        IObservable<Timestamped<TSource>> Process<TSource>(
            IObservable<Timestamped<TSource>> source,
            Func<TSource, string> selector)
        {
            return Process(source, channel_format_t.cf_string,
                (sample, outlet) => outlet.push_sample(new[] { selector(sample.Value) }, sample.Timestamp));
        }

        /// <summary>
        /// Pushes an observable sequence into the specified LSL stream, using a selector
        /// function to transform each timestamped element into variable length samples.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in the <paramref name="source"/> sequence.
        /// </typeparam>
        /// <param name="source">
        /// The sequence of timestamped elements to push to the LSL stream.
        /// </param>
        /// <param name="selector">
        /// The transform function used to select which samples to write to the LSL stream.
        /// Each sample array must have the same number of values as the total number of
        /// channels in the LSL stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped samples to the
        /// LSL stream.
        /// </returns>
        IObservable<Timestamped<TSource>> Process<TSource>(
            IObservable<Timestamped<TSource>> source,
            Func<TSource, string[]> selector)
        {
            return Process(source, channel_format_t.cf_string,
                (sample, outlet) => outlet.push_sample(selector(sample.Value), sample.Timestamp));
        }

        #endregion

        #region Buffer

        delegate int lsl_push_chunk(
            IntPtr obj,
            IntPtr data,
            uint dataLength,
            double timestamp,
            int pushthrough);

        static Action<Native.StreamOutlet, Mat, double> GetWriter(lsl_push_chunk push_chunk)
        {
            return (outlet, value, timestamp) =>
            {
                var elementCount = value.Rows * value.Cols;
                push_chunk(outlet.obj, value.Data, (uint)elementCount, timestamp, 1);
            };
        }

        static Action<Native.StreamOutlet, Mat, double> GetChunkWriter(Depth depth, out channel_format_t channelFormat)
        {
            switch (depth)
            {
                case Depth.U8:
                case Depth.S8:
                    channelFormat = channel_format_t.cf_int8;
                    return GetWriter(dll.lsl_push_chunk_ctp);
                case Depth.U16:
                case Depth.S16:
                    channelFormat = channel_format_t.cf_int16;
                    return GetWriter(dll.lsl_push_chunk_stp);
                case Depth.S32:
                    channelFormat = channel_format_t.cf_int32;
                    return GetWriter(dll.lsl_push_chunk_itp);
                case Depth.F32:
                    channelFormat = channel_format_t.cf_float32;
                    return GetWriter(dll.lsl_push_chunk_ftp);
                case Depth.F64:
                    channelFormat = channel_format_t.cf_double64;
                    return GetWriter(dll.lsl_push_chunk_dtp);
                case Depth.UserType:
                default:
                    throw new ArgumentException("Unsupported array depth.", nameof(depth));
            }
        }

        /// <summary>
        /// Pushes an observable sequence of multi-channel buffers into the specified
        /// LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of multi-channel buffers to push to the LSL stream. Each buffer
        /// must have the same number of rows as the total number of channels in the
        /// stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the buffers to the LSL stream.
        /// </returns>
        public IObservable<Mat> Process(IObservable<Mat> source)
        {
            return Process(source, chunk => chunk, chunk => 0);
        }

        /// <summary>
        /// Pushes an observable sequence of timestamped multi-channel buffers into
        /// the specified LSL stream.
        /// </summary>
        /// <param name="source">
        /// The sequence of timestamped multi-channel buffers to push to the LSL stream.
        /// Each buffer must have the same number of rows as the total number of channels
        /// in the stream.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the source sequence but where
        /// there is an additional side effect of writing the timestamped multi-channel
        /// buffers to the LSL stream.
        /// </returns>
        public IObservable<Timestamped<Mat>> Process(IObservable<Timestamped<Mat>> source)
        {
            return Process(source, chunk => chunk.Value, chunk => chunk.Timestamp);
        }

        IObservable<TSource> Process<TSource>(
            IObservable<TSource> source,
            Func<TSource, Mat> chunkSelector,
            Func<TSource, double> timestampSelector)
        {
            var name = Name;
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException("A valid LSL stream name must be specified.");
            }

            var contentType = ContentType ?? typeof(Mat).Name;
            var channelCount = ChannelCount;
            if (channelCount <= 0)
            {
                throw new InvalidOperationException("The number of channels must be a positive integer.");
            }

            return Observable.Defer(() =>
            {
                Mat buffer = default;
                Native.StreamOutlet outlet = default;
                Action<Native.StreamOutlet, Mat, double> chunkWriter = default;
                return source.Do(value =>
                {
                    var chunk = chunkSelector(value);
                    var timestamp = timestampSelector(value);
                    if (outlet == null)
                    {
                        chunkWriter = GetChunkWriter(chunk.Depth, out channel_format_t channelFormat);
                        var streamInfo = new StreamInfo(name, contentType, channelCount, LSL.IRREGULAR_RATE, channelFormat);
                        outlet = new Native.StreamOutlet(streamInfo);
                        if (chunk.Rows > 1)
                        {
                            buffer = new Mat(chunk.Cols, chunk.Rows, chunk.Depth, 1);
                        }
                    }

                    if (buffer != null)
                    {
                        CV.Transpose(chunk, buffer);
                        chunk = buffer;
                    }
                    chunkWriter(outlet, chunk, timestamp);
                }).Finally(() =>
                {
                    outlet?.Close();
                    buffer?.Close();
                });
            });
        }

        #endregion
    }
}
