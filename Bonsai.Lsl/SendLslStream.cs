using Bonsai.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Bonsai.Lsl
{
    [WorkflowElementCategory(ElementCategory.Sink)]
    public class SendLslStream : SingleArgumentExpressionBuilder
    {
        public string StreamName { get; set; }
        public string StreamType { get; set; }
        public int ChannelCount { get; set; }
        public string Uid { get; set; }

        public override Expression Build(IEnumerable<Expression> arguments)
        {
            var streamName = Expression.Parameter(typeof(string), "streamName");
            var streamType = Expression.Parameter(typeof(string), "streamType");
            var channelCount = Expression.Parameter(typeof(int), "channelCount");
            var source = arguments.First(); // input source
            var parameterTypes = source.Type.GetGenericArguments(); // source types
            var inputParameter = Expression.Parameter(parameterTypes[0], "inputParameter");
            var builder = Expression.Constant(this);

            // need one expression that produces a stream outlet of the correct format - TODO don't like this naming convention of swapping 'build' position in variable name
            var buildStream = StreamBuilder.OutletStream(streamName, streamType, channelCount, inputParameter);
            var streamBuilder = Expression.Lambda<Func<string, string, int, StreamOutlet>>(buildStream, new List<ParameterExpression>() { streamName, streamType, channelCount });

            // need one expression that writes to the outlet with the correct format
            var outletParam = Expression.Parameter(typeof(StreamOutlet), "outletParam");
            var dataParam = Expression.Parameter(parameterTypes[0], "dataParam");

            var buildWriter = StreamBuilder.OutletWriter(outletParam, dataParam);
            var streamWriter = Expression.Lambda(buildWriter, new List<ParameterExpression>() { outletParam, dataParam });

            //                     this     .Process         <parameterTypes>(source, streamBuilder, streamWriter)
            return Expression.Call(builder, nameof(Process), parameterTypes, source, streamBuilder, streamWriter);
        }

        IObservable<double> Process<TSource>(IObservable<TSource> source, Func<string, string, int, StreamOutlet> streamBuilder, Action<StreamOutlet, TSource> streamWriter)
        {
            return Observable.Using(
                () =>
                {
                    return streamBuilder(StreamName, StreamType, ChannelCount);
                },
                outlet => source.Select(input =>
                {
                    streamWriter(outlet, input);
                    return LSL.local_clock();
                }).Finally(() => { outlet.Close(); })
            );
        }
    }
}
