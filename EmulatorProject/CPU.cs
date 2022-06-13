using System;
using System.Collections.Generic;
using System.Text;

namespace EmulatorProject
{
    internal class CPU
    {
        internal ulong[] registers; /*16 64-bit register, g0 to g7 and a0 to a3 for passing args, s0 to s3 saved between calls
                                     * 16: sp,17:fp,18:ra,19: acc*/
        internal ulong ip; /*Instruction pointer */
        internal ulong dp; /*Data pointer, for dynamic memory allocation */
        internal ulong hi, low; /* for overflow multiplication */
        internal byte flags; /*8-bit cpu register flags bit 0: division por cero, bit 1: Error argumentos,bit 2: Error fpu,bit 3: Interrupción IO*/
        internal ulong[] rngState; /* 256-bit register for pseudorng number generation */
        internal MemoryChip memory; //Principal memory, to be replaced for an emulator, little endian
        internal MathChip coprocessor; //Math coprocessor, for floating point operations
        internal Instruction[] program; //Instruction memory, to be integrated in mmu, maybe
        internal int[] routinesTable;
        internal bool executing = false;
        bool handleInterrupts = false;
        int dynamicMemStart;

        //External interfaces
        internal readonly TerminalInterface terminal;
        internal string msg
        {
            get; set;
        }

        internal CPU(TerminalInterface terminal,MemoryChip memory,MathChip coprocessor)
        {
            registers = new ulong[20];
            registers[16] = 4095; //Initialize sp register to start of stack
            dp = 256; //Start of user available memory
            ip = 0; hi = 0; low = 0; 
            flags = 0;
            this.memory = memory;
            this.coprocessor = coprocessor;
            rngState = new ulong[4];
            this.terminal = terminal;
            msg = string.Empty;
        }
        internal void LoadProgram(CProgram program)
        {
            this.program = program.instructions;
            this.routinesTable = program.jumpTable;

            dp = 256 + (ulong)program.dataSegment.Length; //Start of program memory
            dynamicMemStart = (int)dp;
            memory.LoadData(program.dataSegment, 256);
        }

