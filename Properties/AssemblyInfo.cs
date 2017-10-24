// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Owin;

[assembly: AssemblyTitle("Web")]
[assembly: AssemblyDescription("AspDotNetStorefront Web Site")]
[assembly: AssemblyCompany("AspDotNetStorefront.com")]
[assembly: AssemblyProduct("AspDotNetStorefront MultiStore")]
[assembly: AssemblyCopyright("Copyright AspDotNetStorefront")]

[assembly: AssemblyVersion("10.0.5")]
[assembly: AssemblyFileVersion("10.0.5.3891")]
[assembly: AssemblyInformationalVersion("71eff4c99243b7f9178acc597076433749c02f33")]

[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

[assembly: OwinStartup(typeof(AspDotNetStorefront.Application.MvcApplication), "Owin_Start")]
