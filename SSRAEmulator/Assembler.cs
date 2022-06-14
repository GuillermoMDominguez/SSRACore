using System;
using System.Collections.Generic;
using System.Text;

namespace SSRAEmulator
{
    internal class Assembler
    {
        List<Instruction> program;
        List<Byte> programData;
        Dictionary<string, int> data; //Labels
        int[] jumpTable;
        internal bool correctProgram;
        const int MaxMemAddress = MemoryChip.size;
        static readonly Dictionary<string, Byte> alias = new Dictionary<string, Byte>()
        {
            {"$g0",0 },{"$g1",1},{"$g2",2},{"$g3",3},{"$g4",4},{"$g5",5},{"$g6",6},{"$g7",7},{"$a0",8},
            {"$a1",9},{"$a2",10},{"$a3",11},{"$s0",12},{"$s1",13},{"$s2",14},{"$s3",15},
            {"$sp",16},{"$fp",17},{"$ra",18},{"$ac",19 },{"$zero",255}
        };
        static readonly Dictionary<string, Byte> aliasF = new Dictionary<string, Byte>()
        {
            {"$d0",0 },{"$d1",1 },{"$d2",2 },{"$d3",3 },{"$d4",4 },{"$d5",5 },{"$d6",6 },{"$d7",7 },
            {"$d8",8 },{"$d9",9 },{"$d10",10 },{"$d11",11 }
        };
        static readonly Dictionary<string, int> jumpPoints = new Dictionary<string, int>()
        {
            {"main:",0},{"isr_0:",1},{"isr_1:",2},{"isr_2:",3},{"isr_3:",4},{"isr_4:",5},{"isr_5:",6},{"isr_6:",7},{"isr_7:",8}
        };
        internal Assembler()
        {
            program = new List<Instruction>();
            programData = new List<byte>();
            data = new Dictionary<string, int>();
            jumpTable = new int[]
            {
                0,-1,-1,-1,-1,-1,-1,-1,-1
            };
            correctProgram = false;
        }
        internal void EmitNoArgs(Opcode opcode)
        {
            Instruction ins = new Instruction(opcode, new byte[7]);
            program.Add(ins);
        }
        internal void EmitInmediate(Opcode opcode, int inmediate)
        {
            Byte[] bytes =
{
                0,0,0,
                (byte)((inmediate & 0xFF000000) >> 24),
                (byte)((inmediate & 0x00FF0000) >> 16),
                (byte)((inmediate & 0x0000FF00) >> 8),
                (byte)((inmediate & 0x000000FF))
            };
            Instruction ins = new Instruction(opcode, bytes);
            program.Add(ins);
        }
        internal void EmitInmediate(Opcode opcode, byte arg1, int inmediate)
        {
            Byte[] bytes =
            {
                arg1,0,0,
                (byte)((inmediate & 0xFF000000) >> 24),
                (byte)((inmediate & 0x00FF0000) >> 16),
                (byte)((inmediate & 0x0000FF00) >> 8),
                (byte)((inmediate & 0x000000FF))
            };
            Instruction ins = new Instruction(opcode, bytes);
            program.Add(ins);
        }
        internal void EmitInmediate(Opcode opcode,byte arg1,double inmediate)
        {
            long fword = BitConverter.DoubleToInt64Bits(inmediate);
            Byte[] bytes =
{
                arg1,0,0,
                (byte)((fword & 0xFF000000) >> 24),
                (byte)((fword & 0x00FF0000) >> 16),
                (byte)((fword & 0x0000FF00) >> 8),
                (byte)((fword & 0x000000FF))
            };
            Instruction ins = new Instruction(opcode, bytes);
            program.Add(ins);
        }
        internal void EmitInmediate(Opcode opcode, byte arg1, byte arg2, int inmediate)
        {
            Byte[] bytes =
            {
                arg1,arg2,0,
                (byte)((inmediate & 0xFF000000) >> 24),
                (byte)((inmediate & 0x00FF0000) >> 16),
                (byte)((inmediate & 0x0000FF00) >> 8),
                (byte)((inmediate & 0x000000FF))
            };
            Instruction ins = new Instruction(opcode, bytes);
            program.Add(ins);
        }
        internal void Emit2Args(Opcode opcode, byte arg1, byte arg2)
        {
            Byte[] bytes =
            {
                arg1,arg2,0,0,0,0,0
            };
            Instruction ins = new Instruction(opcode, bytes);
            program.Add(ins);
        }
        internal void Emit3Args(Opcode opcode, byte arg1, byte arg2, byte arg3)
        {
            Byte[] bytes =
            {
                arg1,arg2,arg3,
                0,
                0,
                0,
                0
            };
            Instruction ins = new Instruction(opcode, bytes);
            program.Add(ins);
        }
        internal void Emit3Args(Opcode opcode, byte arg1, byte arg2, int arg3)
        {
            Byte[] bytes =
            {
                arg1,arg2,0,
                (byte)((arg3 & 0xFF000000) >> 24),
                (byte)((arg3 & 0x00FF0000) >> 16),
                (byte)((arg3 & 0x0000FF00) >> 8),
                (byte)((arg3 & 0x000000FF))
            };
            Instruction ins = new Instruction(opcode, bytes);
            program.Add(ins);
        }
        internal void Emit3Args(Opcode opcode,byte arg1,byte arg2,double arg3)
        {
            long inmediate = BitConverter.DoubleToInt64Bits(arg3);
            Byte[] bytes =
{
                arg1,arg2,0,
                (byte)((inmediate & 0xFF000000) >> 24),
                (byte)((inmediate & 0x00FF0000) >> 16),
                (byte)((inmediate & 0x0000FF00) >> 8),
                (byte)((inmediate & 0x000000FF))
            };
            Instruction ins = new Instruction(opcode, bytes);
            program.Add(ins);
        }
        internal void Emit3Args(Opcode opcode, byte arg1, byte arg2, uint arg3)
        {
            Byte[] bytes =
            {
                arg1,arg2,0,
                (byte)((arg3 & 0xFF000000) >> 24),
                (byte)((arg3 & 0x00FF0000) >> 16),
                (byte)((arg3 & 0x0000FF00) >> 8),
                (byte)((arg3 & 0x000000FF))
            };
            Instruction ins = new Instruction(opcode, bytes);
            program.Add(ins);
        }
        internal string ParseSource(String[] source)
        {
            string assemblyResult = String.Empty;
            int dataStart = -1, codeStart = -1;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i].StartsWith(".data"))
                {
                    dataStart = i + 1;
                    break;
                }
            }
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i].StartsWith(".code"))
                {
                    codeStart = i + 1;
                    break;
                }
            }
            if (codeStart == -1) //The .code directive is optional if there isn't a dataSegment
            {
                if (dataStart == -1)
                    codeStart = 0;
                else
                    return "[ASB01]: Program must use .code directive to mark the start of executable instructions";
            }
            data = new Dictionary<string, int>();
            data.Add("IO_1", 128);
            data.Add("IO_2", 136);
            data.Add("IO_3", 144);
            data.Add("IO_4", 152);
            data.Add("IO_5", 160);
            data.Add("IO_6", 168);
            data.Add("IO_7", 176);
            data.Add("IO_8", 184);
            data.Add("IO_C", 192);
            data.Add("IO_INT", 248);
            if (dataStart > -1)
            {
                int dataEnd = (dataStart < codeStart) ? codeStart - 1 : source.Length;
                string[] data = new string[dataEnd - dataStart];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = source[dataStart + i];
                }
                assemblyResult += ParseData(data, dataStart);
            }
            int codeEnd = (codeStart < dataStart) ? dataStart - 1 : source.Length;
            string[] code = new string[codeEnd - codeStart];
            for (int i = 0; i < code.Length; i++)
            {
                code[i] = source[codeStart + i];
            }
            assemblyResult += ParseCode(code, codeStart);
            return assemblyResult;
        }
        private string ParseData(String[] source, int textStart)
        {
            /*
             * Data segment assembly, generates the static data segment for the program, accepted data types are
             * byte, half-word(signed 32-bits), word(signed 64-bits), string(UTF-16 encoded text),
             * and stringz(UTF-16 encoded text with a null byte apended at the end)
             * 
             */
            int startPointer = 256; //Start of program defined memory, we track how much we have used to check for memory limits
            int textline = textStart;
            string assemblyResult = String.Empty;
            List<byte> bytes = new List<byte>(); //Memory allocated by the program

            foreach (String current in source)
            {
                textline++;
                if (current == String.Empty || current.StartsWith('#'))
                    continue;
                if(startPointer > MaxMemAddress)
                {
                    assemblyResult += Die(textline, current, "[ASB07] Program allocates more than the allowed memory (" + (MaxMemAddress + 1)
                        + "bytes)");
                }
                string[] text = current.Split(':',2, StringSplitOptions.RemoveEmptyEntries);
                string dataString = current;
                if (text.Length > 1)
                {
                    string label = text[0];
                    dataString = text[1];
                    data.Add(label, startPointer);
                }
                string[] splitted = dataString.Split(' ', 2,StringSplitOptions.RemoveEmptyEntries);
                if (splitted.Length != 2)
                {
                    assemblyResult += Die(textline, current, "[ASB02] Incorrect data directive format, expected .type value1,value2..");
                    continue;
                }
                
                switch (splitted[0].Trim().ToLowerInvariant())
                {
                    case ".byte":
                        string[] values = splitted[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < values.Length; i++)
                        {
                            if (!Byte.TryParse(values[i], out byte value))
                            {
                                assemblyResult += Die(textline, current, "[ASB04] Expected byte numerical literal");
                                continue;
                            }
                            bytes.Add(value);
                            startPointer++;
                        }
                        break;
                    case ".half":
                        values = splitted[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < values.Length; i++)
                        {
                            if (!int.TryParse(values[i], out int value))
                            {
                                assemblyResult += Die(textline, current, "[ASB05] Expected 32-bit signed numerical literal");
                                continue;
                            }
                            Byte[] number =
                                {
                                (Byte)((value >> 24) & 0x00000000000000FF),(Byte)((value >> 16) & 0x00000000000000FF),
                                (Byte)((value >> 8) & 0x00000000000000FF),(Byte)(value & 0x00000000000000FF)
                            };
                            foreach(Byte b in number)
                            {
                                bytes.Add(b);
                            }
                            startPointer += 4;
                        }
                        break;
                    case ".word":
                        values = splitted[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < values.Length; i++)
                        {
                            if (!long.TryParse(values[i], out long value))
                            {
                                assemblyResult += Die(textline, current, "[ASB06] Expected 64-bit signed numerical literal");
                                continue;
                            }
                            Byte[] number =
                            {
                                (Byte)((value >> 56) & 0x00000000000000FF),(Byte)((value >> 48) & 0x00000000000000FF),
                                (Byte)((value >> 40) & 0x00000000000000FF),(Byte)((value >> 32) & 0x00000000000000FF),
                                (Byte)((value >> 24) & 0x00000000000000FF),(Byte)((value >> 16) & 0x00000000000000FF),
                                (Byte)((value >> 8) & 0x00000000000000FF),(Byte)(value & 0x00000000000000FF)
                               };
                            foreach (Byte b in number)
                            {
                                bytes.Add(b);
                            }
                            startPointer += 8;
                        }
                        break;
                    case ".string":
                    case ".stringz":
                        string literalS = splitted[1].Trim(); //[ASB07] Only one string literal is allowed per directive
                        if(!literalS.StartsWith('"') || !literalS.EndsWith('"'))
                        {
                            assemblyResult += Die(textline, current, "[ASB08] String must be enclosed in double quotes '\"'");
                            break;
                        }
                        string ascii = literalS.Substring(1, literalS.Length - 2); //Actually, utf16
                        Byte[] encoding = Encoding.Unicode.GetBytes(ascii); //UTF-16 little endian
                        foreach (byte b in encoding)
                        {
                            bytes.Add(b);
                            startPointer++; //This is here for enconding purposes
                        }
                        if (splitted[0].Trim().ToLowerInvariant() == ".stringz")
                        {
                            //We add the null byte at the end of the string
                            Byte[] nullChar = Encoding.Unicode.GetBytes("\0"); //I hate text
                            foreach (Byte b in nullChar)
                            {
                                bytes.Add(b);
                                startPointer++;
                            }
                        }
                        break;
                    case ".block":
                        values = splitted[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (values.Length > 1)
                            assemblyResult += Die(textline, current, "[ASB08] Expected number literal indicating number of bytes");
                        else
                        {
                            string value = values[0];
                            if(!uint.TryParse(value,out uint quantity))
                            {
                                assemblyResult += Die(textline, current, "[ASB09] Expected positive integer as argument to .block directive");
                                break;
                            }
                            startPointer += (int)quantity;
                            for(int i = 0; i < quantity;i++)
                            {
                                bytes.Add(0);
                            }
                        }
                        break;
                    case ".align":
                        values = splitted[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (values.Length > 1)
                            assemblyResult += Die(textline, current, "[ASB10] Expected number literal indicating aligment boundary");
                        else
                        {
                            //We add padding zeroes to align the next data
                            string value = values[0];
                            if (!uint.TryParse(value, out uint quantity))
                            {
                                assemblyResult += Die(textline, current, "[ASB11] Expected positive integer as argument to .align directive");
                                break;
                            }
                            int alignment = 1 << (int)quantity; //align with 2^n boundary
                            int remain = (startPointer < alignment) ? alignment - startPointer : startPointer % alignment;
                            startPointer += remain;
                            for (int i = 0; i < remain; i++)
                            {
                                bytes.Add(0);
                            }
                        }
                        break;
                    case ".double":
                        values = splitted[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < values.Length; i++)
                        {
                            if (!double.TryParse(values[i], out double fvalue))
                            {
                                assemblyResult += Die(textline, current, "[ASB12] Expected 64-bit double precision floating point literal");
                                continue;
                            }
                            long bits = BitConverter.DoubleToInt64Bits(fvalue);
                            Byte[] number =
                            {
                                (Byte)((bits >> 56) & 0x00000000000000FF),(Byte)((bits >> 48) & 0x00000000000000FF),
                                (Byte)((bits >> 40) & 0x00000000000000FF),(Byte)((bits >> 32) & 0x00000000000000FF),
                                (Byte)((bits >> 24) & 0x00000000000000FF),(Byte)((bits >> 16) & 0x00000000000000FF),
                                (Byte)((bits >> 8) & 0x00000000000000FF),(Byte)(bits & 0x00000000000000FF)
                               };
                            foreach (Byte b in number)
                            {
                                bytes.Add(b);
                            }
                            startPointer += 8;
                        }
                        break;
                    default:
                        assemblyResult += Die(textline, current, "[ASB03] Unknown data type");
                        break;
                }
            }
            programData = bytes;
            return assemblyResult;
        }

        private string ParseCode(String[] source, int textStart)
        {
            int line = 0;
            int textline = textStart;
            string res = String.Empty;
            this.Reset();
            Dictionary<string, int> labels = new Dictionary<string, int>();
            correctProgram = true;

            /*
             * Instruction segment assembly, the assembler uses a 2-pass process, the first pass computes labels addresses,
             * the second pass generates the execution code
             */
            //First pass, calculate label offsets
            foreach (String current in source)
            {
                if (current.StartsWith("#") || current == String.Empty)
                    continue;
                if (!current.Contains(':'))
                {
                    line++;
                    continue;
                }
                labels.Add(current[0..^1], line + 1);
                if (jumpPoints.ContainsKey(current.ToLower()))
                    jumpTable[jumpPoints[current]] = line;
            }
            String assemblyResult = String.Empty;
            line = 0;
            foreach (String current in source)
            {
                textline++;
                if (current == String.Empty || current.StartsWith("#") || current.Contains(':'))
                    continue; //Ignore empty lines and labels
                line++;
                int inlineComment = current.IndexOf('#');
                string actual;
                if (inlineComment > 0)
                    actual = current.Substring(0, inlineComment);
                else
                    actual = current;
                char[] separators = { ',', ' ' };
                String[] splitted = actual.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                if (splitted.Length < 0 || splitted.Length > 4)
                    assemblyResult += Die(textline, current, "Incorrect instruction format");
                Opcode opcode;
                if (!Enum.TryParse(splitted[0].ToUpper(), out opcode))
                    assemblyResult += Die(textline, current, "Unknown or unsupported Operation Code");
                switch (opcode)
                {
                    case Opcode.NOP:
                    case Opcode.HALT:
                    case Opcode.RST:
                    case Opcode.SYSCALL:
                        EmitNoArgs(opcode);
                        break;
                    case Opcode.MOV:
                    case Opcode.NOT:
                        if (splitted.Length != 3)
                            assemblyResult += Die(line, current, "Incorrect instruction format, expected opc,rOrg,rDest");
                        Byte bOrg, bDest;
                        res = ParseRegister(splitted[1], current, textline, out bOrg);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseRegister(splitted[2], current, textline, out bDest);
                        if (res != String.Empty)
                            assemblyResult += res;
                        Emit2Args(opcode, bOrg, bDest);
                        break;
                    case Opcode.SEQ:
                    case Opcode.SNQ:
                    case Opcode.SGT:
                    case Opcode.SLT:
                        if (splitted.Length != 3)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,reg1,reg2");
                        res = ParseRegister(splitted[1], current, textline, out bOrg);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseRegister(splitted[2], current, textline, out bDest);
                        if (res != String.Empty)
                            assemblyResult += res;
                        Emit2Args(opcode, bOrg, bDest);
                        break;
                    case Opcode.CHK:
                        if (splitted.Length != 3)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,regDest,flag");
                        res = ParseRegister(splitted[1], current, textline, out bOrg);
                        if (res != String.Empty)
                            assemblyResult += res;
                        if (!Byte.TryParse(splitted[3], out bDest))
                            assemblyResult += Die(textline, current, "Incorrect argument, expected number literal between 0 and 7");
                        Emit2Args(opcode, bOrg, bDest);
                        break;
                    case Opcode.ADD:
                    case Opcode.SUB:
                    case Opcode.MUL:
                    case Opcode.DIV:
                    case Opcode.MOD:
                    case Opcode.AND:
                    case Opcode.OR:
                    case Opcode.XOR:
                        if (splitted.Length != 4)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,r1,r2,rDest");
                        Byte b1, b2, b3;
                        res = ParseRegister(splitted[1], current, textline, out b1);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseRegister(splitted[2], current, textline, out b2);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseRegister(splitted[3], current, textline, out b3);
                        if (res != String.Empty)
                            assemblyResult += res;
                        /*
                        if (!Byte.TryParse(splitted[1], out b1) || !Byte.TryParse(splitted[2],out b2) || !Byte.TryParse(splitted[3],out b3))
                            return Die(line, current, "Incorrect argument, expected register identifier");
                        */
                        Emit3Args(opcode, b1, b2, b3);
                        break;
                    case Opcode.ADDI:
                    case Opcode.SUBI:
                    case Opcode.MULTI:
                    case Opcode.DIVI:
                    case Opcode.MODI:
                        if (splitted.Length != 4)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,rOrg,rDest,inmediate");
                        res = ParseRegister(splitted[1], current, textline, out bOrg);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseRegister(splitted[2], current, textline, out bDest);
                        if (res != String.Empty)
                            assemblyResult += res;
                        if (!int.TryParse(splitted[3], out int inmediate))
                            assemblyResult += Die(textline, current, "Incorrect argument, expected number literal");
                        Emit3Args(opcode, bOrg, bDest, inmediate);
                        break;
                    case Opcode.ANDI:
                    case Opcode.ORI:
                    case Opcode.XORI:
                    case Opcode.LSB:
                    case Opcode.RSB:
                        if (splitted.Length != 4)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,rOrg,rDest,inmediate");
                        res = ParseRegister(splitted[1], current, textline, out bOrg);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseRegister(splitted[2], current, textline, out bDest);
                        if (res != String.Empty)
                            assemblyResult += res;
                        if (!uint.TryParse(splitted[3], out uint uinmediate))
                            assemblyResult += Die(textline, current, "Incorrect argument, expected number literal");
                        Emit3Args(opcode, bOrg, bDest, uinmediate);
                        break;
                    case Opcode.BEQ:
                    case Opcode.BNQ:
                    case Opcode.BGT:
                    case Opcode.BLT:
                        if (splitted.Length != 4)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,reg1,reg2,offset or label");
                        res = ParseRegister(splitted[1], current, textline, out bOrg);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseRegister(splitted[2], current, textline, out bDest);
                        if (res != String.Empty)
                            assemblyResult += res;
                        if (!int.TryParse(splitted[3], out inmediate))
                        {
                            res = CalculateLabelOffset(splitted[3], current, textline, line, labels, out inmediate);
                            if (res != String.Empty)
                                assemblyResult += res;
                        }
                        Emit3Args(opcode, bOrg, bDest, inmediate);
                        break;
                    case Opcode.MOVI:
                        if (splitted.Length != 3)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,rDest,inmediate");
                        res = ParseRegister(splitted[1], current, textline, out bDest);
                        if (res != String.Empty)
                            assemblyResult += res;
                        if (!int.TryParse(splitted[2], out inmediate))
                            assemblyResult += Die(textline, current, "Incorrect argument, expected number literal");
                        EmitInmediate(opcode, bDest, inmediate);
                        break;
                    case Opcode.JMP:
                    case Opcode.JAL:
                    case Opcode.JST:
                        if (splitted.Length != 2)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,offset");
                        if (!int.TryParse(splitted[1], out inmediate))
                        {
                            res = CalculateLabelOffset(splitted[1], current, textline, line, labels, out inmediate);
                            if (res != String.Empty)
                                assemblyResult += res;
                        }
                        EmitInmediate(opcode, inmediate);
                        break;
                    case Opcode.JR:
                        if (splitted.Length != 2)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,register");
                        res = ParseRegister(splitted[1], current, textline, out bDest);
                        if (res != String.Empty)
                            assemblyResult += res;
                        EmitInmediate(opcode, bDest, 0);
                        break;
                    case Opcode.LW:
                    case Opcode.SW:
                        if (splitted.Length != 3)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,register,offset expression");
                        res = ParseRegister(splitted[1], current, textline, out bOrg);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseAddressOffset(splitted[2], current, textline, out Byte regBase, out int offset);
                        if (res != String.Empty)
                            assemblyResult += res;
                        Emit3Args(opcode, bOrg, regBase, offset);
                        break;
                    case Opcode.PUSH:
                    case Opcode.POP:
                    case Opcode.MFH:
                    case Opcode.MFL:
                        if (splitted.Length != 2)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,register");
                        res = ParseRegister(splitted[1], current, textline, out bOrg);
                        if (res != String.Empty)
                            assemblyResult += res;
                        Emit2Args(opcode, bOrg, 0);
                        break;
                    case Opcode.SIR:
                    case Opcode.CLF:
                    case Opcode.BREAK:
                        if (splitted.Length != 2)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,inmediate");
                        if (!int.TryParse(splitted[1], out inmediate))
                            assemblyResult += Die(textline, current, "Expected inmediate number");
                        EmitInmediate(opcode, inmediate);
                        break;
                    case Opcode.LAD:
                        //Pseudoinstruccion, implemented by the assembler as a movi instruccion
                        //This instrucction accepts either a data segment label, or a instruccion line label
                        //If a label refers to both a data segment and an instruccion, the data takes precedence
                        if (splitted.Length != 3)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,register,label");
                        res = ParseRegister(splitted[1], current, textline, out bDest);
                        inmediate = -1;
                        if (res != String.Empty)
                            assemblyResult += res;
                        if (!data.ContainsKey(splitted[2]) && !labels.ContainsKey(splitted[2]))
                            assemblyResult += Die(textline, current, "Unknown label");
                        else
                        {
                            inmediate = data.ContainsKey(splitted[2]) ? data[splitted[2]] : labels[splitted[2]];
                        }
                        EmitInmediate(Opcode.MOVI, bDest, inmediate);
                        break;
                    case Opcode.ACC:
                        //Pseudo instrucction, implemented by the assembly as an add inmediate instrucction
                        //acc inmediate -> addi $ra $ra inmediate
                        if (splitted.Length != 2)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,inmediate");
                        bOrg = 19; bDest = 19;
                        if (!int.TryParse(splitted[1], out inmediate))
                            assemblyResult += Die(textline, current, "Expected inmediate number");
                        Emit3Args(Opcode.ADDI, bOrg, bDest, inmediate);
                        break;
                    case Opcode.ADDF:
                    case Opcode.SUBF:
                    case Opcode.MULF:
                    case Opcode.DIVF:
                    case Opcode.POWF:
                    case Opcode.SEQF:
                    case Opcode.SNQF:
                    case Opcode.SGTF:
                    case Opcode.SLTF:
                        if (splitted.Length != 4)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,r1,r2,rDest");
                        res = ParseDoubleRegister(splitted[1], current, textline, out b1);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseDoubleRegister(splitted[2], current, textline, out b2);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseDoubleRegister(splitted[3], current, textline, out b3);
                        if (res != String.Empty)
                            assemblyResult += res;
                        /*
                        if (!Byte.TryParse(splitted[1], out b1) || !Byte.TryParse(splitted[2],out b2) || !Byte.TryParse(splitted[3],out b3))
                            return Die(line, current, "Incorrect argument, expected register identifier");
                        */
                        Emit3Args(opcode, b1, b2, b3);
                        break;
                    case Opcode.ADDFI:
                    case Opcode.SUBFI:
                    case Opcode.MULFI:
                    case Opcode.DIVFI:
                        if (splitted.Length != 4)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,rOrg,rDest,inmediate");
                        res = ParseDoubleRegister(splitted[1], current, textline, out bOrg);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseDoubleRegister(splitted[2], current, textline, out bDest);
                        if (res != String.Empty)
                            assemblyResult += res;
                        if (!double.TryParse(splitted[3], out double finmediate))
                            assemblyResult += Die(textline, current, "Incorrect argument, expected number literal");
                        Emit3Args(opcode, bOrg, bDest, finmediate);
                        break;
                    case Opcode.SQRF:
                    case Opcode.COSF:
                    case Opcode.SENF:
                    case Opcode.TANF:
                    case Opcode.INVF:
                    case Opcode.EXPF:
                    case Opcode.SNAN:
                    case Opcode.MOVF:
                        if (splitted.Length != 3)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,rOrg,rDest");
                        res = ParseDoubleRegister(splitted[1], current, textline, out bOrg);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseDoubleRegister(splitted[2], current, textline, out bDest);
                        if (res != String.Empty)
                            assemblyResult += res;
                        Emit2Args(opcode, bOrg, bDest);
                        break;
                    case Opcode.MOVFI:
                        if (splitted.Length != 3)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,rDest,inmediate");
                        res = ParseDoubleRegister(splitted[2], current, textline, out bDest);
                        if (res != String.Empty)
                            assemblyResult += res;
                        if (!double.TryParse(splitted[3], out finmediate))
                            assemblyResult += Die(textline, current, "Incorrect argument, expected number literal");
                        EmitInmediate(opcode, bDest, finmediate);
                        break;
                    case Opcode.LFM:
                    case Opcode.SFM:
                        if (splitted.Length != 3)
                            assemblyResult += Die(textline, current, "Incorrect instruction format, expected opc,register,offset expression");
                        res = ParseDoubleRegister(splitted[1], current, textline, out bOrg);
                        if (res != String.Empty)
                            assemblyResult += res;
                        res = ParseAddressOffset(splitted[2], current, textline, out regBase, out offset);
                        if (res != String.Empty)
                            assemblyResult += res;
                        Emit3Args(opcode, bOrg, regBase, offset);
                        break;
                    default:
                        assemblyResult += Die(textline, current, "Unsupported instruction " + Enum.GetName(typeof(Opcode), opcode));
                        break;
                }
            }

            return assemblyResult;
        }
        private string ParseRegister(string text, string source, int textline, out byte register)
        {
            bool number = Byte.TryParse(text, out register);
            if (!number)
            {
                if (alias.ContainsKey(text))
                    register = alias[text];
                else
                    return Die(textline, source, "Unknown register alias");
            }
            if (register > 19 || register < 0)
                return Die(textline, source, "Incorrect register specifier");
            return String.Empty;
        }
        private string ParseDoubleRegister(string text, string source, int textline, out byte register)
        {
            bool number = Byte.TryParse(text, out register);
            if (!number)
            {
                if (aliasF.ContainsKey(text))
                    register = aliasF[text];
                else
                    return Die(textline, source, "Unknown register alias");
            }
            if (register > 12 || register < 0)
                return Die(textline, source, "Incorrect register specifier");
            return String.Empty;
        }
        private string ParseAddressOffset(string text, string source, int textline, out byte regBase, out int offset)
        {
            regBase = 255;
            offset = 0;
            if (!text.Contains('('))
            {
                bool number = int.TryParse(text, out offset);
                if (!number)
                    return Die(textline, source, "Expected numerical offset");
            }
            else
            {
                string[] split = text.Split('(', StringSplitOptions.RemoveEmptyEntries);
                if (split.Length != 2)
                    return Die(textline, source, "Incorrect address expression");
                bool number = int.TryParse(split[0], out offset);
                if (!number)
                    return Die(textline, source, "Expected numerical offset");
                string res = ParseRegister(split[1].Substring(0, split[1].Length - 1), source, textline, out regBase);
                if (res != String.Empty)
                    return res;
            }
            return String.Empty;
        }
        private string CalculateLabelOffset(string text, string source, int textline, int line, Dictionary<string, int> labels, out int offset)
        {
            string label = text.Trim();
            offset = 0;
            if (!labels.ContainsKey(label))
                return Die(textline, source, "Unknown label on instruction, expected correct label or number literal");
            offset = labels[label] - line - 1;
            return String.Empty;
        }
        internal string Die(int line, string source, string message)
        {
            correctProgram = false;
            String res = "Line " + line + ": " + message + ", instruction: " + source + "\n";
            return res;
        }
        internal void Reset()
        {
            correctProgram = false;
            jumpTable = new int[]
            {
                0,-1,-1,-1,-1,-1,-1,-1,-1
            };
            program.Clear();
        }
        internal CProgram GetProgram(string name)
        {
            return new CProgram(program.ToArray(), programData.ToArray(),name,jumpTable);
        }
        internal string PrintProgram()
        {
            string res = "Program " + program.Count + " lines\n" +
                         "----------------\n";
            int i = 0;
            foreach (Instruction ins in program)
            {
                res += i + ": " + Instruction.PrintInstruction(ins);
                i++;
            }
            res += "----------------\n";
            return res;
        }
    }
}