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
        // Generate a StreamInfo/StreamOutlet from parameters
        public static StreamOutlet CreateOutlet(string streamName, string streamType, int channelCount, channel_format_t channelFormat)
        {
            var info = new StreamInfo(streamName, streamType, channelCount, LSL.IRREGULAR_RATE, channelFormat, "");
            return new StreamOutlet(info);
        }

        // Reflection reference to outlet creation method
        static readonly MethodInfo CreateOutletMethod = typeof(StreamBuilder).GetMethod(nameof(StreamBuilder.CreateOutlet));

        // Reflection references to push_sample overload methods
        static readonly MethodInfo WriteFloat = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(float[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteDouble = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(double[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteInt32 = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(int[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteInt16 = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(short[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteChar = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(char[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteString = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(string[]), typeof(double), typeof(bool) });

        // Generates an expression representing StreamOutlet creation, dependent on input data type (parameter)
        public static Expression OutletStream(Expression nameParam, Expression typeParam, Expression parameter)
        {
            var type = parameter.Type;
            var typeCode = Type.GetTypeCode(type);

            Expression channelCount = type.IsArray ? Expression.ArrayLength(parameter) : Expression.Constant(1);

            switch (typeCode)
            {
                // float
                case TypeCode.Single:
                    return Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_float32, typeof(channel_format_t)));

                // double
                case TypeCode.Double:
                    return Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_double64, typeof(channel_format_t)));

                // int
                case TypeCode.Int32:
                    return Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_int32, typeof(channel_format_t)));

                // short
                case TypeCode.Int16:
                    return Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_int16, typeof(channel_format_t)));

                // string
                case TypeCode.String:
                    return Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_string, typeof(channel_format_t)));

                // long
                case TypeCode.Int64:
                    return Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_int64, typeof(channel_format_t)));

                // For any other types, we need largest type that can hold other types (double64)
                case TypeCode.Object:
                default:
                    return Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_double64, typeof(channel_format_t)));
            }
        }

        // Generates an expression representing a write action to an outlet based on data type
        public static Expression OutletWriter(Expression outlet, Expression data)
        {
            var type = data.Type;
            var typeCode = Type.GetTypeCode(type);

            if (type.IsArray)
            {

            } else
            {

            }

            switch (typeCode)
            {
                // float
                case TypeCode.Single:
                    data = Expression.NewArrayInit(typeof(float), new List<Expression> { data });
                    return Expression.Call(outlet, WriteFloat, data, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                // double
                case TypeCode.Double:
                    data = Expression.NewArrayInit(typeof(double), new List<Expression> { data });
                    return Expression.Call(outlet, WriteDouble, data, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                // int
                case TypeCode.Int32:
                    data = Expression.NewArrayInit(typeof(int), new List<Expression> { data });
                    return Expression.Call(outlet, WriteInt32, data, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                // short
                case TypeCode.Int16:
                    data = Expression.NewArrayInit(typeof(short), new List<Expression> { data });
                    return Expression.Call(outlet, WriteInt16, data, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                // string
                case TypeCode.String:
                    data = Expression.NewArrayInit(typeof(string), new List<Expression> { data });
                    return Expression.Call(outlet, WriteString, data, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                // long
                case TypeCode.Int64:
                    data = Expression.Convert(data, typeof(float)); // no LSL long writer, need to convert to float
                    data = Expression.NewArrayInit(typeof(float), new List<Expression> { data });
                    return Expression.Call(outlet, WriteFloat, data, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                case TypeCode.Object:
                default:
                    return null;
            }
        }
    }
}
