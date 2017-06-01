# Canopy Simulations Command Line Interface (Canopy CLI)

The Canopy CLI is a command line tool providing advanced functionality for users of the Canopy Simulations portal.

The full source code is available from this repository as a reference to using the Canopy Simulations API.

Note this is an early version and new functionality will be added as requested by users.

## Downloading Releases

Specific releases are available from the [releases page](https://github.com/CanopySimulations/canopy-cli/releases). 
You can download the binaries for either Windows or Mac with each release.

## Downloading Using a Package Manager

Rather than downloading releases manually, you can use the [Chocolatey](https://chocolatey.org) package manager under windows to automate 
installing and updating the Canopy CLI.

After [installing Chocolatey](https://chocolatey.org/install) you should add a new source:

```
choco source add -n=canopy -s="https://ci.appveyor.com/nuget/canopy-cli-uigbxj0rajyl"
```

This gives Chocolatey access to a feed containing the `canopy-cli` releases.  You can then install the latest Canopy CLI version by running:

```
choco install canopy-cli
```

This downloads the Canopy CLI and adds it to your path.  When a new version is released you can update your CLI with:

```
choco upgrade canopy-cli
```

## Getting Started

Once the Canopy CLI is installed, you need to connect to an instance of the API:

```
canopy connect
```

You will be prompted for a client ID and a client secret.  These are available to customers by contacting Canopy Simulations.

Once connected, you can authenticate with the same credentials you use on the Canopy Portal:

```
canopy authenticate
```

After authenticating, you can run other commands.  For a full list of commands run:

```
canopy --help
```

For information on a specific command, for example the `get-schemas` command, type the command followed by `--help`:

```
canopy get-schemas --help
```
