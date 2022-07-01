using System.ComponentModel;

namespace Bonsai.Lsl
{
    class TypeTagConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new[]
            {
                TypeTag.Int32,
                TypeTag.Float,
                TypeTag.String,
                TypeTag.Blob,
                TypeTag.Int64,
                TypeTag.TimeTag,
                TypeTag.Double,
                TypeTag.Char
            });
        }
    }
}