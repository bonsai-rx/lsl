using Bonsai.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Bonsai.Lsl
{
    [WorkflowElementCategory(ElementCategory.Source)]
    public class ReceiveLslStream : SingleArgumentExpressionBuilder
    {
        public string StreamName { get; set; }
        [TypeConverter(typeof(TypeTagConverter))]
        public string TypeTag { get; set; } = Lsl.TypeTag.Int64.ToString();
        public int ChannelCount { get; set; }

        static readonly Range<int> argumentRange = Range.Create(lowerBound: 0, upperBound: 0);
        public override Range<int> ArgumentRange
        {
            get { return argumentRange; }
        }

        public override Expression Build(IEnumerable<Expression> arguments)
        {
            var streamName = Expression.Parameter(typeof(string), "streamName");
            var channelCount = Expression.Parameter(typeof(int), "channelCount");
            var builder = Expression.Constant(this);

            // need one expression that produces a stream inlet of the correct format - TODO don't like naming convention (see SendLslStream)
            var buildInlet = StreamBuilder.InletStream(streamName, channelCount);
            var inletBuilder = Expression.Lambda<Func<string, int, StreamInlet>>(buildInlet, new List<ParameterExpression>() { streamName, channelCount });

            // need one expression to read from that inlet
            var typeTag = TypeTag;
            var streamInlet = Expression.Parameter(typeof(StreamInlet), "streamInlet");
            var buildReader = StreamBuilder.InletReader(typeTag, streamInlet, channelCount);
            var readerBuilder = Expression.Lambda<Func<StreamInlet, int, double>>(buildReader, new List<ParameterExpression> { streamInlet, channelCount });

            var parameterTypes = new Type[] { typeof(double) }; // placeholder for generic stuff later

            return Expression.Call(builder,
                nameof(Generate),
                parameterTypes,
                Expression.Constant(StreamName, typeof(string)),
                Expression.Constant(ChannelCount, typeof(int)),
                inletBuilder,
                readerBuilder);
        }

        IObservable<double> Generate<T>(string streamName, int channelCount, 
            Func<string, int, StreamInlet> inletBuilder, 
            Func<StreamInlet, int, double> readerBuilder)
        {
            return Observable.Create<double>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    StreamInlet streamInlet = inletBuilder(streamName, channelCount);
                    streamInlet.open_stream();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        double sampleTime = readerBuilder(streamInlet, channelCount);
                        observer.OnNext(sampleTime);
                    }

                    streamInlet.close_stream();
                });
            });
        }
    }
}
