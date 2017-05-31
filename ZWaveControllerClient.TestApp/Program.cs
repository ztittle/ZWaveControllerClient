using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZWaveControllerClient.SerialIO;

namespace ZWaveControllerClient.TestApp
{
    internal class Program
    {


        static async Task MainAsync(ZWaveSerialController zwController)
        {
            var xml = File.OpenRead(@"ZWave_custom_cmd_classes.xml");
            await zwController.Initialize(xml);
            var lightSwitch = zwController.Nodes.First(l => l.ProtocolInfo.SpecificType.Name == "SPECIFIC_TYPE_POWER_SWITCH_MULTILEVEL");
            var cmdClass = lightSwitch.SupportedCommandClasses.First(l => l.Name == "COMMAND_CLASS_SWITCH_MULTILEVEL");
            var setCmd = cmdClass.Commands.Single(l => l.Name == "SWITCH_MULTILEVEL_SET");
            zwController.FrameReceived += ZwController_FrameReceived;

            var transOptions = TransmitOptions.Acknowledge | TransmitOptions.AutoRoute | TransmitOptions.Explore;

            var response1 = await zwController.SendCommand(lightSwitch, cmdClass, setCmd, transOptions, 99);
            var getCmd = cmdClass.Commands.Single(l => l.Name == "SWITCH_MULTILEVEL_GET");
            await Task.Delay(1000); // todo: how to get rid of delay?
            var response2 = await zwController.SendCommand(lightSwitch, cmdClass, getCmd, transOptions);

        }

        private static void ZwController_FrameReceived(object sender, FrameEventArgs e)
        {
            switch (e.Frame.Function)
            {
                case ZWaveFunction.FUNC_ID_APPLICATION_COMMAND_HANDLER:
                    var node = ((ZWaveSerialController)sender).Nodes.Single(l => l.Id == e.Frame.Payload[1]);
                    var cmdClass = node.SupportedCommandClasses.First(l => l.Key == e.Frame.Payload[3]);
                    var cmd = cmdClass.Commands.First(l => l.Key == e.Frame.Payload[4]);

                    Console.WriteLine($"node: {node} len: {e.Frame.Payload[2]:x2} cmdClass: {cmdClass} cmd: {cmd} val: {e.Frame.Payload[5]}");
                    break;
            }
        }

        static void Main(string[] args)
        {
            using (var zwController = new ZWaveSerialController("COM3", new Logger()))
            {
                zwController.Connect();
                MainAsync(zwController).Wait();

                Console.WriteLine();
                Console.WriteLine("Press enter to end program.");
                Console.ReadLine();
            }
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