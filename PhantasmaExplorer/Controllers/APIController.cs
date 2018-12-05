using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.API;
using LunarLabs.Parser;
using Phantasma.Cryptography;

namespace Phantasma.Explorer.Controllers
{
    public class ApiController
    {
        private readonly NexusAPI _api;

        public ApiController(IRepository repo)
        {
            _api = new NexusAPI(repo.NexusChain);
        }

        public DataNode GetAccount(string addressText)
        {
            var address = Address.FromText(addressText);
            return _api.GetAccount(address);
        }

        public DataNode GetBlock(string blockHash)
        {
            var hash = Hash.Parse(blockHash);
            return _api.GetBlockByHash(hash);
        }

        public DataNode GetBlock(uint height, string chainName)
        {
            return _api.GetBlockByHeight(chainName, height);
        }

        public DataNode GetAddressTransactions(string addressText, int amount)
        {
            var address = Address.FromText(addressText);
            return _api.GetAddressTransactions(address, amount);
        }
    }
}
