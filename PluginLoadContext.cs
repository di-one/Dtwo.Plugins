using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;

namespace Dtwo.Plugins
{
    class PluginLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;
        private Assembly m_root;

        public PluginLoadContext(string pluginPath, Assembly assemblyRoot) : base(true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
            m_root = assemblyRoot;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

            //var alreadyLoadedAssembly = m_root.GetReferencedAssemblies().FirstOrDefault(x => x.FullName == assemblyName.FullName);
            //Assembly assembly = null;

            //if (object.ReferenceEquals(null, alreadyLoadedAssembly))
            //{
            //    Console.WriteLine("alreadyLoadedAssembly == false");
            //}
            //else
            //{
            //    Console.WriteLine("alreadyLoadedAssembly == true");
            //}

            //if (assemblyPath != null)
            //{
            //    Assembly foundedAsm = PluginManager.LoadedAssemblies.Find(x => x.FullName == assemblyName.FullName);
            //    if (foundedAsm != null)
            //    {
            //        Console.WriteLine("Founded asm");
            //        return base.Load(assemblyName);
            //    }
            //    else
            //    {
            //        Console.WriteLine("Not founded asm " + assemblyPath);
            //        return LoadFromAssemblyPath(assemblyPath);
            //    }

            //    //    Console.WriteLine("Return true")
            //}

            return null;
                //////}
                //////else
                //////{
                //////    Console.WriteLine("alreadyLoadedAssembly == true");
                //////    assembly = System.Reflection.Assembly.GetExecutingAssembly();
                //////}

                //Console.WriteLine("return false");
                //return null;

                //string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            //    if (assemblyPath != null)
            //{
            //    return LoadFromAssemblyPath(assemblyPath);
            //}

            //return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
