using System;
using System.Collections.Generic;
using System.IO;
using ZWaveControllerClient.SerialIO;

namespace ZWaveControllerClient.TestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var zwController = new ZWaveSerialController("COM3", new Logger());
            zwController.Connect();
            var xml = File.OpenRead(@"ZWave_custom_cmd_classes.xml");
            zwController.Initialize(xml).Wait();
            Console.WriteLine();
            Console.WriteLine("Press enter to end program.");
            Console.ReadLine();
        }

        private class Logger : ILogger
        {
            public void LogError(string msg, params KeyValuePair<string, object>[] details)
            {
                Console.WriteLine(msg);
            }

            public void LogException(Exception e, string msg = "")
            {
                Console.WriteLine(e.Message);
            }

            public void LogInformation(string msg, params KeyValuePair<string, object>[] details)
            {
                Console.WriteLine(msg);
            }
        }
    }
}