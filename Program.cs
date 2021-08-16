using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Tommy;

namespace Japa_s_Smart_Paper_Spigot_Launcher
{
    class Program
    {
        public static string name = "Japa's Spigot Launcher";
        public static int current_version = 1;
        public static string title = $"{Program.name} v{current_version}";
        public static string config_file = $"{Program.title}.toml";

        static void Main()
        {
            Program.RefreshScreen("Loading...");
          
            if(!File.Exists(Program.config_file))
            {
                Console.Write("No Config File found! generating one with the default parameters... ");

                Program.Generate_Config();

                Console.WriteLine("Ok!");
            }

            else Console.WriteLine("Config file was found! loading...\n");

            using(StreamReader reader = File.OpenText(Program.config_file))
            {
                TomlTable config = TOML.Parse(reader);

                // Previously known as pleaseFixThisTommyImTiredOfCreatingVariables
                string latest_file_name = config["paper_auto_upgrade"]["latest_file_name"];
                string jar_file = $"{System.AppContext.BaseDirectory}{latest_file_name}.jar";

                int initial_ram_allocation = config["general"]["java_initial_allocation_xms"];
                int max_ram_allocation = config["general"]["java_max_allocation_xmx"];
                string mc_version = config["paper_auto_upgrade"]["spigot_version"];

                try
                {
                    if(config["general"]["debug_mode"]) 
                    {
                        Console.Title = ($"{title} - Displaying current config");

                        Console.WriteLine($"Current Program Configs:\n");

                        Console.Write($"- Is program on DEBUG mode?: ");
                        Japa.PrintBoolean(config["general"]["debug_mode"]);

                        Console.Write($"- Will the server run on GRAPHICS Mode: ");
                        Japa.PrintBoolean(config["general"]["graphics_mode"]);

                        Console.Write($"- Will the prompt pause on JAR closure: ");
                        Japa.PrintBoolean(config["general"]["pause_on_jar_closure"]);

                        //Japa.WriteColorLine($"- Java Version that will be used: <green|{config["general"]["java_version"]}>");
                        Japa.WriteColorLine($"- Initial RAM Allocation: <green|{initial_ram_allocation}MB>");
                        Japa.WriteColorLine($"- Max RAM Allocation: <green|{max_ram_allocation}MB>\n");

                        if(config["paper_auto_upgrade"]["enabled"])
                        {
                            Japa.WriteColorLine("Paper Auto Upgrade is <green|ENABLED>");
                            Japa.WriteColorLine($"- Paper File Name: <green|{latest_file_name}.jar>");
                            Japa.WriteColorLine($"- Paper Version: <green|{mc_version}>");
                        }

                        else
                        {
                            Japa.WriteColorLine($"- JAR Preffix used: <green|{config["general"]["jar_preffix"]}>");
                            Japa.WriteColorLine($"- Will the server use the last JAR found with this preffix: <green|{config["general"]["use_last_jar_found"]}>");   

                            string[] files = Directory.GetFiles(System.AppContext.BaseDirectory, $"{config["general"]["jar_preffix"]}*.jar");
                            if(files.Length >= 1)
                            {
                                if(config["general"]["use_last_jar_found"]) jar_file = files[files.Length - 1];
                                else jar_file = files[0];
                            }
                        }

                        Japa.WriteColorLine("\nPress <green|ENTER> to start the server.");
                        Console.ReadLine();
                    }
                }

                catch 
                { Japa.ErrorHalt("Invalid Config File! Its recommended that you generate another one!"); }

                if(config["paper_auto_upgrade"]["enabled"])
                {
                    Program.RefreshScreen("Checking for Paper Updates...");

                    Console.Write("Checking for Paper Updates... ");

                    try
                    {
                        string res = Japa.FetchSync(
                            $"https://papermc.io/api/v2/projects/paper/versions/{mc_version}", 
                            "application/json"
                        );

                        //Console.WriteLine(res);

                        MatchCollection regex = new Regex(@"\d+", RegexOptions.Multiline).Matches(res);

                        if(!config["paper_auto_upgrade"]["current_version"].HasValue)
                        {
                            int version = Convert.ToInt16(regex[regex.Count - 1].Value);

                            //Console.WriteLine("Current Version is null on config file!");
                            //Console.WriteLine(version);

                            config["paper_auto_upgrade"]["current_version"] = version;

                            // Essentially same code as Generate_Config()

                            using(StreamWriter writer = File.CreateText(config_file))
                            {
                                config.WriteTo(writer);
                                writer.Flush();
                            }
                        }

                        int paper_current = config["paper_auto_upgrade"]["current_version"];
                        int paper_latest = Convert.ToInt16(regex[regex.Count - 1].Value);
                        string paper_file = $"{config["paper_auto_upgrade"]["latest_file_name"]}.jar";

                        //Console.WriteLine(current_version);
                        //Console.WriteLine(latest_version);

                        if(paper_current < paper_latest || !File.Exists(paper_file))
                        {
                            if(File.Exists(config["paper_auto_upgrade"]["latest_file_name"])) 
                            {
                                Console.WriteLine($"Current Paper Version {current_version} is outdated!");
                            }

                            else Console.WriteLine("Paper file does not exist!");
                            
                            Console.Write($"Downloading Latest Paper ({paper_latest}) for {mc_version} please wait... ");

                            var web = new WebClient();
                            web.Proxy = null;

                            string url = $"https://papermc.io/api/v2/projects/paper/versions/{mc_version}"
                            + $"/builds/{paper_latest}/downloads/paper-{mc_version}-{paper_latest}.jar";

                            web.DownloadFile(url, paper_file);

                            Console.WriteLine("Ok!");
                        }

                        else Console.WriteLine("Paper is up to date!");
                    }

                    catch(Exception err)
                    { 
                        Japa.WriteColorLine("<red|ERROR!> Maybe the URL changed? Is your internet down?"); 
                        Console.WriteLine(err);
                    }
                }
              
                Program.RefreshScreen("Server is now running...");

                ProcessStartInfo sinfo = new ProcessStartInfo();
                sinfo.UseShellExecute = false;
                sinfo.FileName = "java";

                sinfo.Arguments = $"-Xms{initial_ram_allocation}M -Xmx{max_ram_allocation}M "
                + $"-jar \"{jar_file}\" {(config["general"]["graphics_mode"] ? "" : "nogui")}";

                Process process = Process.Start(sinfo);
                process.WaitForExit();

                if(config["general"]["pause_on_jar_closure"])
                {
                    Japa.HaltProgram();
                }
                
                Environment.Exit(0);
            }
        }
        static void RefreshScreen(string input)
        {
            Console.Clear();

            Console.Title = $"{Program.title} - {input}";
        }

