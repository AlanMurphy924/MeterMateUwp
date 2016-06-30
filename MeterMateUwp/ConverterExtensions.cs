using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeterMateUwp
{
    public static class ConverterExtensions
    {
        public static float ToCentigrade(this float v)
        {
            return (v - 32) * (5f / 9f);
        }

        public static double ToCentigrade(this double v)
        {
            return (v - 32) * (5.0 / 9.0);
        }

        public static float ToFarenheit(this float v)
        {
            return (v * 5f / 9f) + 32;
        }

        public static double ToFarenheit(this double v)
        {
            return (v * 5.0 / 9.0) + 32;
        }

        public static float GetSingle(this byte[] buffer, int position)
        {
            return BitConverter.ToSingle(buffer, position);
        }

        public static double GetDouble(this byte[] buffer, int position)
        {
            return BitConverter.ToDouble(buffer, position);
        }

        public static uint GetUnsignedInt(this byte[] buffer, int position)
        {
            return BitConverter.ToUInt32(buffer, position);
        }

        public static ushort GetUnsignedShort(this byte[] buffer, int position)
        {
            return BitConverter.ToUInt16(buffer, position);
        }

        public static short GetShort(this byte[] buffer, int position)
        {
            return BitConverter.ToInt16(buffer, position);
        }
    }
}
