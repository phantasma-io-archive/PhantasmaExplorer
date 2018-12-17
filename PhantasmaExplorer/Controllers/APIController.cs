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
            return _api.GetAccount(addressText);
        }

        public DataNode GetAddressTransactions(string addressText, int amount)
        {
            return _api.GetAddressTransactions(addressText, amount);
        }

        public DataNode GetApps()
        {
            return _api.GetApps();
        }

        public DataNode GetBlockByHash(string blockHash)
        {
            return _api.GetBlockByHash(blockHash);
        }

        public DataNode GetBlockByHeight(uint height, string chain)
        {
            var result = _api.GetBlockByHeight(chain, height);
            if (result == null)
            {
                if (Address.IsValidAddress(chain))
                {
                    result = _api.GetBlockByHeight(Address.FromText(chain), height);
                }
            }

            return result;
        }

        public DataNode GetBlockHeight(string chain)
        {
            return _api.GetBlockHeightFromChainName(chain) ?? _api.GetBlockHeightFromChainAddress(chain);
        }

        public DataNode GetBlockTransactionCountByHash(string block)
        {
            return _api.GetBlockTransactionCountByHash(block);
        }

        public DataNode GetChains()
        {
            return _api.GetChains();
        }

        public DataNode GetConfirmations(string txHash)
        {
            return _api.GetConfirmations(txHash);
        }

        public DataNode GetTransactionByBlockHashAndIndex(string blockHash, int index)
        {
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
