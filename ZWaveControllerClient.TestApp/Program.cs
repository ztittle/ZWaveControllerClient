using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZWaveControllerClient.Serial;
using ZWaveControllerClient.Xml;

namespace ZWaveControllerClient.TestApp
{
    internal class Program
    {
        private static CommandOption _logLevelOption;
        private const string HelpOptionTemplate = "-?|-h|--help";

        private enum ExitCodes
        {
            Success = 0,
            Failure = 1
        }

        static void Main(string[] args)
        {
            var app = new CommandLineApplication();
            _logLevelOption = app.Option("-loglevel <level>", $"Log levels are [{string.Join(", ", Enum.GetNames(typeof(LogLevel)))}]", CommandOptionType.SingleValue);
            
            app.Command("listnodes", config =>
            {
                var portArg = config.Argument("port", "The serial port name");
                config.Description = "Lists all nodes in the network.";
                config.OnExecute(() => ListNodesCommand(portArg));

                config.HelpOption(HelpOptionTemplate);
            });

            app.Command("controllerinfo", config =>
            {
                var portArg = config.Argument("port", "The serial port name");
                config.Description = "Prints controller information.";
                config.OnExecute(() => ControllerInfoCommand(portArg));

                config.HelpOption(HelpOptionTemplate);
            });

            app.Command("senddata", config =>
            {
                var portArg = config.Argument("port", "The serial port name");
                config.Description = "Sends arbitrary data to a node.";
                var nodeArg = config.Argument("nodeId", "The Node id to send the command.");
                var cmdClassArg = config.Argument("cmdClass", "The Command Class supported by the node.");
                var cmdArg = config.Argument("cmd", "The command associated with the Command Class.");
                var cmdDataArg = config.Argument("data", "Data to send as expressed in a hex string, e.g. 0xFF.");
                var monitorOption = config.Option("-monitor", "Monitor for updates to send data.", CommandOptionType.NoValue);
                config.OnExecute(() => SendDataCommand(portArg, nodeArg, cmdClassArg, cmdArg, cmdDataArg, monitorOption));

                config.HelpOption(HelpOptionTemplate);
            });

            app.Command("addnodes", config =>
            {
                var portArg = config.Argument("port", "The serial port name");
                config.Description = "Adds nodes to the network using Network Wide Inclusion.";
                config.OnExecute(() => AddNodeCommandNWI(portArg));
                config.HelpOption(HelpOptionTemplate);
            });

            app.Command("addnode", config =>
            {
                var portArg = config.Argument("port", "The serial port name");
                config.Description = "Adds a new node to the network.";
                config.OnExecute(() => AddNodeCommand(portArg));
                config.HelpOption(HelpOptionTemplate);
            });

            app.Command("removenode", config =>
            {
                var portArg = config.Argument("port", "The serial port name");
                config.Description = "Removes a new node from the network.";
                config.OnExecute(() => RemoveNodeCommand(portArg));
                config.HelpOption(HelpOptionTemplate);
            });
            app.HelpOption(HelpOptionTemplate);


            if (args.Length == 0)
            {
                app.ShowHelp();
                return;
            }

            try
            {
                app.Execute(args);
            }
            catch(CommandParsingException cpe)
            {
                Console.WriteLine(cpe.Message);
            }
        }

        private static async Task<int> RemoveNodeCommand(CommandArgument portArg)
        {
            Console.WriteLine($"Press the push button on the device.");
            var zwController = CreateZWaveController(portArg.Value);
            var node = await zwController.RemoveNode(CancellationToken.None);
            Console.WriteLine($"Removed {node}");
            return (int)ExitCodes.Success;
        }

        private static async Task<int> AddNodeCommand(CommandArgument portArg)
        {
            Console.WriteLine($"Press the push button on the device.");
            var zwController = CreateZWaveController(portArg.Value);
            var node = await zwController.AddNode(CancellationToken.None);
            Console.WriteLine($"Added {node}");
            return (int)ExitCodes.Success;
        }

        private static async Task<int> AddNodeCommandNWI(CommandArgument portArg)
        {
            Console.WriteLine($"Press the push button on the device.");
            var zwController = CreateZWaveController(portArg.Value);
            var cts = new CancellationTokenSource();
            
            var t1 = Task.Run(() =>
            {
                Console.WriteLine("Press any key to stop Network Wide Inclusion");
                Console.ReadKey();

                cts.Cancel();
            });

            var t2 = zwController.AddNodeNetworkWideInclusion(cts.Token);

            await Task.WhenAll(t1, t2);

            var nodes = t2.Result;

            foreach (var node in nodes)
            {
                Console.WriteLine($"Added {node}");
            }

            return (int)ExitCodes.Failure;
        }

        private static IZWaveController CreateZWaveController(string port)
        {
            var xmlConfig = File.OpenRead(@"ZWave_custom_cmd_classes.xml");
            var xmlParser = new ZWaveClassesXmlParser();
            var loggerFactory = CreateLogFactory(_logLevelOption);

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

        private static async Task<int> SendDataCommand(CommandArgument portArg, CommandArgument nodeArg, CommandArgument cmdClassArg, CommandArgument cmdArg, CommandArgument cmdDataArg, CommandOption monitorOption)
        {
            var zwController = CreateZWaveController(portArg.Value);

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

        private static async Task<int> ControllerInfoCommand(CommandArgument portArg)
        {
            var zwController = CreateZWaveController(portArg.Value);

            await zwController.FetchControllerInfo();

            Console.WriteLine($"Node Id: {zwController.Id}");
            Console.WriteLine($"Home Id: 0x{string.Concat(zwController.HomeId.Select(b => b.ToString("x2")))}");
            Console.WriteLine($"SucNode Id: {zwController.SucNodeId}");
            Console.WriteLine($"Capabilities: {zwController.Capabilities}");
            Console.WriteLine($"Protocol Version: {zwController.Version}");
            Console.WriteLine($"ZWave App Version: {zwController.Version.ApplicationVersion}.{zwController.Version.ApplicationSubVersion}");

            return (int)ExitCodes.Success;
        }

        private static async Task<int> ListNodesCommand(CommandArgument portArg)
        {
            var zwController = CreateZWaveController(portArg.Value);

            await zwController.DiscoverNodes();
            await zwController.FetchNodeInfo();

            foreach (var node in zwController.Nodes)
            {
                Console.WriteLine(node);

                Console.WriteLine($"\tReserved: {node.ProtocolInfo.Reserved}");

                Console.WriteLine($"\tSecurity: {node.ProtocolInfo.Security}");

                Console.WriteLine($"\tCapability: {node.ProtocolInfo.Capability}");

                Console.WriteLine($"\tIsListening: {node.ProtocolInfo.IsListening}");

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