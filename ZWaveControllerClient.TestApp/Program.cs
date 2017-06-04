using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZWaveControllerClient.Serial;
using ZWaveControllerClient.Xml;

namespace ZWaveControllerClient.TestApp
{
    internal class Program
    {
        private static CommandOption _portOption;
        private static CommandOption _logLevelOption;
        private const string HelpOptionTemplate = "-?|-h|--help";

        private enum ExitCodes
        {
            Success = 0
        }

        static void Main(string[] args)
        {
            var app = new CommandLineApplication();
            _portOption = app.Option("-port <port>", "The serial port name", CommandOptionType.SingleValue);
            _logLevelOption = app.Option("-loglevel <level>", $"Log levels are [{string.Join(", ", Enum.GetNames(typeof(LogLevel)))}]", CommandOptionType.SingleValue);

            app.Command("listnodes", config =>
            {
                config.OnExecute(() => ListNodesCommand());

                config.HelpOption(HelpOptionTemplate);
            });

            app.Command("controllerinfo", config =>
            {
                config.OnExecute(() => ControllerInfoCommand());

                config.HelpOption(HelpOptionTemplate);
            });

            app.Command("senddata", config =>
            {
                var nodeArg = config.Argument("nodeId", "The Node id to send the command.");
                var cmdClassArg = config.Argument("cmdClass", "The Command Class supported by the node.");
                var cmdArg = config.Argument("cmd", "The command associated with the Command Class.");
                var cmdDataArg = config.Argument("data", "Data to send as expressed in a hex string, e.g. 0xFF.");
                var monitorOption = config.Option("-monitor", "Monitor for updates to send data.", CommandOptionType.NoValue);
                config.OnExecute(() => SendDataCommand(nodeArg, cmdClassArg, cmdArg, cmdDataArg, monitorOption));

                config.HelpOption(HelpOptionTemplate);
            });

            app.HelpOption(HelpOptionTemplate);

            app.Execute(args);
        }        

        private static IZWaveController CreateZWaveController()
        {
            var xmlConfig = File.OpenRead(@"ZWave_custom_cmd_classes.xml");
            var xmlParser = new ZWaveClassesXmlParser();
            var loggerFactory = CreateLogFactory(_logLevelOption);
            var port = _portOption.Value();

            var zwController = new ZWaveSerialController(port, xmlParser.Parse(xmlConfig), loggerFactory);

            zwController.Connect();

            return zwController;
        }

        public static byte[] FromHexString(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return new byte[] { };
            }

            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex.Substring(2);
            }

            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private static async Task<int> SendDataCommand(CommandArgument nodeArg, CommandArgument cmdClassArg, CommandArgument cmdArg, CommandArgument cmdDataArg, CommandOption monitorOption)
        {
            var zwController = CreateZWaveController();

            var node = byte.Parse(nodeArg.Value);

            var cmdClass = zwController.Classes.CommandClasses.First(l => l.Name == cmdClassArg.Value);
            var cmd = cmdClass.Commands.Single(l => l.Name == cmdArg.Value);

            var cmdData = FromHexString(cmdDataArg.Value);

            void ZwController_FrameReceived(object sender, FrameEventArgs e)
            {
                switch (e.Frame.Function)
                {
                    case ZWaveFunction.ApplicationCommandHandler:
                        var recvNodeId = e.Frame.Payload[1];
                        var recvCmdClass = zwController.Classes.CommandClasses.First(l => l.Key == e.Frame.Payload[3]);
                        var recvCmd = cmdClass.Commands.First(l => l.Key == e.Frame.Payload[4]);

                        Console.WriteLine($"node: {recvNodeId} len: {e.Frame.Payload[2]:x2} cmdClass: {cmdClass} cmd: {cmd} val: {e.Frame.Payload[5]}");
                        break;
                }
            }

            var transOptions = TransmitOptions.Acknowledge | TransmitOptions.AutoRoute;

            await zwController.SendCommand(node, cmdClass, cmd, transOptions, cmdData);

            if (monitorOption.HasValue())
            {
                zwController.FrameReceived += ZwController_FrameReceived;
                await Task.Delay(-1);
            }

            return (int) ExitCodes.Success;
        }

        private static async Task<int> ControllerInfoCommand()
        {
            var zwController = CreateZWaveController();

            await zwController.FetchControllerInfo();

            return (int)ExitCodes.Success;
        }

        private static async Task<int> ListNodesCommand()
        {
            var zwController = CreateZWaveController();

            await zwController.DiscoverNodes();
            await zwController.FetchNodeInfo();

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
                    Console.WriteLine($"\t\t{cmdClass.Name} - {cmdClass}");

                    Console.WriteLine("\t\t\tCommands");

                    foreach (var cmd in cmdClass.Commands)
                    {
                        Console.WriteLine($"\t\t\t\t{cmd.Name} - {cmd}");
                    }
                }
            }

            return (int)ExitCodes.Success;
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