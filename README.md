# Canopy Simulations Command Line Interface (Canopy CLI)

The Canopy CLI is a command line tool providing advanced functionality for users of the Canopy Simulations portal, including support for monitoring a folder for tokens downloaded via the Canopy Portal, which in turn trigger it to download studies faster and more reliably than can be achieved directly through the Portal.

The full source code is available from this repository as a reference to using the Canopy Simulations API.

## Downloading Releases

Specific releases are available from the [releases page](https://github.com/CanopySimulations/canopy-cli/releases). 

You can download the binaries for either Windows or Mac with each release. For the best experience, once you have extracted
the files to a folder you can add the folder to your system's path.

## Getting Started

Once the Canopy CLI is downloaded and on path, you need to connect to an instance of the API:

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

```
canopy download-monitor --help
```

## Downloading Using a Package Manager

We are no longer publishing the CLI via chocolatey. If you have previously installed it this way you 
can remove the chocolatey source by running the following:

```
choco source remove -n=canopy
```

And you can uninstall the Canopy CLI by running the following:

```
choco uninstall canopy-cli
```

You can then download the latest release from our [releases page](https://github.com/CanopySimulations/canopy-cli/releases).