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

        decimal GetAddressBalance(Address address, TokenDto token, string chainName); //done

        string GetAddressName(string address);  //done

        IEnumerable<Address> GetAddressList(int lastAddressAmount = 20); //done

        IEnumerable<BlockDto> GetBlocks(string chainAddress = null, int lastBlocksAmount = 20);//done

        BlockDto GetBlock(string hash);//done

        BlockDto GetBlock(int height, string chainAddress = null);//done

        IEnumerable<ChainDto> GetAllChainsInfo();//dtone

        IEnumerable<ChainDataAccess> GetAllChains();//dtone

        IEnumerable<string> GetChainNames();//dtone

        string GetChainName(string chainAddress);//dtone

        ChainDataAccess GetChain(string chainInput); //done

        int GetChainCount();//done 

        IEnumerable<TransactionDto> GetTransactions(string chainAddress = null, int txAmount = 20);//done

        IEnumerable<TransactionDto> GetAddressTransactions(Address address, int amount = 20);//done

        int GetAddressTransactionCount(Address address, string chainName); //done

        int GetTotalChainTransactionCount(string chain);//done 

        TransactionDto GetTransaction(string txHash); //done

        int GetTotalTransactions(); //done

        IEnumerable<TokenDto> GetTokens(); //done

        TokenDto GetToken(string symbol); //done

        IEnumerable<TransactionDto> GetLastTokenTransfers(string symbol, int amount); //done?

        int GetTokenTransfersCount(string symbol); //done

        string GetEventContent(BlockDto block, EventDto evt);//todo move to utils?

        IEnumerable<AppDto> GetApps(); //done

        BlockDto FindBlockForTransaction(string txHash); //done
    }
}
