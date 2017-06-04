using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
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
            var lightSwitch = zwController.Nodes.First(l => l.ProtocolInfo.SpecificType.Name == "SPECIFIC_TYPE_POWER_SWITCH_MULTILEVEL");
            var cmdClass = lightSwitch.SupportedCommandClasses.First(l => l.Name == "COMMAND_CLASS_SWITCH_MULTILEVEL");
            var setCmd = cmdClass.Commands.Single(l => l.Name == "SWITCH_MULTILEVEL_SET");
            zwController.FrameReceived += ZwController_FrameReceived;

            var transOptions = TransmitOptions.Acknowledge | TransmitOptions.AutoRoute;

            var setLightOnFrame = await zwController.SendCommand(lightSwitch, cmdClass, setCmd, transOptions, 0);
            var getCmd = cmdClass.Commands.Single(l => l.Name == "SWITCH_MULTILEVEL_GET");

            var getLightStatusFrame = await zwController.SendCommand(lightSwitch, cmdClass, getCmd, transOptions);
            
        }

        private static void ZwController_FrameReceived(object sender, FrameEventArgs e)
        {
            switch (e.Frame.Function)
            {
                case ZWaveFunction.ApplicationCommandHandler:
                    var node = ((ZWaveSerialController)sender).Nodes.Single(l => l.Id == e.Frame.Payload[1]);
                    var cmdClass = node.SupportedCommandClasses.First(l => l.Key == e.Frame.Payload[3]);
                    var cmd = cmdClass.Commands.First(l => l.Key == e.Frame.Payload[4]);

                    Console.WriteLine($"node: {node} len: {e.Frame.Payload[2]:x2} cmdClass: {cmdClass} cmd: {cmd} val: {e.Frame.Payload[5]}");
                    break;
            }
        }

        private enum ExitCodes
        {
            Success = 0
        }

        static void Main(string[] args)
        {
            var app = new CommandLineApplication();
            var portOption = app.Option("-port <port>", "The serial port name", CommandOptionType.SingleValue);
            var logLevelOption = app.Option("-loglevel <level>", $"Log levels are [{string.Join(", ", Enum.GetNames(typeof(LogLevel)))}]", CommandOptionType.SingleValue);

            app.Command("listnodes", config =>
            {
                config.OnExecute(async () => {
                    var zwController = await CreateZWaveController(portOption, logLevelOption);

                    ListNodes(zwController);
                    
                    return (int)ExitCodes.Success; ;
                });

                config.HelpOption("-?|-h|--help");
            });
            
            app.HelpOption("-?|-h|--help");

            app.Execute(args);
        }

        private static async Task<ZWaveSerialController> CreateZWaveController(CommandOption portOption, CommandOption logLevelOption)
        {
            var xmlConfig = File.OpenRead(@"ZWave_custom_cmd_classes.xml");
            var loggerFactory = CreateLogFactory(logLevelOption);
            var port = portOption.Value();

            var zwController = new ZWaveSerialController(port, loggerFactory);

            zwController.Connect();

            using (xmlConfig)
            {
                await zwController.Initialize(xmlConfig);
            }

            return zwController;
        }

        private static void ListNodes(ZWaveSerialController zwController)
        {
            foreach (var node in zwController.Nodes)
            {
                Console.WriteLine(node);

                Console.WriteLine("\tSupported Command Classes");

                if (!node.SupportedCommandClasses.Any())
                {
                    Console.WriteLine("\t\tNot Available");
                    continue;
                }

                foreach (var cmdClass in node.SupportedCommandClasses)
                {
                    Console.WriteLine($"\t\t{cmdClass}");
                }
            }
        }

        private static ILoggerFactory CreateLogFactory(CommandOption logLevelOption)
        {
            ILoggerFactory loggerFactory;
            if (!Enum.TryParse(logLevelOption.Value(), true, out LogLevel logLevel))
            {
                logLevel = LogLevel.None;
            }

            loggerFactory = new LoggerFactory()
               .AddConsole(logLevel);
            return loggerFactory;
        }
    }
}