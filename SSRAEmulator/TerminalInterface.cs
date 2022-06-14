using System;
using System.Collections.Generic;
using System.Text;

namespace EmulatorProject
{
    public interface TerminalInterface
    {
        public void Write(string text);
        public int ReadInt();
        public float ReadFloat();
        public double ReadDouble();
        public char ReadChar();
        public string ReadString();

    }
}
