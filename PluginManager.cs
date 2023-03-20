using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Dtwo.Json;
using Ionic.Zip;
using static System.Net.Mime.MediaTypeNames;

namespace Dtwo.Plugins
{
    public class PluginManager
    {
        private static Dictionary<string, PluginLoadContext> m_plugins = new Dictionary<string, PluginLoadContext>();
        private static Dictionary<string, Stream> m_streamAssemblies = new Dictionary<string, Stream>();
        private static List<Assembly> m_loadedAssemblies = new List<Assembly>();

        private static Dictionary<string, PluginInfos> m_pluginInfos = new();

        private static AssemblyLoadContext m_context;
        private static Assembly LoadAssembly(Stream stream)
        {
            Console.WriteLine("LoadAssembly");

            //Console.WriteLine("Load assembly : " + assemblyPath);

            if (m_context == null)
            {
                m_context = new AssemblyLoadContext("PluginsLoadContext", true);
                m_context.Resolving += Context_Resolving;
            }

            stream.Position = 0;

            return m_context.LoadFromStream(stream);

            //context.Resolving -= Context_Resolving;
            //context.Unload();
        }

        private static Stream LoadAssemblyStream(string assemblyPath)
        {
            Console.WriteLine("LoadAssemblyStream " + assemblyPath);

            string name = Path.GetFileName(assemblyPath);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(assemblyPath);

            if (m_streamAssemblies.ContainsKey(name))
            {
                return null;
            }

            MemoryStream stream = new MemoryStream();

            using (FileStream fs = File.Open(assemblyPath, FileMode.Open))
            {
                fs.CopyTo(stream);
                AddStreamAssembly(stream, name);
                fs.Close();
            }

            var infos = LoadInfos(Path.GetDirectoryName(assemblyPath), nameWithoutExtension);
            m_pluginInfos.Add(nameWithoutExtension, infos);

            return stream;
        }

        private static Stream LoadAssemblyStream(string pluginName, byte[] bytes)
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

        private static Assembly Context_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            Console.WriteLine("Context_Resolving");
            Console.WriteLine("name resolving" + assemblyName.Name);

            Stream stream = m_streamAssemblies[assemblyName.Name];
            stream.Position = 0;
            //var expectedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "plugins", assemblyName.Name + ".dll");
            return context.LoadFromStream(stream);
        }

        public static void UnloadPlugin(string pluginName)
        {
            //if (m_plugins.ContainsKey(pluginName))
            //{
            //    Console.WriteLine($"UnloadPlugin : plugin {pluginName} unloaded");
            //    m_plugins[pluginName].Unload();
            //    m_plugins[assemblyPath].EnterContextualReflection().Dispose();
            //    m_plugins.Remove(assemblyPath);
            //}
            //else
            //{
            //    Console.WriteLine($"UnloadPlugin : path {assemblyPath} not found");
            //}
        }

        private static List<Assembly> LoadAssemblies()
        {
            Console.WriteLine("Load assemblies");

            List<Assembly> assemblies = new List<Assembly>();
            //string[] files = System.IO.Directory.GetFiles(folderPath, "Dtwo.Plugins.*.dll");

            for (int i = 0; i < m_streamAssemblies.Count; i++)
            {
                var streamAsm = m_streamAssemblies.ElementAt(i);
                assemblies.Add(LoadAssembly(streamAsm.Value));
            }

            return assemblies;
        }
        
        private static PluginInfos LoadInfos(string folder, string assemblyName)
        {
            PluginInfos pluginInfos = null;
            string path = folder + "\\"+ assemblyName + ".Infos.json";

            if (File.Exists(path))
            {
                try
                {
                    string txt = File.ReadAllText(path);
                    pluginInfos = JSonSerializer<PluginInfos>.DeSerialize(txt);
                    return pluginInfos;
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"PluginManager : Error on load infos ({path} {ex.Message})");
                    return null;
                }
            }
            else
            {
                Console.WriteLine("Infos file not found for " + path);
            }

