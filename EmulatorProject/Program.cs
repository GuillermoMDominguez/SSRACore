using System;

namespace EmulatorProject
{
    class Program
    {
        static void Main(string[] args)
        {
            Emulator uut = new Emulator();
            var clock = new System.Diagnostics.Stopwatch();
            clock.Start();
            String[] source = System.IO.File.ReadAllLines(@"Guess.txt");
            String res = uut.AssembleProgram("Guess", source);
            if(res != String.Empty)
            {
                Console.WriteLine(res);
                return;
            }
            clock.Stop();
            var assemblyTime = clock.ElapsedMilliseconds;
            string program = uut.PrintProgram("Guess");
            Console.WriteLine(program);
            /*
            asb.EmitInmediate(Opcode.MOVI, 1, 3);
            asb.EmitInmediate(Opcode.MOVI, 2, 2);
            asb.Emit3Args(Opcode.ADD, 1, 2, (byte)9);
            asb.EmitInmediate(Opcode.MOVI, 8, 1);
            asb.EmitNoArgs(Opcode.SYSCALL);
            asb.EmitNoArgs(Opcode.HALT);
            */
            /*
            Byte[] i1 =
            {
                1,0,0,0,0,0,3
            };
            Byte[] i2 =
            {
                2,0,0,0,0,0,2
            };
            Byte[] a1 =
            {
                1,2,9,0,0,0,0
            };
            Byte[] i3 =
            {
                8,0,0,0,0,0,1
            };
            Instruction[] program =
            {
                new Instruction(Opcode.MOVI,i1),
                new Instruction(Opcode.MOVI,i2),
                new Instruction(Opcode.ADD,a1),
                new Instruction(Opcode.MOVI,i3),
                new Instruction(Opcode.SYSCALL,(new byte[7])),
                new Instruction(Opcode.HALT,(new byte[7]))
            };
            */
            clock.Restart();
            bool result = uut.RunProgram("Guess");
            clock.Stop();
            if (!result)
            {
                Console.WriteLine("Error on execution");
                Console.WriteLine(uut.ExecutionMsg);
            }
            Console.WriteLine(uut.PrintRegisters());
            Console.WriteLine("Assembly time: " + assemblyTime + " ms");
            Console.WriteLine("Run time: " + clock.ElapsedMilliseconds + " ms");
        }
    }
}
