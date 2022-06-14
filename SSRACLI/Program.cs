using System;
using System.IO;
using System.Reflection;
using SSRAEmulator;

namespace SSRACLI
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename;
            string directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"sources");
            Directory.CreateDirectory(directory);
            if(args.Length > 0)
            {
                filename = args[0];
            }
            else
            {
                Console.Write("Introduce el nombre del archivo con el código:\n>> ");
                filename = Console.ReadLine();
            }
            string[] source;
            try
            {
                source = (args.Length > 0) ? File.ReadAllLines(filename) : File.ReadAllLines(Path.Combine(directory,filename));
            } 
            catch(Exception e)
            {
                Console.WriteLine("Error al abrir el archivo: \n" + e.Message);
                Console.ReadLine();
                return;
            }
            Emulator computer = new Emulator(new ConsoleTerminal());
            var clock = new System.Diagnostics.Stopwatch();
            clock.Start();
            string assemblyResult = computer.AssembleProgram(filename, source);
            clock.Stop();
            if(assemblyResult != string.Empty)
            {
                Console.WriteLine(assemblyResult);
                Console.WriteLine("Ocurrieron uno o mas problemas al generar el ejecutable del archivo\n[Presiona enter para salir]");
                Console.ReadLine();
                return;
            }
            else
            {
                Console.WriteLine("Programa ensamblado en " + clock.ElapsedMilliseconds + " ms");
            }
            clock.Restart();
            bool result = computer.RunProgram(filename);
            clock.Stop();
            if (!result)
            {
                Console.WriteLine("La CPU encontró un problema durante la ejecución del programa y se detuvo");
                Console.WriteLine(computer.ExecutionMsg);
            }
            else
            {
                Console.WriteLine("El programa se ejecutó correctamente");
                Console.WriteLine("Tiempo total de ejecución: " + clock.ElapsedMilliseconds + " ms");
            }
            Console.ReadLine();
        }
    }
}
