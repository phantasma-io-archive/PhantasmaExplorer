using System;
using System.Collections.Generic;
using Phantasma.Domain;
using Phantasma.SDK;

namespace Phantasma.Explorer
{
    public class Database
    {
        private Dictionary<string, IChain> _chains;
        private Dictionary<string, IToken> _tokens;
        private Dictionary<string, IPlatform> _platforms;
        private Dictionary<string, IBlock> _blocks;
        private Dictionary<string, ITransaction> _txs;

        private readonly API API;

        public Database(API api)
        {
            this.API = api;
        }

        public IEnumerable<IChain> Chains => _tokens.Values;
        public IEnumerable<IToken> Tokens => FetchTokens();

        public IEnumerable<IPlatform> Platforms => FetchPlatforms();

        private IEnumerable<IPlatform> FetchPlatforms()
        {
            throw new NotImplementedException();
        }

        private IEnumerable<IChain> FetchChains()
        {
            throw new NotImplementedException();
        }

        private IEnumerable<IToken> FetchTokens()
        {
        }
    }
}
