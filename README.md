# Shoko Sample Plugins

Small, working Shoko Server plugins, each showing one part of the plugin API
the way it is meant to be used. Read them, copy the parts you need, and build
on them.

To start a plugin of your own, begin from the
[plugin template](https://github.com/ShokoAnime/Plugin-Template) instead: it is
a bare plugin that builds, loads and releases, and nothing more. Come back here
when you want to see how a particular feature is done. The
[plugin documentation](https://docs.shokoanime.com/daily/writing-plugins/getting-started)
covers the concepts behind all of it.

The samples build against `Shoko.Abstractions` 6.0.0-alpha.92.

## The samples

Each folder is a separate plugin, with its own plugin ID and `manifest.json`.

| Sample | What it shows |
|---|---|
| [`Shoko.Plugin.OriginalNameRelocator`](Shoko.Plugin.OriginalNameRelocator) | The smallest useful relocation provider: rename a file to its original release name, never move it, and return errors instead of throwing. |
| [`Shoko.Plugin.SampleWithSettingsRelocator`](Shoko.Plugin.SampleWithSettingsRelocator) | A relocation provider with settings stored per preset, honouring the rename and move switches, and choosing a destination with the relocation service's folder helpers. |
| [`Shoko.Plugin.SampleConfiguration`](Shoko.Plugin.SampleConfiguration) | A settings page from a plain class: a secret, descriptions from XML `<summary>` comments, a "Test Connection" button, reading the settings where they are used, and reacting when they are saved. |
| [`Shoko.Plugin.SampleEvents`](Shoko.Plugin.SampleEvents) | `IPlugin.Setup` and `IPlugin.Ready`, a hosted service that subscribes to file, release and series events safely, and a queue job that does the actual work. |
| [`Shoko.Plugin.SampleWebApi`](Shoko.Plugin.SampleWebApi) | API controllers and a SignalR hub under one route namespace, the startup and database gates, a global action and a series action, and running an action on behalf of the calling user. |
| [`Shoko.Plugin.SampleProviders`](Shoko.Plugin.SampleProviders) | A release info provider and a hash provider: found without any registration, identified by their type names, and disabled until a user turns them on. |
| [`Shoko.Plugin.SampleDashboard`](Shoko.Plugin.SampleDashboard) | A web page built with Vite and pnpm, compressed into the plugin at build time, and served by the plugin next to its own API. |

## What every sample does

Some things are the same in every plugin, so they are only explained once here.

- **The plugin class has a public parameterless constructor.** The server
  creates it before any services exist. A plugin that needs services takes them
  in `IPlugin.Setup` (see `Shoko.Plugin.SampleEvents`).
- **Everything the server looks for is `public`.** Providers, actions,
  configuration classes and the plugin class itself are found by scanning the
  assembly's public types. An `internal` one is silently ignored.
- **`Shoko.Abstractions` is referenced with `ExcludeAssets="runtime"`.** The
  server already has it, and a plugin that ships its own copy is skipped.
  `Shoko.QueueProcessor` also needs `native` excluded.
  `Directory.Build.targets` fails the build if one of these slips into the
  output.
- **`manifest.json` and `Shoko.BuildTools.Targets`** embed the plugin identity
  into the assembly. The `id` in the manifest must match `IPlugin.ID`.
- **IDs come from type names.** A provider's or action's ID is derived from its
  namespace and class name, and the user's settings are stored against it.
  Renaming or moving the class is a breaking change.
- **Plugins that serve HTTP use one namespace for all of it.** The API lives
  under `/api/plugin/<Namespace>/`, pages and assets under
  `/plugin/<Namespace>/`, and SignalR hubs under `/signalr/plugin/<Namespace>/`.
  Here each sample uses its own name, such as `SampleWebApi`.

The settings the projects share, such as the target framework and generating
the documentation file, are in `Directory.Build.props`. If you copy a project
out of this repository, copy those settings into its `.csproj` too.

## Building

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). To build
everything:

```sh
dotnet build
```

To build one sample, name its folder:

```sh
dotnet build Shoko.Plugin.SampleEvents
```

The dashboard sample also needs [Node.js](https://nodejs.org/) and
[pnpm](https://pnpm.io/installation) to build its page. Without them it still
builds, but the page is left out and the build says so. Pass
`-p:SkipDashboardBuild=true` to leave it out on purpose.

## Trying a sample

1. Build it, as above.
2. Copy the output folder, for example
   `Shoko.Plugin.SampleEvents/bin/Debug/net10.0/`, into the `plugins` folder in
   the server's data directory. Give the copy a name of its own, such as
   `plugins/SampleEvents/`.
3. Restart the server. A new plugin is enabled when the server first finds it,
   and it shows up in the plugin list in the Web UI.

The server log says whether each plugin loaded, and why not when it didn't.
The providers sample needs one more step before it does anything: enable the
release info provider on the release provider settings, and the hash
provider's hash type on the hashing settings.

## Where to go next

- [Plugin template](https://github.com/ShokoAnime/Plugin-Template), for
  starting a new plugin
- [Writing plugins](https://docs.shokoanime.com/daily/writing-plugins/getting-started),
  the plugin documentation
- The `README.md` files inside the `Shoko.Abstractions` source, which go into
  each part of the API in depth
