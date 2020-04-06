# Phantasma Explorer V 2.0
Block explorer for Phantasma Chain

## Contents

- [Description](#description)
- [Development](#development)
- [Build](#build)
- [Run](#run)
- [Contributing](#contributing)
- [License](#license)

---

## Development
To perform development on the explorer you will need the following:

- A Windows PC that suports Visual Studio Community
  - Can be obtained here: https://visualstudio.microsoft.com/downloads/
- An installation of Visual Studio Community with the following extension
  - .NET Desktop Development
  
Pull or download the following GitHub Repositories
- PhantasmaChain [repository](https://github.com/phantasma-io/PhantasmaChain) 
- PhantasmaRpcClient [repository](https://github.com/phantasma-io/PhantasmaRpcClient)

Ensure both of these sit in the same root directory on your PC and are in folders that match the above. For example:
- C:\<my code>\Phantasma\PhantasmaChain
- C:\<my code>\Phantasma\PhantasmaRpcClient

## Build
- Open Visual Studio
- Open the PhantasmaSpook\Spook.sln solution
- Build the solution
- Publish the Phantasma.Explorer Project

The files needed to run a node will now be in PhantasmaExplorer\Phantasma.Explorer\Publish

## Run
Once you have publisheed the binaries as per above you can run it with the following command:
- Note the first run can take some time as it will build the cache from scratch

```
dotnet /<explorer root dir>/bin/Phantasma.Explorer.dll --port=7074 --env=prod --path=/<explorer root dir>/ -phantasma.rest=http://207.148.17.86:7078/api -cache.path=Cache
```

## Contributing

You can contribute to Phantasma with [issues](https://github.com/PhantasmaProtocol/PhantasmaChain/issues) and [PRs](https://github.com/PhantasmaProtocol/PhantasmaChain/pulls). Simply filing issues for problems you encounter is a great way to contribute. Contributing implementations is greatly appreciated.

## License

The Phantasma project is released under the MIT license, see `LICENSE.md` for more details.
