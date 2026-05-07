using Parquet.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    public interface IChannelValueConverter<T>
    {
        T Convert(object rawValue);
        byte[] Serialize(IEnumerable<T> values);

        void AddConvertedValues(RawColumnData rawData, List<T> targetList)
        {
            switch (rawData)
            {
                case RawColumnData<float> fd:
                    foreach (var v in fd.Values) targetList.Add(Convert(v));
                    break;
                case RawColumnData<double> dd:
                    foreach (var v in dd.Values) targetList.Add(Convert(v));
                    break;
                case RawColumnData<int> id:
                    foreach (var v in id.Values) targetList.Add(Convert(v));
                    break;
                case RawColumnData<long> ld:
                    foreach (var v in ld.Values) targetList.Add(Convert(v));
                    break;
            }
        }
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

    public sealed class IntChannelValueConverter : IChannelValueConverter<int>
    {
        public int Convert(object rawValue) => rawValue switch
        {
            int n => n,
            long l => (int)l,
            float f => (int)f,
            double d => (int)d,
            _ => int.MinValue
        };

        public byte[] Serialize(IEnumerable<int> values)
        {
            var list = values.ToArray();
            var bytes = new byte[list.Length * sizeof(int)];
            Buffer.BlockCopy(list, 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }

    public sealed class LongChannelValueConverter : IChannelValueConverter<long>
    {
        public long Convert(object rawValue) => rawValue switch
        {
            long l => l,
            int n => (long)n,
            float f => (long)f,
            double d => (long)d,
            _ => long.MinValue
        };

        public byte[] Serialize(IEnumerable<long> values)
        {
            var list = values.ToArray();
            var bytes = new byte[list.Length * sizeof(long)];
            Buffer.BlockCopy(list, 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
