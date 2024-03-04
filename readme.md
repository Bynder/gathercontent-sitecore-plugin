1. Install Sitecore locally (e.g. 'Graphical setup package for XM Scaled' from `https://dev.sitecore.net/Downloads/Sitecore_Experience_Platform/103/Sitecore_Experience_Platform_103_Update1.aspx`)
    - prefix 'bynder' gives 'bynderidentityserver.dev.local' and 'byndercm.dev.local' hostnames by default
    - enable SXA during installation or install it manually
1. Install the latest CLI services package, e.g. `https://dev.sitecore.net/Downloads/Sitecore_CLI/5x/Sitecore_CLI_52113.aspx`
1. run `cli-init.ps1` to restore SC CLI, login into the Sitecore and push the items
