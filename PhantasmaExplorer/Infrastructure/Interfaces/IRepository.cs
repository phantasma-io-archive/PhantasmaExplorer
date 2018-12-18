﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;

namespace Phantasma.Explorer.Infrastructure.Interfaces
{
    public interface IRepository//<T> where T : BaseDbEntity
    {
        //todo calls should be async imo
        Nexus NexusChain { get; set; }//todo remove

        //Chain RootChain { get; set; }
        //Dictionary<Hash, Block> Blocks { get; set; }
        //Dictionary<string, Chain> Chains { get; set; }
        //Dictionary<string, Token> Tokens { get; set; }


        Task InitRepo();

        decimal GetAddressNativeBalance(Address address, string chainName = null);

        decimal GetAddressBalance(Address address, Token token, string chainName);

        IEnumerable<Address> GetAddressList(string chainAddress = null, int lastAddressAmount = 20);

        Address GetAddress(string address);

        IEnumerable<Block> GetBlocks(string chainAddress = null, int lastBlocksAmount = 20);

        Block GetBlock(string hash);

        Block GetBlock(int height, string chainAddress = null);

        IEnumerable<Chain> GetAllChains();

        IEnumerable<string> GetChainNames();

        Chain GetChain(string chainAddress);

        Chain GetChainByName(string chainName);

        int GetChainCount();

        IEnumerable<Transaction> GetTransactions(string chainAddress = null, int txAmount = 20);

        IEnumerable<Transaction> GetAddressTransactions(Address address, int amount = 20);

        int GetAddressTransactionCount(Address address, string chainName);

        Transaction GetTransaction(string txHash);

        int GetTotalTransactions();

        IEnumerable<Token> GetTokens();

        Token GetToken(string symbol);

        IEnumerable<Transaction> GetLastTokenTransfers(string symbol, int amount);

        int GetTokenTransfersCount(string symbol);

        string GetEventContent(Block block, Event evt);

        IEnumerable<AppInfo> GetApps();
    }
}
