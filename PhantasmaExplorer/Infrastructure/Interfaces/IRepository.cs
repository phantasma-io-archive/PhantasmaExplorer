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
        decimal GetAddressBalance(Address address);

        List<Address> GetAddressList(string chainAddress = "", int lastAddressAmount = 20);

        Address GetAddress(string address);

        List<Block> GetBlocks(string chainAddress = "", int lastBlocksAmount = 20);

        Block GetBlock(string hash);

        Block GetBlock(int height, string chainAddress = "");

        Block GetBlock(Transaction tx);

        List<Chain> GetAllChains();

        Chain GetChain(string chainAddress);

        List<Transaction> GetTransactions(string chainAddress = "", int txAmount = 20);

        Transaction GetTransaction(string txHash);

        List<Token> GetTokens();

        Token GetToken(string symbol);

        string GetEventContent(Block block, Event evt);
    }
}