            return null;
        }

        public static List<T> LoadPlugins<T>(string folderPath) where T : PluginController
        {
            Console.WriteLine("Load plugins at path " + folderPath);

            LoadAssembliesStreams(folderPath);

            List<T> plugins = new List<T>();
            List<Assembly> assemblies = LoadAssemblies();

            try
            {
                for (int i = 0; i < assemblies.Count; i++)
                {
                    var asm = assemblies[i];

                    if (asm.FullName.Contains("Plugins") == false)
                    {
                        Console.WriteLine("Is not plugin " + asm.FullName);
                        continue;
                    }

                    PluginInfos infos = LoadInfos(folderPath, asm.GetName().Name);
                    T plugin = CreatePlugin<T>(assemblies[i], infos);
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
            catch (Exception ex)
            {
                return null;
            }

            return plugins;
        }


        public static List<T> LoadPlugins<T>(List<byte[]> bytes, string pwd, string otherPluginsFolder) where T : PluginController
        {
            return InternalLoadPlugins<T>(bytes, pwd, otherPluginsFolder);
        }

        private static List<T> InternalLoadPlugins<T>(List<byte[]> bytes, string pwd, string otherPluginsFolder) where T : PluginController
        {
            List<T> plugins = new List<T>();

            foreach (var b in bytes)
            {
                using (MemoryStream ms = new MemoryStream(b))
                {
                    ms.Position = 0;

                    using (ZipFile zip = ZipFile.Read(ms))
                    {
                        zip.Encryption = EncryptionAlgorithm.WinZipAes256;

                        foreach (ZipEntry entry in zip)
                        {
                            if (Path.GetExtension(entry.FileName) == ".dll")
                            {
                                using (MemoryStream dllMs = new MemoryStream()) // Todo : opti
                                {
                                    entry.Encryption = EncryptionAlgorithm.WinZipAes256;
                                    entry.ExtractWithPassword(dllMs, pwd);
                                    dllMs.Position = 0;
                                    byte[] dllBytes = new byte[dllMs.Length];
                                    dllMs.Read(dllBytes, 0, dllBytes.Length);

                                    LoadAssemblyStream(entry.FileName, dllBytes);
                                }
                            }

                            else if (entry.FileName.Contains(".Infos.json"))
                            {
                                PluginInfos infos = null;
                                try
                                {
                                    using (MemoryStream dllMs = new MemoryStream()) // Todo : opti
                                    {
                                        entry.Encryption = EncryptionAlgorithm.WinZipAes256;
                                        entry.ExtractWithPassword(dllMs, pwd);
                                        dllMs.Position = 0;
                                        byte[] dllBytes = new byte[dllMs.Length];
                                        dllMs.Read(dllBytes, 0, dllBytes.Length);

                                        infos = JSonSerializer<PluginInfos>.DeSerialize(System.Text.Encoding.ASCII.GetString(dllBytes));
                                    }

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
            }

            LoadAssembliesStreams(otherPluginsFolder);
            List<Assembly> assemblies = LoadAssemblies();

            try
            {
                for (int i = 0; i < assemblies.Count; i++)
                {
                    var asm = assemblies[i];

                    if (asm.FullName.Contains("Plugins") == false)
                    {
                        Console.WriteLine("Is not plugin " + asm.FullName);
                        continue;
                    }

                    PluginInfos infos = m_pluginInfos[asm.GetName().Name];
                    T plugin = CreatePlugin<T>(assemblies[i], infos);
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
            catch (Exception ex)
            {
                return null;
            }

            return plugins;
        }

        private static void LoadAssembliesStreams(string folderPath)
        {
            Console.WriteLine("LoadPluginStreams");

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


        public static T CreatePlugin<T>(Assembly assembly, PluginInfos infos) where T : PluginController
        {
            try
            {
                int i = 0;
                Console.WriteLine("Create plugin " + assembly.FullName);

                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(T).IsAssignableFrom(type))
                    {
                        T result = Activator.CreateInstance(type, infos, assembly) as T;
                        if (result != null)
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