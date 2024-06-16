# BinAppender

This is a .NET console application that is supposed to act like the `~/.local/bin` in unix-like systems but with batch scripts instead of binaries, that act like shortcuts, that execute their given files.

**This program serves the whole purpose of not having to add the directory of a program to the `PATH` environment variable.**

## Installation

1. Install the .NET 7 SDK at <https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.302-windows-x64-installer>

2. Clone the repo

        git clone https://github.com/JupperTV/BinAppender.git

3. Build the project to get the `BinAppender\bin` directory used for building and running the program

        dotnet build

## What this program essentially does

- When executed for the first time:
   1. It creates the environment variable `%BinAppender_BinPath%` with the path to the directory for the batch scripts as its value
   2. It adds `%BinAppender_BinPath%` to `%Path%`
- When adding Write a batch file, to the path of `%BinAppender_BinPath%`, that looks like this:

        @echo off
        "(path to the executable provided by the user)" %*
