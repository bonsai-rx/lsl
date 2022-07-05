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
        public string TypeTag { get; set; } = Lsl.TypeTag.Double.ToString();
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
            var buildReader = StreamBuilder.InletReader(typeTag, buildInlet, channelCount);
            var readerBuilder = Expression.Lambda<Func<StreamInlet, int, double>>(buildReader, new List<ParameterExpression> { streamInlet, channelCount });

            //// need one expression that creates an appropriately typed data buffer
            //var typeTag = TypeTag;
            //var buildBuffer = StreamBuilder.InletBuffer(typeTag, channelCount);
            //var bufferBuilder = Expression.Lambda(buildBuffer, new List<ParameterExpression>() { channelCount });

            //// need one expression that creates an inlet reader
            //var streamInlet = Expression.Parameter(typeof(StreamInlet), "streamInlet");
            //var buffer = Expression.Parameter(typeof(Array), "dataBuffer");
            //var buildReader = StreamBuilder.InletReader(typeTag, streamInlet, buffer);
            //var readerBuilder = Expression.Lambda(buildReader, new List<ParameterExpression> { streamInlet, buffer });

            //return Expression.Call(typeof(ReceiveLslStream), 
            //    nameof(Generate), 
            //    bufferBuilder.ReturnType.GetGenericArguments(), 
            //    Expression.Constant(StreamName), 
            //    Expression.Constant(ChannelCount),
            //    inletBuilder, bufferBuilder, readerBuilder);

            return Expression.Call(GenerateMethod,
                Expression.Constant(StreamName),
                Expression.Constant(ChannelCount),
                inletBuilder,
                readerBuilder); ;
        }

        static IObservable<double> Generate(string streamName, int channelCount, 
            Func<string, int, StreamInlet> inletBuilder, 
            Func<StreamInlet, int, double> readerBuilder)
        {
            return Observable.Create<double>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    var streamInlet = inletBuilder(streamName, channelCount);
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

        static readonly MethodInfo GenerateMethod = typeof(ReceiveLslStream).GetMethod(nameof(ReceiveLslStream.Generate));

        //TimestampedSample<T[]> GetSample<T>(StreamInlet inlet, T sampleArray)
        //{
        //    double sampleTime = inlet.pull_sample(sampleArray);
        //    return new TimestampedSample<T[]>(sampleTime, sampleArray);
        //}
    }
}
