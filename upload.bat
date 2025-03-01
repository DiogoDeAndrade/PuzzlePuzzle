@echo off

:: Set the version variable
set version=1.0.1

:: Use the version variable in the command
c:\opt\butler\butler.exe push PuzzlePuzzle_v%version%.zip diogoandrade/puzzlepuzzle:windows --userversion %version%
c:\opt\butler\butler.exe push PuzzlePuzzleWeb_v%version%.zip diogoandrade/puzzlepuzzle:html5 --userversion %version%
