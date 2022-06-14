using System;
using System.Collections.Generic;
using System.Text;

namespace EmulatorProject
{
    public class Emulator
    {
        CPU mainProcessor;
        MathChip coprocessor;
        MemoryChip memory;
        Assembler assembler;
        TerminalInterface terminal;
        Dictionary<string, CProgram> programs;
        string loaded;
        Dictionary<string, ulong> cpuRegisters;
        Dictionary<string, double> fpuRegisters;
        uint writePointer, readPointer;

        public string Loaded { get => loaded;private set => loaded = value; }
        public string ExecutionMsg { get => mainProcessor.msg; }
        public string AssemblyMsg { get; private set; }
        public Dictionary<string, double> FpuRegisters { get => fpuRegisters;private set => fpuRegisters = value; }
        public Dictionary<string, ulong> CpuRegisters { get => cpuRegisters;private set => cpuRegisters = value; }

        public Emulator(TerminalInterface terminal = null)
        {
            memory = new MemoryChip();
            assembler = new Assembler();
            this.terminal = (terminal == null) ?  new ConsoleTerminal() : terminal;
            coprocessor = new MathChip(memory);
            mainProcessor = new CPU(this.terminal, memory, coprocessor);
            coprocessor.ConnectToCPU(mainProcessor);
            programs = new Dictionary<string, CProgram>();
            Loaded = string.Empty;
            writePointer = 0; readPointer = 0;
        }
        public void SetTerminal(TerminalInterface terminal)
        {
            this.terminal = terminal;
            mainProcessor = new CPU(terminal, memory, coprocessor);
        }
        public string AssembleProgram(string name, string[] source)
        {
            AssemblyMsg = assembler.ParseSource(source);
            if (!assembler.correctProgram)
                return AssemblyMsg; //We don´t have a correct program, so we bail execution
            CProgram parsed = assembler.GetProgram(name);
            programs.Add(name, parsed);
            return AssemblyMsg;
        }
        public bool LoadProgram(string name)
        {
            if (Loaded != string.Empty)
            {
                ResetEmulator();
                Loaded = string.Empty;
            }
            if (!programs.TryGetValue(name, out CProgram selected))
                return false;
            Loaded = name;
            mainProcessor.LoadProgram(selected);
            return true;
        }
        public bool EmulateStep()
        {
            if (Loaded == string.Empty)
                return false;
            return mainProcessor.StepInstruction();
        }
        public bool RunProgram(string name)
        {
            if (!programs.TryGetValue(name, out CProgram selected))
                return false;
            mainProcessor.LoadProgram(selected);
            return mainProcessor.Run();
        }
        public bool WriteToIO(int port,ulong word)
        {
            /*
             * 128 - 192: 8 word-sized I/O ports
             * 192 - 248: circular buffer with 7 words spaces
             * 248 - 256: word-sized port, writting to it triggers an interrupt
             */
            if (port < 0 || port > 10)
                return false;
            if(port < 8)
            {
                memory.SaveMemory(word, 128 + port * 8);
                return true;
            }
            else if(port == 9)
            {
                memory.SaveMemory(word, 192 + (int)(writePointer % 7));
                writePointer++;
                return true; //Write to circular buffer
            }
            memory.SaveMemory(word, 248); //Trigger an IO interrupt
            mainProcessor.RaiseInterrupt(3);
            return true;
        }
        public ulong ReadIOPort(int port)
        {
            if (port < 0 || port > 10)
                return uint.MaxValue;
            if(port < 8)
            {
                return memory.ReadMemory(128 + port);
            }
            else if (port == 9)
            {
                return memory.ReadMemory(192 + (int)(readPointer++ % 7));
            }
            return memory.ReadMemory(248);
        }
        public void RaiseCPUInterrupt(int interrupt)
        {
            mainProcessor.RaiseInterrupt(interrupt);
        }
        public void RefreshEmulatorView()
        {
            this.CpuRegisters = mainProcessor.GetRegistersView();
            this.FpuRegisters = coprocessor.GetMPUView();
        }
        public ulong[] ReadMemory(int address,int words)
        {
            return memory.ReadWords(address, words);
        }
        public double[] ReadMemoryAsDouble(int address,int words)
        {
            return memory.ReadDoubleWords(address, words);
        }
        public string PrintProgram(string name)
        {
            if (!programs.TryGetValue(name, out CProgram selected))
                return "No program with the name " + name;
            return selected.PrintProgram();
        }
        public string PrintRegisters()
        {
            string res = mainProcessor.PrintCPU();
            res += coprocessor.PrintMPU();
            return res;
        }
        public void DeletePrograms()
        {
            programs.Clear();
        }
        public void ResetEmulator()
        {
            memory.Reset();
            mainProcessor.Reset();
            coprocessor.Reset();
            assembler.Reset();
            writePointer = 0; readPointer = 0;
        }
    }
}
