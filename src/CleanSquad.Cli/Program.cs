using System;
using CleanSquad.Cli;

string output = await CliApplication.BuildOutputAsync(args);
Console.WriteLine(output);
