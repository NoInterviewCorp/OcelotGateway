#!/bin/bash

set -e
run_cmd="dotnet run --server.urls http://*:80"

>&2 echo "Executing command"
exec $run_cmd