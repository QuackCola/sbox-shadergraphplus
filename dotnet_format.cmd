@echo off

pushd "Editor"

echo running dotnet format on code in directory "%cd%"

dotnet format

popd