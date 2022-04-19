# FFLogsViewer

Small plugin to view FFLogs ranking percentiles in-game using Dalamud provided by [XIVLauncher](https://github.com/goatcorp/FFXIVQuickLauncher).

To install, add `https://raw.githubusercontent.com/Aireil/MyDalamudPlugins/master/pluginmaster.json` under "Custom Plugin Repositories" in the "Experimental" tab of Dalamud's in game settings, and then install it through the plugin installer.

/fflogs to open the plugin window.  
/fflogsconfig to open the plugin config.

The /fflogs command supports most placeholders, see the [Lodestone database](https://eu.finalfantasyxiv.com/lodestone/playguide/db/text_command/placeholder/) for a list of them.

To get percentiles in the plugin window, you will need to add an API client, step-by-step guide in /fflogsconfig.  
No API client needed to use the context menu item when opening in a browser.

![image](https://github.com/Aireil/FFLogsViewer/raw/master/res/ui.png)

## Context menu

Currently disabled in 6.1, waiting for a fix.

Adds a context menu item nearly everywhere you have a name.  
Setting in config to open the character page in your default browser instead of the plugin window.

![image](https://github.com/Aireil/FFLogsViewer/raw/master/res/contextMenu1.png)
![image](https://github.com/Aireil/FFLogsViewer/raw/master/res/contextMenu2.png)

## Note
Code is a fiesta, feel free to bully me.
