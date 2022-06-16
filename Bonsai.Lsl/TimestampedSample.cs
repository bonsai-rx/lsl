using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.Lsl
{
    public class TimestampedSample<T>
    {
        public double SampleTime;
        public T[] SampleArray;

        public TimestampedSample(double sampleTime, T[] sampleArray)
        {
            SampleTime = sampleTime;
            SampleArray = sampleArray;
        }
    }
}
