using System.Collections.Generic;
using System.Threading.Tasks;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Models;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.Infrastructure.Interfaces
{
    public interface IRepository
    {
        Task InitRepo();

        decimal GetAddressNativeBalance(Address address, string chainName = null);

        decimal GetAddressBalance(Address address, TokenDto token, string chainName);

        IEnumerable<Address> GetAddressList(string chainAddress = null, int lastAddressAmount = 20);

        Address GetAddress(string address);

        IEnumerable<BlockDto> GetBlocks(string chainAddress = null, int lastBlocksAmount = 20);

        BlockDto GetBlock(string hash);

        BlockDto GetBlock(int height, string chainAddress = null);

        IEnumerable<ChainDto> GetAllChainsInfo();

        IEnumerable<ChainDataAccess> GetAllChains();

        IEnumerable<string> GetChainNames();

        ChainDataAccess GetChain(string chainInput);

        int GetChainCount();

        IEnumerable<TransactionDto> GetTransactions(string chainAddress = null, int txAmount = 20);

        IEnumerable<TransactionDto> GetAddressTransactions(Address address, int amount = 20);

        int GetAddressTransactionCount(Address address, string chainName);

        int GetTotalChainTransactionCount(string chain);

        TransactionDto GetTransaction(string txHash);

        int GetTotalTransactions();

        IEnumerable<TokenDto> GetTokens();

        TokenDto GetToken(string symbol);

        IEnumerable<TransactionDto> GetLastTokenTransfers(string symbol, int amount);

        int GetTokenTransfersCount(string symbol);

        string GetEventContent(BlockDto block, EventDto evt);

        IEnumerable<AppDto> GetApps();

        BlockDto FindBlockForTransaction(string txHash);
    }
}
