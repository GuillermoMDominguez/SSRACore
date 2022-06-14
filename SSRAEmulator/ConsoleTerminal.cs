using System;
using System.Collections.Generic;
using System.Text;

namespace SSRAEmulator
{
    public class ConsoleTerminal : TerminalInterface
    {
        char TerminalInterface.ReadChar()
        {
            return (char)Console.Read();
        }

        double TerminalInterface.ReadDouble()
        {
            string input = Console.ReadLine();
            if (!double.TryParse(input, out double result))
                return double.NaN;
            return result;
        }

        float TerminalInterface.ReadFloat()
        {
            string input = Console.ReadLine();
            if (!float.TryParse(input, out float result))
                return float.NaN;
            return result;
        }

        int TerminalInterface.ReadInt()
        {
            string input = Console.ReadLine();
            if (!int.TryParse(input, out int result))
                return int.MinValue;
            return result;
        }

        string TerminalInterface.ReadString()
        {
            return Console.ReadLine();
        }

        void TerminalInterface.Write(string text)
        {
            Console.Write(text);
        }
    }
}
