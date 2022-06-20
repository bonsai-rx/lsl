using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace Bonsai.Lsl
{
    static class StreamBuilder
    {
        public static Expression Stream(string streamName, Expression parameter)
        {

        }

        static Expression CreateStreamBuilder(Expression parameter, StringBuilder typeTagBuilder)
        {
            var type = parameter.Type;
            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                // float 
                case TypeCode.Single:
                    return null;

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
#
                // string
                case TypeCode.String:
                    return null;

                //object
                case TypeCode.Object:
                    return null;
            }
        }
    }
}
