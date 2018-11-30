# TC .Net Interface
Personal Fork of https://sourceforge.net/projects/tcdotnetinterface/

# Original Description
If you are .NET developer and have a good idea for TC plugin - provided interface is the right choice for you.
You can concentrate on the main functionality of your plugin withouth having to worry about most of mundane tasks of TC plugin building.
Main features:
- use flexibility and power of .NET Framework to create new plugin,
- base classes for all TC plugin types - FS, Lister, Packer, and Content,
- easy debugging with included tracing system,
- all optional methods not implemented in managed plugin are excluded from the final TC plugin,
- TC calls are translated into managed calls with parameters marshaling,
- each plugin loads into separate Application Domain (AD) to provide isolation and security boundaries for executing managed code,
- unified loader located in Default AD loads all types of TC plugins,
- control over the lifetime policy for managed plugin instance,
- auto support for Unicode and 64-bit features,
- compact final binary files (usually < 100 KB)

# Author
olegy-293 
Des Plaines / United States / CST 

# Related projects I found so far
![GIS Viewer](https://github.com/gepcel/GisViewer)
![Markdown Viewer](https://github.com/wangzhfeng/MarkdownViewer)

# Why .Net?
.Net is very easy

# Why not use .Net
If one uses .Net it is dependent on the Framework beeing istalled in the correct verion that is required.
For that the plugin may not run correctly on some systems.

