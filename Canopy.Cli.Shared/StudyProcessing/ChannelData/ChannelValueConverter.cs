using System;
using System.Collections.Generic;
using System.Linq;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    public interface IChannelValueConverter<T>
    {
        T Convert(object rawValue);
        byte[] Serialize(IEnumerable<T> values);
    }

    public sealed class FloatChannelValueConverter : IChannelValueConverter<float>
    {
        public float Convert(object rawValue) => rawValue switch
        {
            float f => f,
            double d => (float)d,
            int n => (float)n,
            long l => (float)l,
            _ => float.NaN
        };

        public byte[] Serialize(IEnumerable<float> values)
        {
            var list = values.ToArray();
            var bytes = new byte[list.Length * sizeof(float)];
            Buffer.BlockCopy(list, 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }

    public sealed class DoubleChannelValueConverter : IChannelValueConverter<double>
    {
        public double Convert(object rawValue) => rawValue switch
        {
            double d => d,
            float f => (double)f,
            int n => (double)n,
            long l => (double)l,
            _ => double.NaN
        };

        public byte[] Serialize(IEnumerable<double> values)
        {
            var list = values.ToArray();
            var bytes = new byte[list.Length * sizeof(double)];
            Buffer.BlockCopy(list, 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
