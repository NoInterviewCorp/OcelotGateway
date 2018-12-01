#!/bin/bash

set -e
# run_cmd="dotnet run --server.urls http://*:80"
run_cmd="dotnet run"

>&2 echo "Executing command"
exec $run_cmd