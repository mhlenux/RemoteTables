Releasing this project here. I don't have a Windows anymore (to run it) and don't remember much about this app, but I try to log here as much as I can. I won't be improving it as I do not use Windows anymore.

# About

Remote Windowses/Tables. Instead of sending over your whole desktop, this app sends over only specific windowses (or multiple). (as bring in poker botting world)

It was designed for poker botting (to send poker tables to another PC and avoid botting tools to be installed on a clean pc), but I wans't doing this for a long, so this app was born from that journey. Releasing this incase someone finds it useful, or has some other use for it. 

# how it works

Start server (on a clean pc), define WindowTitles you want to watch as explained below. Connect client and it should be auto starting and transforming/closing those windowses over to client. Both Client and Server have logging UI's (desktop apps) to see what is happening.

## Server

Define settings in appSettings.json. (will be generated after compilation in app dir)

- "WindowTitles": "NLH,Notepad" // Windowses that will be sent over to client, this name is readed from the window title. As soon as it spots one, it will transfer it automatically over. And close connection (and window in client) if it is closed.
- "ImageAmountSeed": "3", // This divides our window to example 3x3 grid. So not everything is sent over.. only what has changed.. example with 3x3 we have 9 squares, if only half of them change, then only half will be sended over. High number increases memory usage, low increases network traffic. 
- "DelaySend": "650", // dont set it too low, also in Botting, keeping it higher makes it look more human and less network traffic.
- "MinDelayReceive": "40", // I think with these I tried to randomize a bit Inputs from Client.. and also to have somekind of delays.
- "MaxDelayReceive": "120" // Previous was min, this is max it will randomize it.

## Client

Define server port and ip in appSettings.json. Client is in botting pc (that example OpenHoldem uses)

### HUD

Client also has this OHReader.cs, this was used to read some specific memory addresses example from "OpenHoldem.exe" and then It drawed a HUD over a Clients Poker table.. so you could get some specific information that you wanted to see. (for manual playing example, or debugging).

# Other 

In LowLevelHooks, we also are removing some keyboard and mouse input flags from server inputs, as those could be detected, by poker client. I'm no expert in this, and this was last thing i did here, so it can be improved.

# Compile 

Coded with Windows C# .NET 5 with WPF (for UIs) (Visual Studio). 
