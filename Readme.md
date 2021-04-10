# ModContentDiffer

A tModLoader mod for modders to help generate diff files of the content that changed between mod versions. Client only. Report any bugs on the github homepage.

Simply having the mod enabled will take a snapshot of all currently loaded mods (except this one) and create a list of all relevant content added, exported as a file (My Games/Terraria/ModLoader/ModContentDiffer on Windows). If the version of a mod increases, it creates a diff file ("changelog") with the highest previous version.

If you want to generate diff files retroactively, get an older version of the mod you want to compare against, and load that. Whenever you load a newer version, it will generate a diff (not all previous versions separately; if you wish to, remove the version files inbetween temporarily).

Keep the mod enabled at all times for a consistent result.