using System;
using System.Collections.Generic;
using System.Text;

namespace SSRAEmulator
{
    internal class CProgram
    {
        internal readonly Instruction[] instructions;
        internal readonly byte[] dataSegment;
        internal readonly int[] jumpTable;
        public readonly string name;

        internal CProgram(Instruction[] instructions,byte[] data,string name,int[] routines=null)
        {
            this.name = name;
            this.instructions = instructions;
            this.dataSegment = data;
            if (routines != null)
                jumpTable = routines;
            else
            {
                jumpTable = new int[]
                {
                    0,-1,-1,-1,-1,-1,-1,-1
                };
            }
        }
        internal string PrintProgram()
        {
            string res = "Program " + instructions.Length + " lines\n" +
                         "----------------\n";
            int i = 0;
            foreach (Instruction ins in instructions)
            {
                res += i + ": " + Instruction.PrintInstruction(ins);
                i++;
            }
            res += "----------------\n";
            return res;
        }
    }
}
