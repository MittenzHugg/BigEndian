using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;

namespace BigEndian
{
    /* Big Endian Array Type Grabbing*/

    public static class BigEndian
    {
        //PUBLIC METHODS
        #region READ
        public static void read<T>(out T output, in IEnumerable<byte> input, int offset = 0) { output = read<T>(input, offset); }
        public static T read<T>(in IEnumerable<byte> input, int offset = 0)
        {
            var prim_method = typeof(BigEndian)
                .GetMethod("_read", BindingFlags.NonPublic | BindingFlags.Static);

            var method = typeof(BigEndian)
                .GetMethod("_readObj", BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(typeof(T));

            var result = method.Invoke(method, new object[] { input, offset, prim_method });
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public static void read_array<T>(in IEnumerable<byte> input, ref T[] output, int offset = 0)
        {
            int real_offset = offset;
            var prim_f = typeof(BigEndian)
                .GetMethod("_read", BindingFlags.NonPublic | BindingFlags.Static);

            _readArray<T>(input, ref output, ref real_offset, prim_f);
            return;

        }
        public static T[] read_array<T>(in IEnumerable<byte> input, int length, int offset = 0)
        {
            T[] output = new T[length];
            read_array(input, ref output, offset);
            return output;

        }

        public static int size<T>(in T typedVal) { return size<T>(); }
        public static int size<T>()
        {
            int output = 0;
            var prim_method = typeof(BigEndian)
                .GetMethod("_size", BindingFlags.NonPublic | BindingFlags.Static);

            var method = typeof(BigEndian)
                .GetMethod("_readObj", BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(typeof(T));

            object[] args = new object[] { new byte[0], output, prim_method };
            var result = method.Invoke(method, args);
            return (int)args[1];
        }

        #endregion

        #region WRITE
        public static byte[] write<T>(T value)
        {
            byte[] output = new byte[BigEndian.size<T>()];
            write<T>(value, ref output, 0);
            return output;
        }
        public static void write<T>(T value, ref byte[] output, int offset = 0)
        {
            int ref_offset = offset;
            _writeObj(value, ref output, ref ref_offset);
        }

        public static byte[] writeArray<T>(in T[] value)
        {
            byte[] output = new byte[BigEndian.size<T>() * value.Length];
            int ref_off = 0;
            _writeArray(value, ref output, ref ref_off);
            return output;
        }
        public static void writeArray<T>(in T[] value, ref byte[] output, int offset = 0)
        {
            int ref_off = offset;
            _writeArray(value, ref output, ref ref_off);
            return;
        }
        #endregion

        #region Private
        //PRIVATE PRIMATIVE METHODS
        private static T _read<T>(in IEnumerable<byte> input, ref int offset)
        {
            dynamic output = 0;
            int t_size = Marshal.SizeOf(typeof(T));
            for (int i = 0; i < t_size; i++)
            {
                if (i != 0)
                    output = output << 8;
                output |= input.ElementAt(offset);
                offset++;
            }
            return (T)output;
        }

        private static T _size<T>(in IEnumerable<byte> input, ref int offset)
        {
            offset += (int)Marshal.SizeOf(typeof(T));
            return default(T);
        }

        private static void _write<T>(T value, ref byte[] output, ref int offset)
        {
            dynamic in_val = value;
            UInt64 tmp = (UInt64)in_val;
            int t_size = Marshal.SizeOf(typeof(T));
            for (int i = 0; i < t_size; i++)
            {
                output[offset + i] = (byte)((tmp >> ((t_size - (i + 1)) * 8)) & 0xff);
            }
            offset += t_size;
            return;
        }


        //PRIVATE OBJECT ITERATION METHODS
        private static void _readArray<T>(in IEnumerable<byte> input, ref T[] output, ref int offset, MethodInfo prim_f)
        {
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = _readObj<T>(input, ref offset, prim_f);
            }
            return;

        }

        private static T _readObj<T>(IEnumerable<byte> input, ref int offset, MethodInfo prim_f)
        {
            if (typeof(T).IsPrimitive)
            {
                var gen_prim_method = prim_f.MakeGenericMethod(typeof(T));
                object[] args = new object[] { input, offset };
                T output = (T)gen_prim_method.Invoke(gen_prim_method, args);
                offset = (int)args[1];
                return output;
            }
            else
            {
                T output = (T)Activator.CreateInstance(typeof(T), null);
                foreach (FieldInfo prop in output.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    Type prop_t = prop.FieldType;
                    if (prop.FieldType.IsArray)
                    {
                        var x = prop.GetValue(output);
                        Type arrayType = prop_t.GetElementType();
                        object[] args = new object[] { input, x, offset, prim_f };
                        var method = typeof(BigEndian)
                            .GetMethod("_readArray", BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(arrayType);

                        method.Invoke(method, args);
                        offset = (int)args[2];
                    }
                    else
                    {
                        var method = typeof(BigEndian)
                            .GetMethod("_readObj", BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(prop_t);

                        object[] args = new object[] { input, offset, prim_f };
                        var result = method.Invoke(method, args);
                        offset = (int)args[1];
                        prop.SetValue(output, result);
                    }
                }
                return (T)output;
            }
        }

        private static void _writeArray<T>(in T[] value, ref byte[] output, ref int offset)
        {
            foreach (T curObj in value)
            {
                _writeObj(curObj, ref output, ref offset);
            }
            return;
        }

        private static void _writeObj<T>(T value, ref byte[] output, ref int offset)
        {
            if (typeof(T).IsPrimitive)
            {
                _write(value, ref output, ref offset);
            }
            else
            {
                foreach (FieldInfo prop in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    Type prop_t = prop.FieldType;
                    if (prop.FieldType.IsArray)
                    {
                        Type arrayType = prop_t.GetElementType();
                        object[] args = new object[] { prop.GetValue(value), output, offset };
                        var method = typeof(BigEndian)
                                 .GetMethod("_writeArray", BindingFlags.NonPublic | BindingFlags.Static)
                                 .MakeGenericMethod(arrayType);
                        offset = (int)args[2];
                    }
                    else
                    {
                        object[] args = new object[] { prop.GetValue(value), output, offset };
                        var method = typeof(BigEndian)
                            .GetMethod("_writeObj", BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(prop_t);
                        method.Invoke(method, args);
                        offset = (int)args[2];
                    }
                }
            }
            return;
        }
        #endregion
    }
}
