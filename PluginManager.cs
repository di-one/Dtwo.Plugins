using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Ionic.Zip;

namespace Dtwo.Plugins
{
    public class PluginManager
    {
        private static Dictionary<string, PluginLoadContext> m_plugins = new Dictionary<string, PluginLoadContext>();
        private static Dictionary<string, Stream> m_streamAssemblies = new Dictionary<string, Stream>();
        private static List<Assembly> m_loadedAssemblies = new List<Assembly>();

        private static Dictionary<string, PluginInfos> m_pluginInfos = new();

        private static AssemblyLoadContext? m_context;
        private static Assembly LoadAssembly(Stream stream)
        {
            Console.WriteLine("LoadAssembly");

            if (m_context == null)
            {
                m_context = new AssemblyLoadContext("PluginsLoadContext", true);
                m_context.Resolving += Context_Resolving;
            }

            stream.Position = 0;

            return m_context.LoadFromStream(stream);
        }

        private static Stream? LoadAssemblyStream(string assemblyPath)
        {
            string name = Path.GetFileName(assemblyPath);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(assemblyPath);

            if (m_streamAssemblies.ContainsKey(name))
            {
                System.Diagnostics.Debug.WriteLine("Assembly already loaded");
                return null;
            }

            MemoryStream stream = new MemoryStream();

            using (FileStream fs = File.Open(assemblyPath, FileMode.Open))
            {
                fs.CopyTo(stream);
                AddStreamAssembly(stream, name);
                fs.Close();
            }

            var directoryName = Path.GetDirectoryName(assemblyPath);

            if (directoryName == null)
            {
                System.Diagnostics.Debug.WriteLine("Directory name is null");
                return null;
            }

            var infos = LoadInfos(directoryName, nameWithoutExtension);

            if (infos == null)
            {
                System.Diagnostics.Debug.WriteLine("Infos is null");
                return null;
            }

            m_pluginInfos.Add(nameWithoutExtension, infos);

            return stream;
        }

        private static Stream? LoadAssemblyStream(string pluginName, byte[] bytes)
        {
            Console.WriteLine("LoadAssemblyStream " + pluginName);

            if (m_streamAssemblies.ContainsKey(pluginName))
            {
                return null;
            }

            MemoryStream stream = new MemoryStream();

            stream.Write(bytes, 0, bytes.Length);
            AddStreamAssembly(stream, pluginName);

            return stream;
        }

        private static void AddStreamAssembly(Stream stream, string assemblyName)
        {
            m_streamAssemblies.Add(assemblyName, stream);
        }

        private static Assembly? Context_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            if (assemblyName.Name == null)
            {
                return null;
            }


            Stream stream = m_streamAssemblies[assemblyName.Name];
            stream.Position = 0;

            return context.LoadFromStream(stream);
        }

        private static List<Assembly> LoadAssemblies()
        {
            List<Assembly> assemblies = new List<Assembly>();

            for (int i = 0; i < m_streamAssemblies.Count; i++)
            {
                var streamAsm = m_streamAssemblies.ElementAt(i);
                assemblies.Add(LoadAssembly(streamAsm.Value));
            }

            return assemblies;
        }
        