        static void Generate_Config()
        {
            TomlTable toml = new TomlTable
            {
                ["general"] = new TomlTable
                {
                    ["debug_mode"] = true,
                    ["graphics_mode"] = false,

                    ["pause_on_jar_closure"] = new TomlBoolean
                    {
                        Value = true,
                        Comment = "If the server closes or the JAR crashes, will pause the prompt instead of closing it immediately"
                    },

                    // Scrapped for now since it would not work on Linux
                    // ["java_version"] = new TomlString
                    // {
                    //     Value = "default",
                    //     Comment = "Specifies the Java Folder if you have to, else use PC's Default Java (Example: jdk-16.0.2)"
                    // },

                    ["java_initial_allocation_xms"] = new TomlInteger
                    {   
                        Value = 256,
                        Comment = "Both Initial and Max Allocation are in MB (256MB of RAM)"
                    },

                    ["java_max_allocation_xmx"] = 1536,

                    ["use_last_jar_found"] = new TomlBoolean
                    {
                        Value = true,
                        Comment = "Both will be overrided if \"paper_auto_upgrade\" is enabled"
                    },

                    ["jar_preffix"] = "paper",
                },

                ["paper_auto_upgrade"] = new TomlTable
                {
                    ["enabled"] = new TomlBoolean
                    {
                        Value = false,
                        Comment = "If enabled, the program will automatically download the latest JAR for the specified version.\n"
                        + " Program will also use \"paper-latest.jar\" instead of the JAR found by the preffix."
                    },

                    ["spigot_version"] = "1.17.1",
                    ["latest_file_name"] = "paper-latest",
                }
            };

            //File.WriteAllText(config_file, config);
            using(StreamWriter writer = File.CreateText(config_file))
            {
                toml.WriteTo(writer);
                writer.Flush();
            }
        }
    }

    public class Japa
    {
        public static void WriteColor(string phrase)
        {
            Regex colors = new Regex("([^\\<]+)|(?:\\<([a-z]+)\\|([^\\>]+)\\>)");
            MatchCollection matches = colors.Matches(phrase);

            for(int i = 0; i < matches.Count; i++)
            {
                string current_match = Convert.ToString(matches[i]);

                if(current_match.Contains("<"))
                {
                    string[] array = current_match.Split("|");
                    string current_color = array[0].Replace("<", "");
                    string current_text = array[1].Replace(">", "");

                    string color_name = current_color.Substring(0, 1).ToUpper() + current_color.Substring(1, current_color.Length - 1);

                    //Console.WriteLine(current_color);
                    //Console.WriteLine(color_name);
                    //Console.WriteLine(current_text);
                    
                    try
                    {
                        Type type = typeof(ConsoleColor);
                        Console.ForegroundColor = (ConsoleColor)Enum.Parse(type, color_name);
                    }

                    catch(Exception err) 
                    { 
                        Console.WriteLine("WriteColor Error: Invalid color!"); 
                        Console.WriteLine(err);
                    }

                    //Console.Write(current_text); 

                    Console.Write(current_text);

                    //if(i + 1 != color_matches.Count) Console.Write(current_text); 
                    //else Console.WriteLine(current_text);
                }

                else Console.Write(current_match);

                Console.ForegroundColor = ConsoleColor.Gray;
                //Console.Write(current_match);
                //{
                    //if(i + 1 == color_matches.Count) Console.WriteLine(current_match);
                    //else Console.Write(current_match);
                //}
            }
        }
        public static void WriteColorLine(string phrase)
        {
            WriteColor(phrase);
            Console.Write("\n");
        }
        public static void PrintBoolean(bool input)
        {
            if(input) Japa.WriteColorLine("<green|TRUE>");
            else Japa.WriteColorLine("<red|FALSE>");
        }
        public static void HaltProgram()
        {
            Japa.WriteColorLine("\n<red|Program will now halt.>");
            Console.ReadLine();
            Environment.Exit(0);
        }
        public static void ErrorHalt(string input)
        {
            Japa.WriteColorLine($"\n<red|ERROR:> {input}");
            Japa.HaltProgram();
        }
        public static string FetchSync(string url, string accepted_responses)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = accepted_responses;

            var httpResponse = (HttpWebResponse)request.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {

                return streamReader.ReadToEnd();
            }
        }
    }
}
