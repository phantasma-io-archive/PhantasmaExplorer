using System.Collections.Generic;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;

namespace Phantasma.Explorer.Infrastructure.Data
{
    ////todo get db stuff from local node?
    //public class Repository : IRepository
    //{
    //    public Nexus NexusChain { get; set; }

    //    public decimal GetAddressNativeBalance(Address address, string chainName = null)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public IEnumerable<Address> GetAddressList(string chainAddress = null, int lastAddressAmount = 20)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public Address GetAddress(string address)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public IEnumerable<Block> GetBlocks(string chainAddress = null, int lastBlocksAmount = 20)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public Block GetBlock(string hash)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public Block GetBlock(int height, string chainAddress = null)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public Block GetBlock(Transaction tx)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public IEnumerable<Chain> GetAllChains()
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public IEnumerable<string> GetChainNames()
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public Chain GetChain(string chainAddress)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public Chain GetChainByName(string chainName)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public int GetChainCount()
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public IEnumerable<Transaction> GetTransactions(string chainAddress, int txAmount)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public IEnumerable<Transaction> GetAddressTransactions(Address address, int amount = 20)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public int GetAddressTransactionCount(Address address, string chainName)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public Transaction GetTransaction(string txHash)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public int GetTotalTransactions()
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public IEnumerable<Token> GetTokens()
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public Token GetToken(string symbol)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public IEnumerable<Transaction> GetLastTokenTransfers(string symbol, int amount)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public int GetTokenTransfersCount(string symbol)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public string GetEventContent(Block block, Event evt)
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}
}
