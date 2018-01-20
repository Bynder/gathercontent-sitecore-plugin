using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("Brimit LLC")]
[assembly: AssemblyCopyright("Copyright © 2018")]

#if SC90
[assembly: AssemblyProduct("GatherContent.Connector 2.11 SC9.0+")]
#else
#if SC81
[assembly: AssemblyProduct("GatherContent.Connector 2.11 SC8.1-SC8.2")]
#else
#if SC80
[assembly: AssemblyProduct("GatherContent.Connector 2.11 SC80")]
#else
#if SC72
 [assembly: AssemblyProduct("GatherContent.Connector 2.11 SC7.2")]
#else
[assembly: AssemblyProduct("GatherContent.Connector 2.11")]
#endif
#endif
#endif
#endif
[assembly: AssemblyVersion("1.17.11.30")]
[assembly: AssemblyFileVersion("1.17.11.30")]