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
        public static Expression OutletStream(Expression nameParam, Expression typeParam, Expression channelCount, Expression parameter, List<Expression> outletExpressions)
        {
            var type = parameter.Type;
            TypeCode typeCode = Type.GetTypeCode(type); ; // the typecode that we switch by depends on whether the input data is already in an array
            Expression exp;

            switch (typeCode)
            {
                // float
                case TypeCode.Single:
                    exp = Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_float32, typeof(channel_format_t)));
                    break;

                // double
                case TypeCode.Double:
                    exp = Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_double64, typeof(channel_format_t)));
                    break;

                // int
                case TypeCode.Int32:
                    exp = Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_int32, typeof(channel_format_t)));
                    break;

                // short
                case TypeCode.Int16:
                    exp = Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_int16, typeof(channel_format_t)));
                    break;

                // string
                case TypeCode.String:
                    exp = Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_string, typeof(channel_format_t)));
                    break;

                // long
                case TypeCode.Int64:
                    exp = Expression.Call(CreateOutletMethod, nameParam, typeParam, channelCount, Expression.Constant(channel_format_t.cf_int64, typeof(channel_format_t)));
                    break;

                // For an object, we recurse through object members and generate a stream for each
                case TypeCode.Object:
                default:
                    // recursion time
                    var members = GetDataMembers(type);
                    return Expression.Block(members.Select(member =>
                    {
                        var memberAccess = Expression.MakeMemberAccess(parameter, member);
                        return OutletStream(nameParam, typeParam, channelCount, memberAccess, outletExpressions);
                    }));
            }

            outletExpressions.Add(exp);
            return exp;
        }
    }
}
