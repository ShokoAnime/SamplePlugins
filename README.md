# Sample Plugins For Shoko
Honestly, the name is pretty self-explanatory. Shoko has a plugin system, and these are here to help you get started and show off some useful tricks.

## Prerequisites
- Make sure you have the dotnet core SDK. Get it from here: https://dotnet.microsoft.com/download
- While not required, it's much nicer to use something that isn't Notepad.
  1. [VSCode is free, lightweight, and is generally a great text editor](https://code.visualstudio.com/)
  2. [Rider is a fully featured IDE for developers, and is free for students and many others.](https://www.jetbrains.com/rider/) [You need to set up a jetbrains account for free stuff if you qualify](https://www.jetbrains.com/community/education/)
  3. [Visual Studio is old, huge, and laggy, but if you like suffering, there's a free version](https://visualstudio.microsoft.com/vs/community/)
- You need Shoko. These are plugins, and they need something to run them.
- (Optionally) You can use `git` to manage version control. It makes it easier to ask for advice, and if you lose your workspace, the code still exists on GitHub.

## Getting Started
1. Clone the repository. [Forking and cloning with GitHub](https://docs.github.com/en/github/getting-started-with-github/fork-a-repo)
2. Open the solution (SamplePlugins.sln) or folder with your IDE of choice. They each have their own methods for doing this.
3. Pick a project that sounds like what you might want. Each example has different feature implementations and levels of complexity.
4. Write some code! This may sound scary, but you'll find that it's infinitely easier to build a filename with code than it is with a custom script implementation like we had before. The best part about code is that it's really easy to ask for help. Use Google! You'll probably end up on StackOverflow, and that's a good thing.
   1. Ideally, we'll get some testing tools made, but for now, throw it in Shoko and see what it does. Shoko won't do any irreversible damage and has all of the info that Plugins can access, so it's easy to fix a mistake.
5. Build the plugin. Some IDEs have a convenient button, but you can also just use `dotnet build -c Release` in the project's directory. The resulting DLL will be in `bin/Release/netstandard2.0/`. Grab the one that has the same name as the project (ignore Shoko.Plugins.Abstractions.dll) and put it in Shoko's plugins folder (plugins folder inside Shoko's installation directory).
6. Start Shoko. The logs will say at the beginning of startup if the plugin was loaded successfully.
7. Fail and try again from step 4...Welcome to programming.....
