using System;
using LSL;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Bonsai.LSL
{
    public class FloatStreamReceiver : Source<TimestampedSample<float>>
    {
        public string ResolveStreamName { get; set; }
        public processing_options_t InletProcessing { get; set; }

        public override IObservable<TimestampedSample<float>> Generate()
        {
            return Observable.Create<TimestampedSample<float>>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    StreamInfo info = global::LSL.LSL.resolve_stream("name", ResolveStreamName, 1)[0];
                    StreamInlet inlet = new StreamInlet(info, postproc_flags: InletProcessing);
                    inlet.open_stream();
                    float[] sampleArray = new float[info.channel_count()];

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        observer.OnNext(GetSample(inlet, sampleArray));
                    }

                    inlet.close_stream();
                });
            });
        }

        public TimestampedSample<float> GetSample(StreamInlet inlet, float[] sampleArray)
        {
            double sampleTime = inlet.pull_sample(sampleArray);
            return new TimestampedSample<float>(sampleTime, sampleArray);
        }
    }
}