        internal bool Run()
        {
            executing = true;
            handleInterrupts = true;
            ip = (ulong)routinesTable[0]; //Set the ip to the address of the entry point
            flags = 0;
            bool result = true;
            while (executing && (int)ip < program.Length)
            {
                result = StepInstruction();
            }
            return result;
        }
        internal bool StepInstruction()
        {
            Instruction current = program[ip];
            ip++;
            bool result = true;
            if (current.opcode <= Opcode.BREAK || current.opcode == Opcode.HALT)
                result = ExecuteStep(current);
            else
                result = coprocessor.ExecuteInstruccion(current);
            if(flags != 0) //Dispatch a interrupt subroutine
            {
                //This bit is not CLS-compliant, may be subtituted by Math.Log2(Double)
                //int mostSignificantPosition = (flags == 1) ? 1 : (int)Math.Floor(Math.Log2(flags - 1));                
                int mostSignificantPosition = System.Numerics.BitOperations.Log2((uint)flags);
                //Push current ip to the stack, and set execution to the isr
                if(handleInterrupts && routinesTable[mostSignificantPosition + 1] > 0)
                {
                    registers[16] = registers[16] - 8;
                    memory.SaveMemory(ip, (int)registers[16]);
                    ip = (ulong)routinesTable[mostSignificantPosition + 1];
                    flags = (byte)(flags & (byte)~(1 << (mostSignificantPosition)));
                }
            }
            return result;
        }
        internal bool ExecuteStep(Instruction current)
        {
            switch (current.opcode)
            {
                case Opcode.NOP:
                    break;
                case Opcode.ADD:
                    int reg1 = current.b2;
                    int reg2 = current.b3;
                    int regDest = current.b4;

                    registers[regDest] = registers[reg1] + registers[reg2];
                    break;
                case Opcode.ADDI:
                    int regOrg = current.b2;
                    regDest = current.b3;

                    int inmediate = ReadInt(current);

                    registers[regDest] = (ulong)((long)registers[regOrg] + inmediate);
                    break;
                case Opcode.SUB:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    registers[regDest] = registers[reg1] - registers[reg2];
                    break;
                case Opcode.SUBI:
                    regOrg = current.b2;
                    regDest = current.b3;
                    inmediate = ReadInt(current);

                    registers[regDest] = (ulong)((long)registers[regOrg] - inmediate);
                    break;
                case Opcode.MUL:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    registers[regDest] = registers[reg1] * registers[reg2];
                    break;
                case Opcode.MULT:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    System.Numerics.BigInteger result = registers[reg1] * registers[reg2];
                    hi = (ulong)(result >> 64);
                    low = (ulong)(result);
                    break;
                case Opcode.MULTI:
                    regOrg = current.b2;
                    regDest = current.b3;
                    inmediate = ReadInt(current);

                    registers[regDest] = (ulong)((long)registers[regOrg] * inmediate);
                    break;
                case Opcode.DIV:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    ulong divisor = registers[reg2];
                    if (divisor == 0)
                    {
                        RaiseInterrupt(0);
                    }
                    else
                    {
                        registers[regDest] = registers[reg1] / registers[reg2];
                    }
                    break;
                case Opcode.DIVI:
                    regOrg = current.b2;
                    regDest = current.b3;

                    inmediate = ReadInt(current);
                    if (inmediate == 0)
                    {
                        RaiseInterrupt(0);
                    }
                    else
                    {
                        registers[regDest] = (ulong)((long)registers[regOrg] / inmediate);
                    }
                    break;
                case Opcode.MOD:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    divisor = registers[reg2];
                    if (divisor == 0)
                    {
                        RaiseInterrupt(0);
                    }
                    else
                    {
                        registers[regDest] = registers[reg1] % registers[reg2];
                    }
                    break;
                case Opcode.MODI:
                    regOrg = current.b2;
                    regDest = current.b3;
                    inmediate = ReadInt(current);
                    if (inmediate == 0)
                    {
                        RaiseInterrupt(0);
                    }
                    else
                    {
                        registers[regDest] = registers[regOrg] % (ulong)inmediate;
                    }
                    break;
                case Opcode.MOV:
                    regOrg = current.b2;
                    regDest = current.b3;

                    registers[regDest] = registers[regOrg];
                    break;
                case Opcode.MOVI:
                    int reg = current.b2;
                    ulong uinmediate = (ulong)ReadInt(current);
                    registers[reg] = uinmediate;
                    break;
                case Opcode.AND:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    registers[regDest] = registers[reg1] & registers[reg2];
                    break;
                case Opcode.ANDI:
                    regOrg = current.b2;
                    regDest = current.b3;

                    uinmediate = ReadUInt(current);

                    registers[regDest] = registers[regOrg] & uinmediate;
                    break;
                case Opcode.OR:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    registers[regDest] = registers[reg1] | registers[reg2];
                    break;
                case Opcode.ORI:
                    regOrg = current.b2;
                    regDest = current.b3;

                    uinmediate = ReadUInt(current);

                    registers[regDest] = registers[regOrg] | uinmediate;
                    break;
                case Opcode.XOR:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    registers[regDest] = registers[reg1] ^ registers[reg2];
                    break;
                case Opcode.XORI:
                    regOrg = current.b2;
                    regDest = current.b3;

                    uinmediate = ReadUInt(current);

                    registers[regDest] = registers[regOrg] ^ uinmediate;
                    break;
                case Opcode.NOT:
                    regOrg = current.b2;
                    regDest = current.b3;

                    registers[regDest] = ~registers[regOrg];
                    break;
                case Opcode.LSB:
                    regOrg = current.b2;
                    regDest = current.b3;
                    inmediate = ReadInt(current);

                    registers[regDest] = registers[regOrg] << inmediate;
                    break;
                case Opcode.RSB:
                    regOrg = current.b2;
                    regDest = current.b3;
                    inmediate = ReadInt(current);

                    registers[regDest] = registers[regOrg] >> inmediate;
                    break;
                case Opcode.SEQ:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    if (registers[reg1] == registers[reg2])
                        registers[regDest] = 1;
                    else
                        registers[regDest] = 0;
                    break;
                case Opcode.SNQ:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    if (registers[reg1] != registers[reg2])
                        registers[regDest] = 1;
                    else
                        registers[regDest] = 0;
                    break;
                case Opcode.SLT:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    if (registers[reg1] < registers[reg2])
                        registers[regDest] = 1;
                    else
                        registers[regDest] = 0;
                    break;
                case Opcode.SGT:
                    reg1 = current.b2;
                    reg2 = current.b3;
                    regDest = current.b4;

                    if (registers[reg1] > registers[reg2])
                        registers[regDest] = 1;
                    else
                        registers[regDest] = 0;
                    break;
                case Opcode.CHK:
                    reg1 = current.b2;
                    int flag = current.b3;

                    if (flag > 7 || flag < 0)
                    {
                        RaiseInterrupt(1); //Flag an error and skip to next instruction
                    }
                    else
                    {
                        registers[reg1] = ((flags & (1 << reg1)) != 0) ? (ulong)1 : 0;
                    }
                    break;
                case Opcode.CLF:
                    flag = ReadInt(current);
                    if (flag > 7 || flag < 0)
                    {
                        RaiseInterrupt(1); //Flag an error and skip to next instruction
                    }
                    else
                    {
                        int mask = 1 << flag;
                        flags = (byte)(flags & (~mask));
                    }
                    break;
                case Opcode.BEQ:
                    reg1 = current.b2;
                    reg2 = current.b3;

                    int offset = ReadInt(current);
                    ip = (uint)((long)ip + ((registers[reg1] == registers[reg2]) ? offset : 0));
                    break;
                case Opcode.BNQ:
                    reg1 = current.b2;
                    reg2 = current.b3;

                    offset = ReadInt(current);
                    ip = (uint)((long)ip + ((registers[reg1] != registers[reg2]) ? offset : 0));
                    break;
                case Opcode.BGT:
                    reg1 = current.b2;
                    reg2 = current.b3;

                    offset = ReadInt(current);
                    ip = (uint)((long)ip + ((registers[reg1] > registers[reg2]) ? offset : 0));
                    break;
                case Opcode.BLT:
                    reg1 = current.b2;
                    reg2 = current.b3;

                    offset = ReadInt(current);
                    ip = (ulong)((long)ip + ((registers[reg1] < registers[reg2]) ? offset : 0));
                    break;
                case Opcode.JMP:
                    offset = ReadInt(current);

                    ip = (ulong)((long)ip + offset);
                    break;
                case Opcode.JR:
                    reg = current.b2;
                    ulong address = registers[reg];

                    ip = address;
                    break;
                case Opcode.JAL:
                    offset = ReadInt(current);

                    registers[18] = ip; //Save current ip on ra register, before jump
                    ip = (ulong)((long)ip + offset);
                    break;
                case Opcode.JST:
                    offset = ReadInt(current);

                    registers[16] = registers[16] - 8;
                    memory.SaveMemory(ip, (int)registers[16]); //Push current ip value to the stack
                    ip = (ulong)((long)ip + offset);
                    break;
                case Opcode.RST:
                    ip = memory.ReadMemory((int)registers[16]); //Pop the return value from the stack
                    registers[16] = registers[16] + 8;

                    break;
                case Opcode.LW:
                    regDest = current.b2;
                    reg = current.b3;
                    offset = ReadInt(current);

                    address = (ulong)(offset + (long)((reg != 255) ? registers[reg] : 0));
                    registers[regDest] = memory.ReadMemory((int)address);
                    break;
                case Opcode.SW:
                    regOrg = current.b2;
                    reg = current.b3;
                    offset = ReadInt(current);

                    address = (ulong)(offset + (long)((reg != 255) ? registers[reg] : 0));
                    memory.SaveMemory(registers[regOrg], (int)address); //TODO: Calculate the actual address space
                    break;
                case Opcode.SIR:
                    //Tell the CPU to not handle interrputs if passed a zero argument
                    handleInterrupts = (ReadInt(current) != 0);

                    break;
                case Opcode.SYSCALL:
                    SysOP routine = (SysOP)(int)registers[8];
                    if (routine <= 0)
                        //return Die("Invalid system call code on register a0");
                        return false;
                    return SysRoutines(routine);
                case Opcode.PUSH:
                    regOrg = current.b2;
                    registers[16] = registers[16] - 8;
                    memory.SaveMemory(registers[regOrg], (int)registers[16]);
                    break;
                case Opcode.POP:
                    regDest = current.b2;
                    registers[regDest] = memory.ReadMemory((int)registers[16]);
                    registers[16] = registers[16] + 8;
                    break;
                case Opcode.MFH:
                    regDest = current.b2;
                    registers[regDest] = hi;

                    break;
                case Opcode.MFL:
                    regDest = current.b2;
                    registers[regDest] = low;

                    break;
                case Opcode.BREAK:
                    //Trigger interrupt
                    offset = ReadInt(current);

                    RaiseInterrupt(offset); //The raise interrupt func handles checking for a valid interrupt
                    break;
                case Opcode.HALT:
                    executing = false;
                    break;
                default:
                    return Die("Unknown or unsupported OPCODE " + Enum.GetName(typeof(Opcode), current.opcode));
            }
            return true;
        }
        internal bool SysRoutines(SysOP routine)
        {
            switch(routine)
            {
                case SysOP.PRINT_INT:
                    terminal.Write(((long)registers[9]).ToString());
                    break;
                case SysOP.PRINT_UINT:
                    terminal.Write((registers[9]).ToString());
                    break;
                case SysOP.PRINT_WORD:
                    byte b;
                    if (registers[9] < 0 || registers[9] > MemoryChip.size)
                        return Die("Tried to print a word in an out-of-bounds memory location");
                    for(int i = 0; i < 8; i++)
                    {
                        memory.GetByte((int)(registers[9] + (ulong)i),out b);
                        terminal.Write("B" + i + ": " + b + " ");
                    }
                    terminal.Write("\n");
                    break;
                case SysOP.PRINT_DOUBLE:
                    terminal.Write(coprocessor.registers[0].ToString());
                    break;
                case SysOP.PRINT_CHAR:
                    char character = (char)registers[9];
                    terminal.Write(character.ToString());
                    break;
                case SysOP.PRINT_STRING:
                    int address = (int)registers[9];
                    string toPrint = String.Empty;
                    bool correct = true;
                    while(correct)
                    {
                        char current = memory.ReadCharMemory(address);
                        address += 2;
                        if(current == '\\') //I really hate text
                        {
                            char next = memory.ReadCharMemory(address);
                            switch(next)
                            {
                                case 'n':
                                    toPrint += "\n"; //Yeah
                                    address += 2;
                                    break;
                                case 'r':
                                    toPrint += "\r";
                                    address += 2;
                                    break;
                                case 't':
                                    toPrint += "\t";
                                    address += 2;
                                    break;
                                case '0':
                                    current = '\0';
                                    toPrint += "\0";
                                    address += 2;
                                    break;
                            }
                        }
                        else
                        {
                            toPrint += current;
                        }
                        if (address >= MemoryChip.size - 1)
                            correct = false;
                        if (current == '\0')
                            break;
                    }
                    if (!correct)
                        return Die("Tried to print non-ending string in memory");
                    terminal.Write(toPrint);
                    break;
                case SysOP.READ_INT:
                    registers[9] = (ulong)terminal.ReadInt();
                    break;
                case SysOP.READ_DOUBLE:
                    double number = terminal.ReadDouble();
                    coprocessor.registers[0] = number;
                    break;
                case SysOP.READ_CHAR:
                    registers[9] = (ulong)terminal.ReadChar();
                    break;
                case SysOP.READ_STRING:
                    //$a1: direccion de memoria del buffer,$a2: bytes por leer
                    string input = terminal.ReadString();
                    byte[] bytes = Encoding.Unicode.GetBytes(input);
                    address = (int)registers[9];
                    int bufferLength = (int)registers[10];
                    for(int i = 0; i < bufferLength; i++)
                    {
                        if (!memory.WriteByte(address + i,bytes[1]))
                            return Die("Tried to write on out of bounds memory");
                    }
                    break;
                case SysOP.ALLOC:
                    ulong requested = registers[9];
                    address = (int)dp;
                    if (dp >= registers[16])
                        dp = (ulong)dynamicMemStart;

                    while (dp < registers[16])
                    {
                        ulong allocated = 0;
                        while(allocated < requested)
                        {
                            memory.GetByte((int)dp++, out byte content);
                            if (content == 0)
                                allocated++;
                            else
                                break;
                        }
                        if(allocated == requested)
                        {
                            break;
                        }
                        address = (int)dp;
                    }
                    registers[10] = (dp < registers[16]) ? (ulong)address : ulong.MaxValue;
                    break;
                case SysOP.EXIT:
                    return Die("Program exited with code " + registers[9]);
                case SysOP.RANDOM:
                    //Pseudo rng: xoshiro256** 1.0
                    ulong x = rngState[1] * 5;
                    ulong result = ((x << 7) | (x >> (64 - 7))) * 9;
                    ulong t = rngState[1] << 17;

                    rngState[2] ^= rngState[0];
                    rngState[3] ^= rngState[1];
                    rngState[1] ^= rngState[2];
                    rngState[0] ^= rngState[3];

                    rngState[2] ^= t;
                    rngState[3] = (rngState[3] << 45) | (rngState[3] >> (64 - 45));
                    registers[9] = result;
                    break;
                case SysOP.RANDOM_SEED:
                    rngState[0] = 0;
                    rngState[1] = (registers[9] << 32);
                    rngState[2] = (registers[9] >> 32);
                    rngState[3] = 0;
                    break;
                case SysOP.TIMESTAMP:
                    var time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                    registers[9] = (ulong)time;
                    break;
                case SysOP.SLEEP:
                    //Time to sleep in ms, on register a1
                    int timeToSleep = (int)registers[9];
                    if(timeToSleep > 0)
                        System.Threading.Thread.Sleep(timeToSleep);
                    break;
                default:
                    return Die("Unsupported or invalid system operation code");
            }
            return true;
        }
        internal void RaiseInterrupt(int interrupt)
        {
            if (interrupt < 0 || interrupt > 7)
                return;
            flags |= (byte)(1 << interrupt);
        }
        internal void Reset()
        {
            registers = new ulong[20];
            registers[16] = 4095; //Initialize sp register to start of stack
            dp = 256; //Start of user available memory
            ip = 0; hi = 0; low = 0;
            flags = 0;
            rngState = new ulong[4];
        }
        private uint ReadUInt(Instruction current)
        {
            uint inmediate = ((uint)current.b5 << 24) + ((uint)current.b6 << 16)
                            + ((uint)current.b7 << 8) + (uint)current.b8;
            return inmediate;
        }
        private int ReadInt(Instruction current)
        {
            int inmediate = ((int)current.b5 << 24) + ((int)current.b6 << 16)
                            + ((int)current.b7 << 8) + (int)current.b8;
            return inmediate;
        }
        internal bool Die(string message)
        {
            executing = false;
            msg = "Error: " + message + " on instrucction " + (ip - 1);
            return false;
        }
        internal Dictionary<string,ulong> GetRegistersView()
        {
            var view = new Dictionary<string, ulong>();
            for (int i = 0; i < 12; i++)
            {
                string name = string.Empty;
                name += (i < 8) ? "g" : "a";
                name += (i % 8);
                view.Add(name, registers[i]);
            }
            for (int i = 12; i < 16; i++)
            {
                string name = "s" + (i % 12);
                view.Add(name, registers[i]);
            }
            view.Add("flags", flags);
            view.Add("sp", registers[16]);
            view.Add("fp", registers[17]);
            view.Add("dp", dp);
            view.Add("ip", ip);
            view.Add("ra", registers[18]);
            view.Add("acc", registers[19]);

            return view;
        }
        internal string PrintCPU()
        {
            string res = String.Empty;
            for(int i = 0; i < 12; i++)
            {
                res += (i < 8) ? "g" : "a";
                res += (i % 8) + ": ";
                res += registers[i] + " (" + (long)registers[i] + "*)" + "\n";
            }
            for(int i = 12; i < 16; i++)
            {
                res += "s" + (i % 12) + ": " + registers[i] + " (" + (long)registers[i] + ")\n";
            }
            res += "flags: " + Convert.ToString(flags,2).PadLeft(8,'0') + "\n";
            res += "sp: " + registers[16] + "\n";
            res += "fp: " + registers[17] + "\n";
            res += "dp: " + dp + "\n";
            res += "ip: " + ip + "\n";
            res += "ra: " + registers[18] + "\n";
            res += "acc: " + registers[19] + "\n";
            
            return res;
        }
    }
}
