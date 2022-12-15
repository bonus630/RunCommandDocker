In many times i going to Macro Manager Docker to creates C# macros, c# macros have the advantage over addons becauses dont requires

close and reopen coreldraw all times when the codes changes, is very usuful to makes tests, but if you code breaks coreldraw crashes

and the visual studio will close then is heigher the risk to lost the lasts changes in code. Another disadvantageis require the

specifique version of visual studio.

To work around this disadvantage and maintains the "hot code edit" vantage, i have creates a addon docker whose monitor a folder to display a tree wich

contains dll,class,method.

this docker will permits editions in dll files and not required reopen coreldraw similary *Macro Manager Docker*, but you dont lost code if coreldraw crashes

permits the use any visual studio version, and use VB not only C#

Uses the same macro official template, located in the **[corel installation folder]/Data/VGCoreApp.vstax**, but only requires custom attributes to works fine, 

you can creates you self template [https://docs.microsoft.com/en-us/visu...](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-update-existing-templates?view=vs-2022)

i has create a c# template, this template copys the created dll to "*c:\cdrCommands*", you can edit this path editing the file "ClassLibrary2.csproj",in line 54 and 55,

VGCore reference will needs update to target coreldraw version

` `to use in visual studio copy the zip to **%USERPROFILE%\Documents\Visual Studio [version]\Templates\ProjectTemplates**


In monitored folder can puts many dll.To create the dll use Class Library Template .net framework (not core, not standart) in visual studio 

A dll can contains many class.

A class can contains many methods.

class require the attribute [**CgsAddInModule**] and [**CgsAddInConstructor**] in the constructor method, pass the coreldraw application object in the constructor

methods require the attribute [**CgsAddInMacro**]

this works like VSTA macros in Macro Manager.

after you can reuse yous dll in another projects.

I use many times the vsta macros to makes tests, i has create this tool to personal uses, but maybes helps you too 

Now we are have a variable inspector popup to returneds values from ours commands

Not chained command will run in async in background

A new UI zone to display details about selected command, command with params needs run by this field.

You can pass value types or another returned values from another commands to params, but be careful, i has implements a weak way to detect unbounded recursivity functions

A Shape range manager is been added, shape range is in my case the most used param passed to my methods, this manager stores the shapes statics id in a list, and recovery by method, you can add remove or clean , and add the shaperange get method to another command with shaperange param.

The returns inspector popup will open in mouse over event in return value field and take a little time to display, and will close after 1 second after mouse leave the popup.

Pin the most usedes commands to fast acess

Source code: <https://github.com/bonus630/RunCommandDocker>

Tests Demo 1: <https://youtu.be/LT8luj9DFoA> 

Tests Demo 2: <https://youtu.be/jotNg8oajJo>
