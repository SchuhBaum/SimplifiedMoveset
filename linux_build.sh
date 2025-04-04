#!/bin/bash

cd "./SourceCode" || exit

config="Release"
dotnet build -c "$config"

mod_name="SimplifiedMoveset"
dll_name="$mod_name.dll"
pdb_name="$mod_name.pdb"

dest_path="../$mod_name/plugins"
mkdir -p $dest_path

src_file=$(find "bin/$config" -type f -name "$dll_name")
dest_file="$dest_path/$dll_name"
mv -f "$src_file" "$dest_file"

src_file=$(find "bin/$config" -type f -name "$pdb_name")
dest_file="$dest_path/$pdb_name"
mv -f "$src_file" "$dest_file"

cd ..

# Update ctags for nvim after compiling. Ctags contain the locations for things
# like function definitions.
ctags -R

if [ ! -d "$HOME/snap/steam/common/.local/share/Steam/steamapps/common/Rain World/RainWorld_Data/StreamingAssets/mods/SimplifiedMoveset" ]; then
    ln --symbolic "$HOME/snap/steam/common/.local/share/Steam/steamapps/common/Rain World/modding/simplifiedmoveset/SimplifiedMoveset" "$HOME/snap/steam/common/.local/share/Steam/steamapps/common/Rain World/RainWorld_Data/StreamingAssets/mods/SimplifiedMoveset"
fi
