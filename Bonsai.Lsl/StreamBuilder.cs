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

        // Generate a StreamInfo/StreamInlet from parameters
        public static StreamInlet CreateInlet(string streamName, int channelCount, processing_options_t processingOptions)
        {
            StreamInfo info = LSL.resolve_stream("name", streamName, channelCount)[0]; // TODO - assumes first returned stream is correct
            return new StreamInlet(info, postproc_flags: processingOptions);
        }

        // Reflection reference to outlet/inlet creation method
        static readonly MethodInfo CreateOutletMethod = typeof(StreamBuilder).GetMethod(nameof(StreamBuilder.CreateOutlet));
        static readonly MethodInfo CreateInletMethod = typeof(StreamBuilder).GetMethod(nameof(StreamBuilder.CreateInlet));

        // Reflection references to push_sample overload methods
        static readonly MethodInfo WriteFloat = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(float[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteDouble = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(double[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteInt32 = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(int[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteInt16 = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(short[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteChar = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(char[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteString = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(string[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteLong = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(long[]), typeof(double), typeof(bool) });

        // Reflection references to push_sample overload methods
        static readonly MethodInfo ReadFloat = typeof(StreamInlet).GetMethod(nameof(StreamInlet.pull_sample), new[] { typeof(float[]), typeof(double) });
        static readonly MethodInfo ReadDouble = typeof(StreamInlet).GetMethod(nameof(StreamInlet.pull_sample), new[] { typeof(double[]), typeof(double) });
        static readonly MethodInfo ReadInt32 = typeof(StreamInlet).GetMethod(nameof(StreamInlet.pull_sample), new[] { typeof(int[]), typeof(double) });
        static readonly MethodInfo ReadInt16 = typeof(StreamInlet).GetMethod(nameof(StreamInlet.pull_sample), new[] { typeof(short[]), typeof(double) });
        static readonly MethodInfo ReadChar = typeof(StreamInlet).GetMethod(nameof(StreamInlet.pull_sample), new[] { typeof(char[]), typeof(double) });
        static readonly MethodInfo ReadString = typeof(StreamInlet).GetMethod(nameof(StreamInlet.pull_sample), new[] { typeof(string[]), typeof(double) });
        static readonly MethodInfo ReadLong = typeof(StreamInlet).GetMethod(nameof(StreamInlet.pull_sample), new[] { typeof(long[]), typeof(double) });

        // Generates an expression representing StreamOutlet creation, dependent on input data type (parameter)
        public static Expression OutletStream(Expression nameParam, Expression typeParam, Expression channelCount, Expression parameter)
        {
            var type = parameter.Type;
            TypeCode typeCode; // the typecode that we switch by depends on whether the input data is already in an array

            // if the data is in an array already, we need to switch by the element data type, otherwise we will get object as type
            if (type.IsArray)
            {
                typeCode = Type.GetTypeCode(
                    Expression.ArrayAccess(parameter, new List<Expression> { Expression.Constant(1, typeof(int)) }).Type
                );
            }
            else
            {
                typeCode = Type.GetTypeCode(type);
            }

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

        public static Expression InletStream(Expression nameParam, Expression channelCount)
        {
            return Expression.Call(CreateInletMethod, nameParam, channelCount, Expression.Constant(processing_options_t.proc_ALL));
        }

        // Generates an expression representing a write action to an outlet based on data type
        public static Expression OutletWriter(Expression outlet, Expression data)
        {
            var type = data.Type;
            TypeCode typeCode; // the typecode that we switch by depends on whether the input data is already in an array
            Expression formatData; // the way that we format the data to be pushed also depends on whether it is already in an array

            // if the data is in an array already, we need to switch by the element data type and there is no need to format
            if (type.IsArray)
            {
                typeCode = Type.GetTypeCode(
                    Expression.ArrayAccess(data, new List<Expression> { Expression.Constant(1, typeof(int)) }).Type
                );
                formatData = data;
            }
            // if we have just a single value, we need to format the data into a single element array
            else
            {
                typeCode = Type.GetTypeCode(type);
                formatData = Expression.NewArrayInit(type, new List<Expression> { data });
            }

            switch (typeCode)
            {
                // float
                case TypeCode.Single:
                    return Expression.Call(outlet, WriteFloat, formatData, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                // double
                case TypeCode.Double:
                    return Expression.Call(outlet, WriteDouble, formatData, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                // int
                case TypeCode.Int32:
                    return Expression.Call(outlet, WriteInt32, formatData, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                // short
                case TypeCode.Int16:
                    return Expression.Call(outlet, WriteInt16, formatData, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                // string
                case TypeCode.String:
                    return Expression.Call(outlet, WriteString, formatData, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                // long
                case TypeCode.Int64:
                    return Expression.Call(outlet, WriteLong, formatData, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool)));

                case TypeCode.Object:
                default:
                    return null;
            }
        }

        public static Expression InletReader(string typeTag, Expression inlet, Expression channelCount)
        {
            NewArrayExpression buffer;
            switch (typeTag[0]) // TODO - doesn't do multiple tags yet 
            {
                // float
                case TypeTag.Float:
                    buffer = Expression.NewArrayBounds(typeof(float), channelCount);
                    return Expression.Call(inlet, ReadFloat, buffer, Expression.Constant(LSL.FOREVER));

                // double
                case TypeTag.Double:
                    buffer = Expression.NewArrayBounds(typeof(double), channelCount);
                    return Expression.Call(inlet, ReadDouble, buffer, Expression.Constant(LSL.FOREVER));

                // int
                case TypeTag.Int32:
                    buffer = Expression.NewArrayBounds(typeof(int), channelCount);
                    return Expression.Call(inlet, ReadInt32, buffer, Expression.Constant(LSL.FOREVER));

                // short - TODO no short typetag

                // string
                case TypeTag.String:
                    buffer = Expression.NewArrayBounds(typeof(string), channelCount);
                    return Expression.Call(inlet, ReadString, buffer, Expression.Constant(LSL.FOREVER));

                // long
                case TypeTag.Int64:
                    buffer = Expression.NewArrayBounds(typeof(long), channelCount);
                    return Expression.Call(inlet, ReadLong, buffer, Expression.Constant(LSL.FOREVER));

                // other
                case TypeTag.Blob:
                default:
                    return null;
            } 
        }
    }
}
