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

        public DataNode GetAddressTransactions(string addressText, int amount)
        {
            var address = Address.FromText(addressText);
            return _api.GetAddressTransactions(address, amount);
        }

        public DataNode GetApps()
        {
            return _api.GetApps();
        }

        public DataNode GetBlockByHash(string blockHash)
        {
            var hash = Hash.Parse(blockHash);
            return _api.GetBlockByHash(hash);
        }

        public DataNode GetBlockByHeight(uint height, string chainName)
        {
            return _api.GetBlockByHeight(chainName, height);
        }

        public DataNode GetBlockNumber(string chain)
        {
            return _api.GetBlockNumber(chain) ?? _api.GetBlockNumber(Address.FromText(chain));
        }

        public DataNode GetBlockTransactionCountByHash(string block)
        {
            var blockHash = Hash.Parse(block);
            return _api.GetBlockTransactionCountByHash(blockHash);
        }

        public DataNode GetChains()
        {
            return _api.GetChains();
        }

        public DataNode GetConfirmations(string txHash)
        {
            var hash = Hash.Parse(txHash);
            return _api.GetConfirmations(hash);
        }

        public DataNode GetTransactionByBlockHashAndIndex(string block, int index)
        {
            var blockHash = Hash.Parse(block);
            return _api.GetTransactionByBlockHashAndIndex(blockHash, index);
        }

        public DataNode GetTokens()
        {
            return _api.GetTokens();
        }

        public DataNode SendRawTransaction(string signedTx)
        {
            return _api.SendRawTransaction(signedTx);
        }

    }
}
