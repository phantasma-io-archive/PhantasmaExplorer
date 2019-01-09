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
            //_api = new NexusAPI(repo.NexusChain); todo
        }

        public DataNode GetAccount(string addressText)
        {
            return APIUtils.FromAPIResult(_api.GetAccount(addressText));
        }

        public DataNode GetAddressTransactions(string addressText, int amount)
        {
            return APIUtils.FromAPIResult(_api.GetAddressTransactions(addressText, amount));
        }

        public DataNode GetApps()
        {
            return APIUtils.FromAPIResult(_api.GetApps());
        }

        public DataNode GetBlockByHash(string blockHash)
        {
            return APIUtils.FromAPIResult(_api.GetBlockByHash(blockHash));
        }

        public DataNode GetBlockByHeight(uint height, string chain)
        {
            var result = _api.GetBlockByHeight(chain, height);
            if (result == null)
            {
                if (Address.IsValidAddress(chain))
                {
                    result = _api.GetBlockByHeight(chain, height);
                }
            }

            return APIUtils.FromAPIResult(result);
        }

        public DataNode GetBlockHeight(string chain)
        {
            return APIUtils.FromAPIResult(_api.GetBlockHeightFromChain(chain));
        }

        public DataNode GetBlockTransactionCountByHash(string block)
        {
            return APIUtils.FromAPIResult(_api.GetBlockTransactionCountByHash(block));
        }

        public DataNode GetChains()
        {
            return APIUtils.FromAPIResult(_api.GetChains());
        }

        public DataNode GetConfirmations(string txHash)
        {
            return APIUtils.FromAPIResult(_api.GetConfirmations(txHash));
        }

        public DataNode GetTransactionByBlockHashAndIndex(string blockHash, int index)
        {
            return APIUtils.FromAPIResult(_api.GetTransactionByBlockHashAndIndex(blockHash, index));
        }

        public DataNode GetTokens()
        {
            return APIUtils.FromAPIResult(_api.GetTokens());
        }

        public DataNode SendRawTransaction(string signedTx)
        {
            return APIUtils.FromAPIResult(_api.SendRawTransaction(signedTx));
        }

    }
}
