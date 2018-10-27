using System.Collections.Generic;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;

namespace Phantasma.Explorer.Infrastructure.Interfaces
{
    public interface IRepository//<T> where T : BaseDbEntity
    {
        //todo calls should be async imo
        Nexus NexusChain { get; set; }//todo remove
        decimal GetAddressBalance(Address address, string chainName = "");

        IEnumerable<Address> GetAddressList(string chainAddress = "", int lastAddressAmount = 20);

        Address GetAddress(string address);

        IEnumerable<Block> GetBlocks(string chainAddress = "", int lastBlocksAmount = 20);

        Block GetBlock(string hash);

        Block GetBlock(int height, string chainAddress = "");

        IEnumerable<Chain> GetAllChains();

        IEnumerable<string> GetChainNames();

        Chain GetChain(string chainAddress);

        Chain GetChainByName(string chainName);

        IEnumerable<Transaction> GetTransactions(string chainAddress = null, int txAmount = 20);

        Transaction GetTransaction(string txHash);

        IEnumerable<Token> GetTokens();

        Token GetToken(string symbol);

        string GetEventContent(Block block, Event evt);
    }
}
