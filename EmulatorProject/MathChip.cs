using System;
using System.Collections.Generic;
using System.Text;

namespace EmulatorProject
{
    internal class MathChip
    {
        internal double[] registers; //64-bits double precision registers, f0 to f11
        byte flags;
        MemoryChip memory;
        CPU mainProcessor;

        internal MathChip(MemoryChip memory)
        {
            this.memory = memory;
            flags = 0;
            registers = new double[12];
        }
        internal void ConnectToCPU(CPU main)
        {
            this.mainProcessor = main;
        }
        internal bool ExecuteInstruccion(Instruction current)
        {
            switch (current.opcode)
            {
                case Opcode.ADDF:
                    int reg1 = current.b2;
                    int reg2 = current.b3;
                    int regDest = current.b4;

                    registers[regDest] = registers[reg1] + registers[reg2];
                    break;
                case Opcode.SUBF:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    registers[regDest] = registers[reg1] - registers[reg2];
                    break;
                case Opcode.MULF:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    registers[regDest] = registers[reg1] * registers[reg2];
                    break;
                case Opcode.DIVF:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;
                    if (registers[reg2] == 0)
                    {
                        //mainProcessor.flags = (byte)(mainProcessor.flags | 0b00000100);
                        mainProcessor.RaiseInterrupt(0);
                        registers[regDest] = double.NaN;
                    }
                    else
                        registers[regDest] = registers[reg1] / registers[reg2];
                    break;
                case Opcode.ADDFI:
                    int regOrg = current.b2;
                    regDest = current.b3;
                    double inmediate = ReadInmediate(current);

                    registers[regDest] = registers[regOrg] + inmediate;
                    break;
                case Opcode.SUBFI:
                    regOrg = current.b2;
                    regDest = current.b3;
                    inmediate = ReadInmediate(current);

                    registers[regDest] = registers[regOrg] - inmediate;
                    break;
                case Opcode.MULFI:
                    regOrg = current.b2;
                    regDest = current.b3;
                    inmediate = ReadInmediate(current);

                    registers[regDest] = registers[regOrg] * inmediate;
                    break;
                case Opcode.DIVFI:
                    regOrg = current.b2;
                    regDest = current.b3;
                    inmediate = ReadInmediate(current);

                    if (inmediate == 0)
                    {
                        mainProcessor.RaiseInterrupt(2);
                        registers[regDest] = double.NaN;
                    }
                    else
                        registers[regDest] = registers[regOrg] / inmediate;
                    break;
                case Opcode.SQRF:
                    regOrg = current.b2;
                    regDest = current.b3;
                    if (registers[regOrg] < 0)
                    {
                        mainProcessor.RaiseInterrupt(2);
                        registers[regDest] = double.NaN;
                    }
                    else
                        registers[regDest] = Math.Sqrt(regOrg);
                    break;
                case Opcode.POWF:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;
                    registers[regDest] = Math.Pow(registers[reg1],registers[reg2]);
                    break;
                case Opcode.COSF:
                    regOrg = current.b2;
                    regDest = current.b3;

                    registers[regDest] = Math.Cos(regOrg);
                    break;
                case Opcode.SENF:
                    regOrg = current.b2;
                    regDest = current.b3;

                    registers[regDest] = Math.Sin(regOrg);
                    break;
                case Opcode.TANF:
                    regOrg = current.b2;
                    regDest = current.b3;

                    registers[regDest] = Math.Tan(regOrg);
                    break;
                case Opcode.INVF:
                    regOrg = current.b2;
                    regDest = current.b3;

                    if (registers[regOrg] == 0)
                    {
                        mainProcessor.RaiseInterrupt(2);
                        registers[regDest] = double.NaN;
                    }
                    else
                        registers[regDest] = 1 / registers[regOrg];
                    break;
                case Opcode.EXPF:
                    regOrg = current.b2;
                    regDest = current.b3;

                    registers[regDest] = Math.Exp(regOrg);
                    break;
                case Opcode.SEQF:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    if (registers[reg1] == registers[reg2])
                        registers[regDest] = 1;
                    else
                        registers[regDest] = 0;
                    break;
                case Opcode.SNQF:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    if (registers[reg1] != registers[reg2])
                        registers[regDest] = 1;
                    else
                        registers[regDest] = 0;
                    break;
                case Opcode.SLTF:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    if (registers[reg1] < registers[reg2])
                        registers[regDest] = 1;
                    else
                        registers[regDest] = 0;
                    break;
                case Opcode.SGTF:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    if (registers[reg1] > registers[reg2])
                        registers[regDest] = 1;
                    else
                        registers[regDest] = 0;
                    break;
                case Opcode.SNAN:
                    regOrg = current.b2;
                    regDest = current.b3;

                    registers[regDest] = (registers[regOrg] == double.NaN) ? 1 : 0;
                    break;
                case Opcode.MOVF:
                    regOrg = current.b2;
                    regDest = current.b3;

                    registers[regDest] = registers[regOrg];
                    break;
                case Opcode.MOVFI:
                    regDest = current.b2;
                    inmediate = ReadInmediate(current);

                    registers[regDest] = inmediate;
                    break;
                case Opcode.LFM:
                    regDest = current.b2;
                    regOrg = current.b3;
                    int offset = ReadInt(current);

                    ulong address = (ulong)(offset + (long)((regOrg != 255) ? registers[regOrg] : 0));
                    if (address < 128 || address > MemoryChip.size)
                        return Die("Tried to access an invalid memory address");
                    registers[regDest] = memory.ReadMemoryDouble((int)address);
                    break;
                case Opcode.SFM:
                    regOrg = current.b2;
                    int reg = current.b3;
                    offset = ReadInt(current);

                    address = (ulong)(offset + (long)((reg != 255) ? registers[reg] : 0));
                    if (address < 128 || address > MemoryChip.size)
                        return Die("Tried to access an invalid memory address");
                    memory.SaveMemory(registers[regOrg], (int)address);
                    break;
            }
            return true;
        }
        internal bool Die(string message)
        {
            mainProcessor.executing = false;
            mainProcessor.msg = "Error: " + message + "while processing coprocessor instruction";
            return false;
        }
        internal Dictionary<string,double> GetMPUView()
        {
            var view = new Dictionary<string, double>();
            for (int i = 0; i < 12; i++)
            {
                string name = "d" + i;
                view.Add(name, registers[i]);
            }
            return view;
        }
        internal string PrintMPU()
        {
            string res = String.Empty;
            for (int i = 0; i < 12; i++)
            {
                res += "d" + i + ":" + registers[i] + " (" + (long)registers[i] + ")" + "\n";
            }
            res += "flags: " + Convert.ToString(flags, 2).PadLeft(8, '0') + "\n";
            return res;
        }
        internal void Reset()
        {
            registers = new double[12];
            flags = 0;
        }
        private double ReadInmediate(Instruction current)
        {
            long inmediate = ((current.b5 << 24) + (current.b6 << 16)
                            + (current.b7 << 8) + current.b8);
            return BitConverter.Int64BitsToDouble(inmediate);
        }
        private int ReadInt(Instruction current)
        {
            int inmediate = ((int)current.b5 << 24) + ((int)current.b6 << 16)
                            + ((int)current.b7 << 8) + (int)current.b8;
            return inmediate;
        }
    }
}
