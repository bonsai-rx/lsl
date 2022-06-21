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

        public static StreamOutlet CreateOutlet(string streamName, string streamType, int channelCount, double nominalSampRate, channel_format_t channelFormat, string uid)
        {
            var info = new StreamInfo(streamName, streamType, channelCount, nominalSampRate, channelFormat, uid);
            return new StreamOutlet(info);
        }

        static readonly MethodInfo CreateOutletMethod = typeof(StreamBuilder).GetMethod(nameof(StreamBuilder.CreateOutlet));

        public static Expression OutletStream(string streamName, string streamType, Expression parameter)
        {
            var typeTagBuilder = new StringBuilder();
            var outletStreamBuilder = CreateOutletStreamBuilder(parameter, typeTagBuilder);

            return Expression.Block(outletStreamBuilder);
        }

        //public static Expression OutletStreamWriter()
        //{

        //}

        static Expression CreateOutletStreamBuilder(Expression parameter, StringBuilder typeTagBuilder)
        {
            var type = parameter.Type;
            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                // float 
                case TypeCode.Single:
                    // need to return an expression call that creates a stream of type float
                    //StreamInfo info = new StreamInfo(StreamName, StreamType, 1, 0, channel_format_t.cf_float32, Uid);
                    //return new StreamOutlet(info);
                    return Expression.Call(typeof(StreamBuilder), nameof(CreateOutlet), null, null, null, null, null, null);

                // double
                case TypeCode.Double:
                    return null;

                // int
                case TypeCode.Int32:
                    return null;

                // short
                case TypeCode.Int16:
                    return null;

                // char
                case TypeCode.Char:
                    return null;

                // string
                case TypeCode.String:
                    return null;

                //object
                case TypeCode.Object:
                    return null;

                default:
                    return null;
            }
        }

        static Expression CreateOutletStreamWriterBuilder()
        {
            return null;
        }
    }
}
