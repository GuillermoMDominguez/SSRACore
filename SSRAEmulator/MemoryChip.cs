using System;
using System.Collections.Generic;
using System.Text;

namespace SSRAEmulator
{
    internal class MemoryChip
    {
        byte[] memory;
        internal const int size = 4096;
        internal MemoryChip()
        {
            memory = new byte[size]; //1 kB
        }
        internal void LoadData(byte[] data, int start)
        {
            for (int i = 0; i < data.Length; i++)
            {
                memory[start + i] = data[i];
            }
        }
        internal ulong ReadMemory(int address)
        {
            ulong word = ((ulong)memory[address] << 56) + ((ulong)memory[address + 1] << 48)
                + ((ulong)memory[address + 2] << 40) + ((ulong)memory[address + 3] << 32)
                + ((ulong)memory[address + 4] << 24) + ((ulong)memory[address + 5] << 16)
                + ((ulong)memory[address + 6] << 8) + ((ulong)memory[address + 7]);
            return word;
        }
        internal ulong[] ReadWords(int address,int number)
        {
            var result = new ulong[number];
            for(int i = 0; i < number; i++)
            {
                result[i] = ReadMemory(address + i*8);
            }
            return result;
        }
        internal double ReadMemoryDouble(int address)
        {
            long word = ((long)memory[address] << 56) + ((long)memory[address + 1] << 48)
                + ((long)memory[address + 2] << 40) + ((long)memory[address + 3] << 32)
                + ((long)memory[address + 4] << 24) + ((long)memory[address + 5] << 16)
                + ((long)memory[address + 6] << 8) + ((long)memory[address + 7]);
            double fword = BitConverter.Int64BitsToDouble(word);
            return fword;
        }
        internal double[] ReadDoubleWords(int address, int number)
        {
            var result = new double[number];
            for (int i = 0; i < number; i++)
            {
                result[i] = ReadMemoryDouble(address + i * 8);
            }
            return result;
        }
        internal void SaveMemory(ulong word, int address)
        {
            Byte[] bytes =
            {
                (Byte)((word >> 56) & 0x00000000000000FF),(Byte)((word >> 48) & 0x00000000000000FF),
                (Byte)((word >> 40) & 0x00000000000000FF),(Byte)((word >> 32) & 0x00000000000000FF),
                (Byte)((word >> 24) & 0x00000000000000FF),(Byte)((word >> 16) & 0x00000000000000FF),
                (Byte)((word >> 8) & 0x00000000000000FF),(Byte)(word & 0x00000000000000FF)
            };
            for (int i = 0; i < bytes.Length; i++)
            {
                memory[address + i] = bytes[i];
            }
        }
        internal void SaveMemory(double fword, int address)
        {
            long bits = BitConverter.DoubleToInt64Bits(fword);
            Byte[] bytes =
            {
                (Byte)((bits >> 56) & 0x00000000000000FF),(Byte)((bits >> 48) & 0x00000000000000FF),
                (Byte)((bits >> 40) & 0x00000000000000FF),(Byte)((bits >> 32) & 0x00000000000000FF),
                (Byte)((bits >> 24) & 0x00000000000000FF),(Byte)((bits >> 16) & 0x00000000000000FF),
                (Byte)((bits >> 8) & 0x00000000000000FF),(Byte)(bits & 0x00000000000000FF)
            };
            for (int i = 0; i < bytes.Length; i++)
            {
                memory[address + i] = bytes[i];
            }
        }
        internal bool WriteByte(int index, byte b)
        {
            if (index > 0 && index < memory.Length)
            {
                memory[index] = b;
                return true;
            }
            return false;
        }
        internal bool GetByte(int index, out byte b)
        {
            if (index > 0 && index < memory.Length)
            {
                b = memory[index];
                return true;
            }
            b = byte.MaxValue;
            return false;
        }
        internal char ReadCharMemory(int address)
        {
            Byte[] character =
            {
                memory[address],memory[address + 1]
            };
            char result = Encoding.Unicode.GetString(character)[0];

            return result;
        }
        internal void Reset()
        {
            memory = new byte[MemoryChip.size];
        }
    }
}
