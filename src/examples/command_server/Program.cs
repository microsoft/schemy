// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Schemy;

namespace Examples.command_server
{
    class Program
    {
        delegate object Function(object input);

        static void Main(string[] args)
        {
            Interpreter.CreateSymbolTableDelegate extension = _ => new Dictionary<Symbol, object>()
            {
                { Symbol.FromString("get-current-os"), NativeProcedure.Create(() => GetCurrentSystem()) },
                { Symbol.FromString("chain"), new NativeProcedure(funcs => new Function(input => funcs.Cast<Function>().Select(b => input = b(input)).Last())) },
                { Symbol.FromString("say-hi"), NativeProcedure.Create<Function>(() => name => $"Hello {name}!") },
                { Symbol.FromString("man-freebsd"), NativeProcedure.Create<Function>(() => cmd => GetUrl($"https://www.freebsd.org/cgi/man.cgi?query={cmd}&format=ascii")) },
                { Symbol.FromString("man-linux"), NativeProcedure.Create<Function>(() => cmd => GetUrl($"http://man7.org/linux/man-pages/man1/{cmd}.1.html")) },
                { Symbol.FromString("truncate-string"), NativeProcedure.Create<int, Function>(len => input => ((string)input).Substring(0, len)) },
            };

            var interpreter = new Interpreter(new[] { extension });

            if (args.Contains("--repl")) // start the REPL with all implemented functions
            {
                interpreter.REPL(Console.In, Console.Out);
                return;
            }
            else
            {
                // starts a TCP server that receives request (cmd <data>) and sends response back.
                var engines = new Dictionary<string, Function>();
                foreach (var fn in Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "*.ss"))
                {
                    Console.WriteLine($"Loading file {fn}");
                    LoadScript(interpreter, fn);
                    engines[Path.GetFileNameWithoutExtension(fn)] = (Function)interpreter.Environment[Symbol.FromString("EXECUTE")];
                }

                string ip = "127.0.0.1"; int port = 8080;
                var server = new TcpListener(IPAddress.Parse(ip), port);
                server.Start();
                Console.WriteLine($"Server started at {ip}:{port}");

                try
                {
                    using (var c = server.AcceptTcpClient())
                    using (var cs = c.GetStream())
                    using (var sr = new StreamReader(cs))
                    using (var sw = new StreamWriter(cs))
                    {
                        Console.WriteLine($"Client accepted at {c.Client.RemoteEndPoint}");
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            string[] parsed = line.Split(new[] { ' ' }, 2);
                            if (parsed.Length != 2)
                            {
                                sw.WriteLine($"cannot parse {line}");
                                sw.Flush();
                            }
                            else
                            {
                                string engine = parsed[0], request = parsed[1];
                                if (!engines.ContainsKey(engine))
                                {
                                    sw.WriteLine($"engine not found: {engine}");
                                    sw.Flush();
                                }
                                else
                                {
                                    string output = (string)(engines[engine](request));
                                    sw.WriteLine(output);
                                    sw.Flush();
                                }
                            }
                        }
                    }
                }
                catch (IOException) { }
            }
        }

        static void LoadScript(Interpreter interpreter, string file)
        {
            using (Stream script = File.OpenRead(file))
            using (TextReader reader = new StreamReader(script))
            {
                var res = interpreter.Evaluate(reader);
                if (res.Error != null) throw res.Error;
            }
        }

        // support: windows, freebsd, linux, unknown
        static string GetCurrentSystem()
        {
            var os = System.Environment.OSVersion.Platform;
            if (os.ToString().Contains("Win")) return "windows";
            if (os == PlatformID.Unix)
            {
                Process proc = new Process() { StartInfo = new ProcessStartInfo("uname") { RedirectStandardOutput = true } };
                proc.Start();
                var output = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit();

                foreach (var w in new[] { "freebsd", "linux" })
                {
                    if (output.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) return w;
                }
            }

            return "unknown";
        }
        
        static string GetUrl(string url)
        {
            using (var wc = new System.Net.WebClient())
            {
                return wc.DownloadString(url);
            }
        }
    }
}
