using System;
using System.Reactive.Linq;

namespace Bonsai.Lsl
{
    public class FloatStreamBroadcaster : Sink<float>
    {
        public string StreamName { get; set; }
        public string StreamType { get; set; }
        public string Uid { get; set; }

        public override IObservable<float> Process(IObservable<float> source)
        {
            return Observable.Using(() =>
            {
                StreamInfo info = new StreamInfo(StreamName, StreamType, 1, 0, channel_format_t.cf_float32, Uid);
                StreamOutlet outlet = new StreamOutlet(info);

                return outlet;
            },
            outlet => source.Do(input =>
            {
                outlet.push_sample(new float[] { input });
            }));
        }
    }
}
