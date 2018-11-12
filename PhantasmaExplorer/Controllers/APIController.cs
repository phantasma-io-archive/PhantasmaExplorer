using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.API;
using LunarLabs.Parser;
using Phantasma.Cryptography;

namespace Phantasma.Explorer.Controllers
{
    public class APIController
    {
        private IRepository Repository { get; set; }

        private NexusAPI _API;

        public APIController(IRepository repo)
        {
            Repository = repo;
            this._API = new NexusAPI(repo.NexusChain);
        }

        public DataNode GetAccount(string addressText)
        {
            var address = Address.FromText(addressText);
            return _API.GetAccount(address);
        }

        public DataNode GetBlock(string blockHash)
        {
            var hash = Hash.Parse(blockHash);
            return _API.GetBlock(hash);
        }

        public DataNode GetAddressTransactions(string addressText, int amount)
        {
            var address = Address.FromText(addressText);
            return _API.GetAddressTransactions(address, amount);
        }
    }
}
