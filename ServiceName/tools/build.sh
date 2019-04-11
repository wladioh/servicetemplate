#!/usr/bin/env bash
SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
TOOLS_DIR=/tmp/cake/tools
CAKE_CORECLR=$TOOLS_DIR/Cake.CoreCLR
CAKE_DLL=$CAKE_CORECLR/Cake.dll

# Make sure the tools folder exists.
if [ ! -d "$TOOLS_DIR" ]; then
	mkdir -p "$TOOLS_DIR"
fi

# Install Cake.CoreCLR
if [ ! -d "$CAKE_CORECLR" ]; then
	CAKE_CORECLR_URL=https://www.nuget.org/api/v2/package/Cake.CoreCLR/
	echo "Downloading Cake.CoreCLR from $CAKE_CORECLR_URL .."
	curl -Lsfo Cake.CoreCLR.zip $CAKE_CORECLR_URL && unzip -q Cake.CoreCLR.zip -d "$TOOLS_DIR/Cake.CoreCLR" && rm -f Cake.CoreCLR.zip
	if [ $? -ne 0 ]; then
		echo "An error occurred while fetching Cake.CoreCLR from nuget."
		exit 1
	fi
fi

if [ ! -f "$CAKE_DLL" ]; then
	echo "Could not find $CAKE_DLL."
	exit 1
fi

exec dotnet "$CAKE_DLL" "$@"