#!/bin/bash
# The era demo's pure model, checked outside Unity in seconds (Jeffrey, #150 N4): Unity's bundled .NET 6 and csc compile
# Assets/EraDemo/EraMap.cs and EraDemoContent.cs with EraMapChecks.cs into a scratch folder and run it.
#   Tools/era-demo-harness/run.sh
set -e
cd "$(dirname "$0")/../.."
U=/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/Resources/Scripting
V=$(ls "$U/NetCoreRuntime/shared/Microsoft.NETCore.App/" | head -1); R="$U/NetCoreRuntime/shared/Microsoft.NETCore.App/$V"
OUT=$(mktemp -d); REFS=(); for f in "$R"/System*.dll "$R"/Microsoft*.dll "$R/netstandard.dll" "$R/mscorlib.dll"; do REFS+=("-r:$f"); done
"$U/NetCoreRuntime/dotnet" "$U/DotNetSdkRoslyn/csc.dll" -nologo -nostdlib -noconfig -out:"$OUT/checks.dll" "${REFS[@]}" \
  Tools/era-demo-harness/EraMapChecks.cs Assets/EraDemo/EraMap.cs Assets/EraDemo/EraDemoContent.cs | grep -v "CS8981\|CS1701\|CS1702" || true
echo "{\"runtimeOptions\":{\"tfm\":\"net6.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"$V\"}}}" > "$OUT/checks.runtimeconfig.json"
"$U/NetCoreRuntime/dotnet" "$OUT/checks.dll"
