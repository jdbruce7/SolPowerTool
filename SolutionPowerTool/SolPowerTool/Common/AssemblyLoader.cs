using System;
using System.Reflection;

namespace SolPowerTool.App.Common
{
    [Serializable]
    public class AssemblyLoader : MarshalByRefObject
    {
        private AssemblyName _getAssemblyFullName(string assemblyFile)
        {
            return Assembly.ReflectionOnlyLoadFrom(assemblyFile).GetName();
        }

        public static AssemblyName GetAssemblyFullName(string assemblyName)
        {
            AppDomain domain = AppDomain.CreateDomain("tempDomain",
                                                      AppDomain.CurrentDomain.Evidence,
                                                      new AppDomainSetup
                                                          {
                                                              ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                                                              ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                                                          });
            var loader = (AssemblyLoader) domain.CreateInstanceAndUnwrap(typeof (AssemblyLoader).Assembly.FullName, typeof (AssemblyLoader).FullName);
            AssemblyName name = loader._getAssemblyFullName(assemblyName);
            AppDomain.Unload(domain);
            return name;
        }
    }
}