using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;

namespace Bonsai.Lsl
{
    static class ObjectStreamBuilder
    {
        // Generate a StreamInfo/StreamOutlet from parameters
        public static StreamOutlet CreateOutlet(string streamName, string streamType, int channelCount, channel_format_t channelFormat)
        {
            var info = new StreamInfo(streamName, streamType, channelCount, LSL.IRREGULAR_RATE, channelFormat, "");
            return new StreamOutlet(info);
        }

        static readonly MethodInfo CreateOutletMethod = typeof(StreamBuilder).GetMethod(nameof(StreamBuilder.CreateOutlet));

        static readonly MethodInfo WriteFloat = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(float[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteDouble = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(double[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteInt32 = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(int[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteInt16 = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(short[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteChar = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(char[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteString = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(string[]), typeof(double), typeof(bool) });
        static readonly MethodInfo WriteLong = typeof(StreamOutlet).GetMethod(nameof(StreamOutlet.push_sample), new[] { typeof(long[]), typeof(double), typeof(bool) });

        // Copied from OSC Message builder
        static IEnumerable<MemberInfo> GetDataMembers(Type type)
        {
            var members = Enumerable.Concat<MemberInfo>(
                type.GetFields(BindingFlags.Instance | BindingFlags.Public),
                type.GetProperties(BindingFlags.Instance | BindingFlags.Public));
            if (type.IsInterface)
            {
                members = members.Concat(type
                    .GetInterfaces()
                    .SelectMany(i => i.GetProperties(BindingFlags.Instance | BindingFlags.Public)));
            }
            return members.OrderBy(member => member.MetadataToken);
        }

        // Generates an expression representing StreamOutlet creation, dependent on input data type (parameter)
        // TODO in this configuration we always use 1 channel, change the channel count parameter
        public static List<Expression> OutletStream(Expression nameParam, Expression typeParam, Expression channelCount, Expression parameter)
        {
            var type = parameter.Type;
            TypeCode typeCode = Type.GetTypeCode(type); // the typecode that we switch by depends on whether the input data is already in an array
            List<Expression> expressions = new List<Expression>();

            switch (typeCode)
            {
                // float
                case TypeCode.Single:
                    expressions.Add(Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_float32, typeof(channel_format_t))));
                    break;

                // double
                case TypeCode.Double:
                    expressions.Add(Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_double64, typeof(channel_format_t))));
                    break;

                // int
                case TypeCode.Int32:
                    expressions.Add(Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_int32, typeof(channel_format_t))));
                    break;

                // short
                case TypeCode.Int16:
                    expressions.Add(Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_int16, typeof(channel_format_t))));
                    break;

                // string
                case TypeCode.String:
                    expressions.Add(Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_string, typeof(channel_format_t))));
                    break;

                // long
                case TypeCode.Int64:
                    expressions.Add(Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_int64, typeof(channel_format_t))));
                    break;

                // For an object, we recurse through object members and generate a stream for each
                case TypeCode.Object:
                default:
                    // recursion time
                    var members = GetDataMembers(type);
                    foreach (MemberInfo member in members)
                    {
                        var memberAccess = Expression.MakeMemberAccess(parameter, member);
                        expressions.AddRange(OutletStream(nameParam, typeParam, channelCount, memberAccess));
                    }
                    break;
            }

            return expressions;
        }

        public static List<Expression> OutletWriter(Expression outlet, Expression data)
        {
            var type = data.Type;
            TypeCode typeCode = Type.GetTypeCode(type); // the typecode that we switch by depends on whether the input data is already in an array
            Expression formatData; // the way that we format the data to be pushed also depends on whether it is already in an array
            var expressions = new List<Expression>();

            //// if the data is in an array already, we need to switch by the element data type and there is no need to format
            //if (type.IsArray)
            //{
            //    typeCode = Type.GetTypeCode(
            //        Expression.ArrayAccess(data, new List<Expression> { Expression.Constant(1, typeof(int)) }).Type
            //    );
            //    formatData = data;
            //}
            //// if we have just a single value, we need to format the data into a single element array
            //else
            //{
            //    typeCode = Type.GetTypeCode(type);
            //    formatData = Expression.NewArrayInit(type, new List<Expression> { data });
            //}

            switch (typeCode)
            {
                // float
                case TypeCode.Single:
                    formatData = Expression.NewArrayInit(type, new List<Expression> { data });
                    expressions.Add(Expression.Call(outlet, WriteFloat, formatData, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool))));
                    break;

                // double
                case TypeCode.Double:
                    formatData = Expression.NewArrayInit(type, new List<Expression> { data });
                    expressions.Add(Expression.Call(outlet, WriteDouble, formatData, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool))));
                    break;

                // int
                case TypeCode.Int32:
                    formatData = Expression.NewArrayInit(type, new List<Expression> { data });
                    expressions.Add(Expression.Call(outlet, WriteInt32, formatData, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool))));
                    break;

                // short
                case TypeCode.Int16:
                    formatData = Expression.NewArrayInit(type, new List<Expression> { data });
                    expressions.Add(Expression.Call(outlet, WriteInt16, formatData, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool))));
                    break;

                // string
                case TypeCode.String:
                    formatData = Expression.NewArrayInit(type, new List<Expression> { data });
                    expressions.Add(Expression.Call(outlet, WriteString, formatData, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool))));
                    break;

                // long
                case TypeCode.Int64:
                    formatData = Expression.NewArrayInit(type, new List<Expression> { data });
                    expressions.Add(Expression.Call(outlet, WriteLong, formatData, Expression.Constant(0.0, typeof(double)), Expression.Constant(true, typeof(bool))));
                    break;

                case TypeCode.Object:
                default:
                    // recursion time
                    var members = GetDataMembers(type);
                    foreach (MemberInfo member in members)
                    {
                        var memberAccess = Expression.MakeMemberAccess(data, member);
                        expressions.AddRange(OutletWriter(outlet, memberAccess));
                    }
                    break;
            }

            return expressions;
        }
    }
}
