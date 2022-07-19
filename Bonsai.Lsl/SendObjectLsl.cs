using Bonsai.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Bonsai.Lsl
{
    [WorkflowElementCategory(ElementCategory.Sink)]
    public class SendObjectLsl : SingleArgumentExpressionBuilder
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

            // Generates required outlets
            var buildOutlets = ObjectStreamBuilder.OutletStream(streamName, streamType, channelCount, inputParameter);
            var outletFuncs = buildOutlets.Select(outlet => Expression.Lambda(outlet, new List<ParameterExpression> { streamName, streamType, channelCount })).ToList();

            // Experimental - object accessor - DEBUG
            //var buildAccessors = ObjectStreamBuilder.ObjectAccessor(inputParameter);
            //var accessorFuncs = buildAccessors.Select(access => Expression.Lambda<Func<object>>(access, new List<ParameterExpression> { })).ToList();
            //var comp = accessorFuncs[0].Compile();
            //comp();

            // Generate required writers
            var outletParam = Expression.Parameter(typeof(StreamOutlet), "outletParam");
            var dataParam = Expression.Parameter(parameterTypes[0], "dataParam");
            var buildWriters = ObjectStreamBuilder.OutletWriter(outletParam, dataParam);
            var writerFuncs = buildWriters.Select(writer => Expression.Lambda(writer, new List<ParameterExpression> { outletParam, dataParam }));

            //                     this     .Process        <parameterTypes>(source)
            return Expression.Call(builder, nameof(Process), parameterTypes, source);
        }

        IObservable<List<double>> Process<TSource>(IObservable<TSource> source)
        {
            return Observable.Never(new List<double> { 1 });
        }

        //IObservable<List<double>> Process<TSource>(IObservable<TSource> source, List<Func<string, string, int, StreamOutlet>> outletBuilder)
        //{
        //    return Observable.Never(new List<double> { 1 });
        //}

        //IObservable<List<double>> Process<TSource>(IObservable<TSource> source, List<Func<string, string, int, StreamOutlet>> outletBuilder, List<Action<StreamOutlet, TSource>> streamWriter)
        //{
        //    return Observable.Never(new List<double> { 1 });
        //}
    }
}
