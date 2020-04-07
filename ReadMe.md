# Big Endian
### Library for pulling generic types (primatives, classes, structs) from byte collections (ex: array, list, etc.) with big endian byte ordering.

## Reading
The resulting type instance value can be returned as the function output:
```C#
T read<T>(in IEnumerable<byte> input, int offset = 0);
```
or be asigned by reference:
```C#
void read<T>(out T output, in IEnumerable<byte> input, int offset = 0)
```

**Arrays** of a single type `T` can be returned from the big-endian byte collection using the `read_array` function. 
The function will either return a new array of `T` instances:
```C#
T[] read_array<T>(in IEnumerable<byte> input, int length, int offset = 0)
```
or fill an existing array by reference:
```C#
void read_array<T>(in IEnumerable<byte> input, ref T[] output, int offset = 0)
```

## Writing
 Similar to the read operation, writing can either return a new `byte[]`
```C#
byte[] write<T>(T value) 
```
or write data to an existing array at a specific offset
```C#
void write<T>(T value, ref byte[] output, int offset = 0)
```

## Type Size
This returns the size in bytes of a generic type 'T'
```C#
int size<T>(in T typedVal)
```
```C#
int size<T>()
```

## Important Notes about C# classes and Structs
There are a few this to take into consideration trying to parse C# classes and structs from a byte array. 
 - C# structs do not support arrays of a defined size like C structs do.
 - C# classes do not normally maintain the order of their properties in the order you list them.
For these reasons I suggest you use define any classes you plan on reading in with this library as having an explicit layout.
### Example:
Below is an example of a class used to read N64 ROM headers:
```C#
[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public class ROMHeader
{
    [FieldOffset(0x00)] public byte init_state_1;
    [FieldOffset(0x01)] public byte init_state_2;
    [FieldOffset(0x02)] public byte init_state_3;
    [FieldOffset(0x03)] public byte init_state_4;
    [FieldOffset(0x04)] public uint clockRate;
    [FieldOffset(0x08)] public uint program_counter;
    [FieldOffset(0x0C)] public uint release_address;
    [FieldOffset(0x10)] public uint crc1;
    [FieldOffset(0x14)] public uint crc2;
    [FieldOffset(0x18)] public byte[] unknown_0x18_0x1F = new byte[0x08];
    [FieldOffset(0x20)] public char[] name = new char[0x14];
    [FieldOffset(0x34)] public uint unknown_0x34;
    [FieldOffset(0x38)] public uint media_format;
    [FieldOffset(0x3C)] public ushort cartidge_id;
    [FieldOffset(0x3E)] public byte country_code;
    [FieldOffset(0x3F)] public byte version;
}
```

```C#
using System;
using System.IO;
using BigEndian;

byte[] rom = File.ReadAllBytes("PATH/TO/N64/ROM.z64");
ROMHeader parsed_header = BigEndian.read<ROMHeader>(rom);
```
