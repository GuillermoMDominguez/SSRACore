using System;
using System.Collections.Generic;
using System.Text;

namespace EmulatorProject
{
    internal enum Opcode
    {
        NOP=0, ADD, SUB, ADDI, SUBI, ADDU, SUBU, ADDUI, SUBUI,MUL, MULT, MULTI, DIV, DIVI,MOD,MODI,ACC,
        AND, ANDI, OR, ORI, XOR, XORI, NOT,
        LSB,RSB,
        MOV, MOVI,
        SEQ, SNQ, SLT, SGT, CHK, CLF,
        BEQ,BNQ,BGT,BLT,
        JMP,JR,JAL,JST,RST,
        LW,SW,LAD,PUSH,POP,
        MFH,MFL,
        SIR,SYSCALL,BREAK,
        ADDF,SUBF,MULF,DIVF,ADDFI,SUBFI,MULFI,DIVFI,SQRF,POWF,COSF,SENF,TANF,INVF,EXPF,
        SEQF,SNQF,SLTF,SGTF,SNAN,
        MOVF,MOVFI,LFM,SFM,
        HALT
    }
    internal enum SysOP
    {
        PRINT_INT=1,PRINT_UINT=2,PRINT_WORD=3,PRINT_DOUBLE=4,PRINT_CHAR=5,PRINT_STRING=6,READ_INT=7,
        READ_DOUBLE=8,READ_CHAR=9,READ_STRING=10,ALLOC=11,EXIT=12,RANDOM=13,RANDOM_SEED=14,TIMESTAMP=15,SLEEP=16
    }
    internal struct Instruction
    {
        internal readonly Opcode opcode;
        internal readonly byte b2, b3, b4, b5, b6, b7, b8;
        internal Instruction(Opcode opcode,byte[] bytes)
        {
            this.opcode = opcode;
            b2 = bytes[0];
            b3 = bytes[1];
            b4 = bytes[2];
            b5 = bytes[3];
            b6 = bytes[4];
            b7 = bytes[5];
            b8 = bytes[6];

        }
        static internal string PrintInstruction(Instruction ins)
        {
            string res = Enum.GetName(typeof(Opcode),ins.opcode) + "," + ins.b2 + "," + ins.b3 + "," + ins.b4;
            int inmediate = ((int)ins.b5 << 24) + ((int)ins.b6 << 16)
                            + ((int)ins.b7 << 8) + (int)ins.b8;
            res += "," + inmediate + "\n";
            return res;
        }
    }
}
