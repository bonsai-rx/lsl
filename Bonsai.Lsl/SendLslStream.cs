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
            var streamName = StreamName;
            var source = arguments.First();
            var parameterTypes = source.Type.GetGenericArguments();
            var inputParameter = Expression.Parameter(parameterTypes[0]);
            var builder = Expression.Constant(this);

            var buildStream = StreamBuilder.Stream(streamName, inputParameter);

            // this.Process(types, source, streambuilder)
            return Expression.Call(builder, null); // Placeholder
        }

        IObservable<int> Process(IObservable<int> source)
        {
            return Observable.Return(1);
        }

        IObservable<TSource> Process<TSource>(IObservable<TSource> source)
        {
            return Observable.Never<TSource>(); // Placeholder
        }
    }
}
