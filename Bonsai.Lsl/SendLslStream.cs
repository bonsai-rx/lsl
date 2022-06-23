using Bonsai.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public string Uid { get; set; }

        public override Expression Build(IEnumerable<Expression> arguments)
        {
            var streamName = Expression.Parameter(typeof(string), "streamName");
            var streamType = Expression.Parameter(typeof(string), "streamType");
            var source = arguments.First(); // input source
            var parameterTypes = source.Type.GetGenericArguments(); // source types
            var inputParameter = Expression.Parameter(parameterTypes[0], "inputParameter");
            var builder = Expression.Constant(this);

            // need one expression that produces a stream outlet of the correct format
            var buildStream = StreamBuilder.OutletStream(streamName, streamType, inputParameter);
            var streamBuilder = Expression.Lambda<Func<string, string, StreamOutlet>>(buildStream, new List<ParameterExpression>() { streamName, streamType });
            //var streamBuilder = Expression.Lambda(buildStream, new List<ParameterExpression>() { streamName, streamType, inputParameter });

            streamBuilder.Compile();

            // need one expression that writes to the outlet with the correct format
            var outletParam = Expression.Parameter(typeof(StreamOutlet), "outletParam");
            var dataParam = Expression.Parameter(parameterTypes[0], "dataParam");

            var buildWriter = StreamBuilder.OutletWriter(outletParam, dataParam);
            var streamWriter = Expression.Lambda(buildWriter, new List<ParameterExpression>() { outletParam, dataParam });

            //                     this     .Process         <parameterTypes>(source, streamBuilder, streamWriter)
            return Expression.Call(builder, nameof(Process), parameterTypes, source, streamBuilder, streamWriter);
        }

        IObservable<TSource> Process<TSource>(IObservable<TSource> source, Func<string, string, StreamOutlet> streamBuilder, Action<StreamOutlet, TSource> streamWriter)
        {
            return Observable.Using(
                () =>
                {
                    //StreamInfo info = new StreamInfo(StreamName, StreamType, 1, 0, channel_format_t.cf_float32, Uid);
                    //return new StreamOutlet(info);
                    return streamBuilder(StreamName, StreamType);
                },
                outlet => source.Do(input =>
                {
                    //outlet.push_sample(new float[] { input });
                    streamWriter(outlet, input);
                })
            );
        }
    }
}
