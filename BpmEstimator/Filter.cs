using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BpmEstimator
{
    public static class Filter
    {
        public static short[] BiQuadFilter(Span<short> input, double a0, double a1, double a2, double b0, double b1, double b2)
        {
            short[] output = new short[input.Length];
            double in1 = 0, in2 = 0, out1 = 0, out2 = 0;
            for (int i = 0; i < input.Length; i++)
            {
                double current = (short)(b0 / a0 * input[i] + b1 / a0 * in1 + b2 / a0 * in2 - a1 / a0 * out1 - a2 / a0 * out2);
                output[i] = (short)current;
                in2 = in1;
                in1 = input[i];
                out2 = out1;
                out1 = current;
            }
            return output;
        }

        public static short[] LowPassFilter(Span<short> input, double sampleFrequency, double cutOffFrequency, double q)
        {
            double omega = 2.0 * Math.PI * cutOffFrequency / sampleFrequency;
            double alpha = Math.Sin(omega) / (2.0 * q);
            double a0 = 1.0 + alpha;
            double a1 = -2.0 * Math.Cos(omega);
            double a2 = 1.0 - alpha;
            double b0 = (1.0 - Math.Cos(omega)) / 2.0;
            double b1 = 1.0 - Math.Cos(omega);
            double b2 = (1.0 - Math.Cos(omega)) / 2.0;
            return BiQuadFilter(input, a0, a1, a2, b0, b1, b2);
        }
    }
}