        private static PluginInfos? LoadInfos(string folder, string assemblyName)
        {
            PluginInfos? pluginInfos;
            string path = folder + "\\"+ assemblyName + ".Infos.json";

            if (File.Exists(path))
            {
                try
                {
                    string txt = File.ReadAllText(path);
                    pluginInfos = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginInfos>(txt);
                    return pluginInfos;
                }
                catch (Exception ex)
                {

                    System.Diagnostics.Debug.WriteLine($"PluginManager : Error on load infos ({path} {ex.Message})");
                    return null;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Infos file not found for " + path);
            }

            return null;
        }

        public static List<T>? LoadPlugins<T>(string folderPath) where T : PluginController
        {
            LoadAssembliesStreams(folderPath);

            List<T> plugins = new List<T>();
            List<Assembly> assemblies = LoadAssemblies();

            try
            {
                for (int i = 0; i < assemblies.Count; i++)
                {
                    var asm = assemblies[i];

                    var asmFullName = asm.FullName;

                    if (asmFullName == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Assembly is null");
                        continue;
                    }

                    if (asmFullName.Contains("Plugins") == false)
                    {
                        System.Diagnostics.Debug.WriteLine("Is not plugin " + asm.FullName);
                        continue;
                    }

                    var asmName = asm.GetName();
                    if (asmName == null || asmName.Name == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Assembly name is null");
                        continue;
                    }

                    PluginInfos? infos = LoadInfos(folderPath, asmName.Name);

                    if (infos == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Infos is null");
                        continue;
                    }

                    T? plugin = CreatePlugin<T>(assemblies[i], infos);
                    if (plugin != null)
                    {
                        plugins.Add(plugin);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Error on load plugin {infos.Name}");
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return plugins;
        }


        public static List<T>? LoadPlugins<T>(List<byte[]>? bytes, string pwd, string otherPluginsFolder) where T : PluginController
        {
            return InternalLoadPlugins<T>(bytes, pwd, otherPluginsFolder);
        }

        private static List<T>? InternalLoadPlugins<T>(List<byte[]>? bytes, string pwd, string otherPluginsFolder) where T : PluginController
        {
            List<T> plugins = new();

            // Load with bytes
            if (bytes != null)
            {
                foreach (var b in bytes)
                {
                    using MemoryStream ms = new MemoryStream(b);
                    ms.Position = 0;

                    using ZipFile zip = ZipFile.Read(ms);
                    zip.Encryption = EncryptionAlgorithm.WinZipAes256;

                    foreach (ZipEntry entry in zip)
                    {
                        if (Path.GetExtension(entry.FileName) == ".dll")
                        {
                            using MemoryStream dllMs = new MemoryStream(); // Todo : opti
                            entry.Encryption = EncryptionAlgorithm.WinZipAes256;
                            entry.ExtractWithPassword(dllMs, pwd);
                            dllMs.Position = 0;
                            byte[] dllBytes = new byte[dllMs.Length];
                            dllMs.Read(dllBytes, 0, dllBytes.Length);

                            LoadAssemblyStream(entry.FileName, dllBytes);
                        }

                        else if (entry.FileName.Contains(".Infos.json"))
                        {
                            PluginInfos? infos = null;
                            try
                            {
                                using MemoryStream dllMs = new MemoryStream();
                                entry.Encryption = EncryptionAlgorithm.WinZipAes256;
                                entry.ExtractWithPassword(dllMs, pwd);
                                dllMs.Position = 0;
                                byte[] dllBytes = new byte[dllMs.Length];
                                dllMs.Read(dllBytes, 0, dllBytes.Length);

                                infos = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginInfos>(System.Text.Encoding.ASCII.GetString(dllBytes));

                            }
                            catch (Exception ex)
                            {

                                Console.WriteLine($"PluginManager : Error on load infos ({ex.Message})");
                                return null;
                            }

                            if (infos == null)
                            {
                                Console.WriteLine($"PluginManager : Error on load infos");
                                return null;
                            }
                            else
                            {
                                m_pluginInfos.Add(entry.FileName.Replace(".Infos.json", ""), infos);
                            }
                        }
                    }
                }
            }


            // Load with folders
            LoadAssembliesStreams(otherPluginsFolder);
            List<Assembly> assemblies = LoadAssemblies();

            try
            {
                for (int i = 0; i < assemblies.Count; i++)
                {
                    var asm = assemblies[i];

                    if (asm == null)
                    {
                        Console.WriteLine("Assembly is null");
                        continue;
                    }

                    if (asm.FullName?.Contains("Plugins") == false)
                    {
                        Console.WriteLine("Is not plugin " + asm.FullName);
                        continue;
                    }

                    var asmName = asm.GetName();
                    if (asmName == null || asmName.Name == null)
                    {
                        Console.WriteLine("Assembly name is null");
                        continue;
                    }

                    PluginInfos infos = m_pluginInfos[asmName.Name];
                    T? plugin = CreatePlugin<T>(assemblies[i], infos);
                    if (plugin != null)
                    {
                        plugins.Add(plugin);
                    }
                    else
                    {
                        Console.WriteLine($"Error on load plugin {infos.Name}");
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return plugins;
        }

        private static void LoadAssembliesStreams(string folderPath)
        {
            string[] files = System.IO.Directory.GetFiles(folderPath, "*.dll");

            for (int i = 0; i < files.Length; i++)
            {
                LoadAssemblyStream(files[i]);
            }
        }

        private static void LoadAssembliesStreams(Dictionary<string, byte[]> asemblies)
        {
            for (int i = 0; i < asemblies.Count; i++)
            {
                var asm = asemblies.ElementAt(i);

                LoadAssemblyStream(asm.Key, asm.Value);
            }
        }


        public static T? CreatePlugin<T>(Assembly assembly, PluginInfos infos) where T : PluginController
        {
            try
            {
                int i = 0;

                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(T).IsAssignableFrom(type))
                    {
                        if (Activator.CreateInstance(type, infos, assembly) is T result)
                        {
                            return result;
                        }
                    }

                    i++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }

            string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
            Console.WriteLine(
                $"Can't find any type which implements PluginController in {assembly} from {assembly.Location}.\n" +
                $"Available types: {availableTypes}");

            return null;
        }
    }
}