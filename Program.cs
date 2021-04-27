using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ProjectVersioner
{
    class Program
    {
        static string Version = "";
        static string Version4 => Version + ".0";

        static void Main(string[] args)
        {
            #region MakeUp Version
            Version = string.Empty;
#if COMMIT
            string commit = string.Empty;
#endif
            Regex reg = new(@"\d*\.\d*\.?\d*?");
            try
            {
                Version = RunCommand("git", "describe --tags");
#if COMMIT
                commit = RunCommand("git rev-parse HEAD").Replace("\n", "");
#endif
                Version = reg.Match(Version).Value;
            }
            catch { }


            if (args.Length > 1)
            {
                if (reg.IsMatch(args[1]))
                {
                    Version = reg.Match(args[1]).Value;
                }
                foreach(string tmp in args)
                {
                    string file = Path.GetFullPath(tmp);
                    if (File.Exists(file))
                    {
                        if (file.EndsWith("Info.plist")) { MakeIos(file); continue; }
                        if (file.EndsWith("AndroidManifest.xml")) { MakeAndroid(file); continue; }

                        string ext = Path.GetExtension(tmp);
                        if (ext.Equals(".cs")) { MakeAssembly(file); continue; }
                        if (ext.Equals(".csproj")) { MakeProject(file); continue; }

                    }
                }
            }
            else
            {
                Console.WriteLine("Type a version replacement or press enter to use last tag version [current: " + Version + "]");
                string line = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (reg.IsMatch(line))
                    {
                        Version = reg.Match(line).Value;
                    }
                }
            }
        }
        #endregion

        #region Helpers
        static void MakeIos(string path)
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                var db = 0;
            }
        }
        static void MakeAndroid(string path)
        {
            try
            {
                string versionCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString().Substring(0, 8);
                XmlDocument doc = new();
                doc.Load(path);

                doc.ChildNodes[1].Attributes["android:versionCode"].Value = versionCode;
                doc.ChildNodes[1].Attributes["android:versionName"].Value = Version;

                doc.Save(path);
            }
            catch (Exception ex)
            {
                var db = 0;
            }
        }

        static void MakeProject(string path)
        {
            try
            {
                XmlDocument doc = new();
                doc.Load(path);
                try { doc.GetElementsByTagName("Version").Item(0).InnerText = Version; } catch { }
                try { doc.GetElementsByTagName("FileVersion").Item(0).InnerText = Version4; } catch { }
                try { doc.GetElementsByTagName("AssemblyVersion").Item(0).InnerText = Version4; } catch { }
#if COMMIT
                try { doc.GetElementsByTagName("Description").Item(0).InnerText = commit; } catch { }
#endif
                doc.Save(path);
            }
            catch (Exception ex)
            {
                var db = 0;
            }
        }
        
        static void MakeAssembly(string path)
        {
            try
            {
                string text = File.ReadAllText(path);
                text = Regex.Replace(text, "(AssemblyF?i?l?e?Version\\(\")[\\w|.|\\*]*(\"\\))", "${1}" + Version4 + "${2}");
#if COMMIT
                text = Regex.Replace(text, "(AssemblyDescription\\(\\\")[\\w|.|\\*]*(\\\"\\))", "${1}" + commit + "${2}");
#endif
                File.WriteAllText(path, text);
            }
            catch (Exception ex)
            {
                var db = 0;
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
