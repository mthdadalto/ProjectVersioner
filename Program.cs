using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ProjectVersioner
{
    class Program
    {
        const string AutoParameter = "-auto";
        static string Version = "";
        static string Version4 = "";
        static readonly Regex reg = new(@"(\d+)\.(\d+)\.?(\d*)\.?(\d*)");

        static int ValidFiles = 0;

        static void Main(string[] args)
        {
            try
            {
                #region MakeUp Version
#if COMMIT
            string commit = string.Empty;
#endif
                MakeVersion(args);

                Console.WriteLine($"Using versions {Version} ({Version4})");


                foreach (string tmp in args)
                {
                    string file = Path.GetFullPath(tmp);
                    if (File.Exists(file))
                    {
                        DriveFile(file);
                    }
                }
                if (ValidFiles > 0) { return; }
                Console.WriteLine("No valid files passed.");
                while (true)
                {
                    Console.WriteLine("Input a valid file path or Return empty to finish.");
                    string file = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(file)) { break; }

                    DriveFile(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(nameof(Main));
                Console.WriteLine(ex.Message);
            }
        }
        #endregion

        #region Helpers

        static void MakeVersion(string[] args)
        {
#if COMMIT
            commit = RunCommand("git rev-parse HEAD").Replace("\n", "");
#endif

            //Try given version from arguments
            try
            {
                string ver = args.FirstOrDefault(x => reg.IsMatch(x));
                Match tmpMatch = reg.Match(ver);
                if (tmpMatch.Success)
                {
                    MakeVersion(tmpMatch);
                    return;
                }

            }
            catch { }


            //Try -Git from arguments
            try
            {
                if (args.Any(x => x.Equals(AutoParameter, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Match tmpMatch = reg.Match(RunCommand("git", "describe --tags"));
                    if (tmpMatch.Success)
                    {
                        MakeVersion(tmpMatch);
                        return;
                    }
                }
            }
            catch
            {
            }
            try
            {
                Console.WriteLine("Type a version replacement or press enter to use last tag version [current: " + Version + "] or pass "+ AutoParameter);
                string line = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Match tmpMatch = reg.Match(line);
                    if (tmpMatch.Success)
                    {
                        MakeVersion(tmpMatch);
                        return;
                    }
                }
            }
            catch
            {
            }
            Console.WriteLine("Failed to fetch a version. Please Fix!");
        }

        static void MakeVersion(Match tmpMatch)
        {
            try
            {
                Version = tmpMatch.Groups[1].Value + "." + tmpMatch.Groups[2].Value + "." + (string.IsNullOrWhiteSpace(tmpMatch.Groups[3]?.Value) ? "0" : tmpMatch.Groups[3].Value);
                Version4 = Version + "." + (string.IsNullOrWhiteSpace(tmpMatch.Groups[4].Value) ? "0" : tmpMatch.Groups[4].Value);
            }
            catch
            {
                Version = "0.0.0";
                Version4 = "0.0.0.0";
            }
        }



        static void DriveFile(string path)
        {
            if (path.EndsWith("Info.plist")) { MakeIos(path); return; }
            if (path.EndsWith("AndroidManifest.xml")) { MakeAndroid(path); return; }

            string ext = Path.GetExtension(path);
            if (ext.Equals(".cs")) { MakeAssembly(path); return; }
            if (ext.Equals(".csproj")) { MakeProject(path); return; }
        }

        static void MakeIos(string path)
        {
            try
            {
                Console.WriteLine(nameof(MakeIos) + ": " + path + " -> ");
                XDocument doc = XDocument.Load(path);
                try { doc.DocumentType.InternalSubset = null; } catch { }
                XElement plist = doc.Element("plist");
                XElement dict = plist.Element("dict");

                foreach (var el in dict.Elements())
                {
                    if (el.Value.Equals("CFBundleVersion"))
                    {
                        (el.NextNode as XElement).Value = Version;
                        break;
                    }
                }

                foreach (var el in dict.Elements())
                {
                    if (el.Value.Equals("CFBundleShortVersionString"))
                    {
                        (el.NextNode as XElement).Value = Version;
                        break;
                    }
                }

                doc.Save(path);
                Console.WriteLine("OK");
                ++ValidFiles;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        static void MakeAndroid(string path)
        {
            try
            {
                Console.WriteLine(nameof(MakeAndroid) + ": " + path + " -> ");
                string versionCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString().Substring(0, 8);
                XmlDocument doc = new();
                doc.Load(path);

                doc.ChildNodes[1].Attributes["android:versionCode"].Value = versionCode;
                doc.ChildNodes[1].Attributes["android:versionName"].Value = Version;

                doc.Save(path);
                Console.WriteLine("OK");
                ++ValidFiles;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void MakeProject(string path)
        {
            try
            {
                Console.WriteLine(nameof(MakeProject) + ": " + path + " -> ");
                XmlDocument doc = new();
                doc.Load(path);
                try { doc.GetElementsByTagName("Version").Item(0).InnerText = Version; } catch { }
                try { doc.GetElementsByTagName("FileVersion").Item(0).InnerText = Version4; } catch { }
                try { doc.GetElementsByTagName("AssemblyVersion").Item(0).InnerText = Version4; } catch { }
#if COMMIT
                try { doc.GetElementsByTagName("Description").Item(0).InnerText = commit; } catch { }
#endif
                doc.Save(path);
                Console.WriteLine("OK");
                ++ValidFiles;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void MakeAssembly(string path)
        {
            try
            {
                Console.WriteLine(nameof(MakeAssembly) + ": " + path + " -> ");
                string text = File.ReadAllText(path);
                text = Regex.Replace(text, "(AssemblyF?i?l?e?Version\\(\")[\\w|.|\\*]*(\"\\))", "${1}" + Version4 + "${2}");
#if COMMIT
                text = Regex.Replace(text, "(AssemblyDescription\\(\\\")[\\w|.|\\*]*(\\\"\\))", "${1}" + commit + "${2}");
#endif
                File.WriteAllText(path, text);
                Console.WriteLine("OK");
                ++ValidFiles;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static string RunCommand(string command)
        {
            int idx = command.IndexOf(" ");
            if (idx > 0) { return RunCommand(command.Substring(0, idx), command.Substring(idx + 1)); }
            return RunCommand(command, null);
        }

        static string RunCommand(string command, string args)
        {
            using Process proc = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };
            if (!string.IsNullOrWhiteSpace(args))
            {
                proc.StartInfo.Arguments = args;
            }
            proc.Start();

            return proc.StandardOutput.ReadLine();
        }

        public static string ReplaceGroup(string input, string pattern, RegexOptions options, string groupName, string replacement)
        {
            Match match;
            while ((match = Regex.Match(input, pattern, options)).Success)
            {
                var group = match.Groups[groupName];

                var sb = new StringBuilder();

                // Anything before the match
                if (match.Index > 0)
                    sb.Append(input.Substring(0, match.Index));

                // The match itself
                var startIndex = group.Index - match.Index;
                var length = group.Length;
                var original = match.Value;
                var prior = original.Substring(0, startIndex);
                var trailing = original.Substring(startIndex + length);
                sb.Append(prior);
                sb.Append(replacement);
                sb.Append(trailing);

                // Anything after the match
                if (match.Index + match.Length < input.Length)
                    sb.Append(input.Substring(match.Index + match.Length));

                input = sb.ToString();
            }

            return input;
        }
        #endregion
    }
}
