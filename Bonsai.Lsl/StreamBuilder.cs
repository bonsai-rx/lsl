using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;

namespace Bonsai.Lsl
{
    static class StreamBuilder
    {

        public static StreamOutlet CreateOutlet(string streamName, string streamType, channel_format_t channelFormat)
        {
            var info = new StreamInfo(streamName, streamType, 1, LSL.IRREGULAR_RATE, channelFormat, "");
            return new StreamOutlet(info);
        }

        static readonly MethodInfo CreateOutletMethod = typeof(StreamBuilder).GetMethod(nameof(StreamBuilder.CreateOutlet));

        static readonly MethodInfo WriteInt64 = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(float[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteDouble = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(double[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteInt32 = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(int[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteInt16 = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(short[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteChar = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(char[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteString = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(string[]), typeof(double), typeof(bool) });

        public static Expression OutletStream(Expression nameParam, Expression typeParam, Expression parameter)
        {
            var type = parameter.Type;
            var typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                // int
                case TypeCode.Int32:
                    return Expression.Call(CreateOutletMethod, nameParam, typeParam, Expression.Constant(channel_format_t.cf_int32, typeof(channel_format_t)));

                // float
                case TypeCode.Single:
                    return Expression.Call(CreateOutletMethod, nameParam, typeParam, Expression.Constant(channel_format_t.cf_float32, typeof(channel_format_t)));  

                // long
                case TypeCode.Int64:
                    return Expression.Call(CreateOutletMethod, nameParam, typeParam, Expression.Constant(channel_format_t.cf_int64, typeof(channel_format_t)));

                default:
                    return null;
            }
        }

        public static Expression OutletWriter(Expression outlet, Expression data)
        {
            var type = data.Type;
            var typeCode = Type.GetTypeCode(type);
            NewArrayExpression dataArray;

            switch (typeCode)
            {
                // int
                case TypeCode.Int32:
                    data = Expression.NewArrayInit(typeof(int), new List<Expression> { data });
                    return Expression.Call(outlet, WriteInt32, data, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                // float
                case TypeCode.Single:
                    return null;

                // long
                case TypeCode.Int64:
                    data = Expression.Convert(data, typeof(float));
                    dataArray = Expression.NewArrayInit(typeof(float), new List<Expression> { data });
                    return Expression.Call(outlet, WriteInt64, dataArray, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                default:
                    return null;
            }
        }
    }
}
