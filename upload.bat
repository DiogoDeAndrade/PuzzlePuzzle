@echo off

:: Set the version variable
set version=1.0.0

:: Use the version variable in the command
c:\opt\butler\butler.exe push PuzzlePuzzle_v%version%.zip diogoandrade/puzzle-puzzle:windows --userversion %version%
