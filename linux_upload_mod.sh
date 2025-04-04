#!/bin/bash

mod_id="2931679448"
mod_name="SimplifiedMoveset"

prev_wd="$(pwd)"

cur_wd_relative="$(dirname "${BASH_SOURCE[0]}")"
cur_wd="$(cd $cur_wd_relative && pwd)"

$HOME/rain_world_uploader/rain_world_uploader.out "$mod_id" "./$mod_name"

cd "$prev_wd"
