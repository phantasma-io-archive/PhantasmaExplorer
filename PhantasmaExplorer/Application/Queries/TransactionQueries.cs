using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;
using Phantasma.IO;
using Phantasma.Numerics;
using EventKind = Phantasma.Explorer.Domain.Entities.EventKind;

namespace Phantasma.Explorer.Application.Queries
{
    public class TransactionQueries
    {
        private readonly ExplorerDbContext _context;

        public TransactionQueries(ExplorerDbContext context)
        {
            _context = context;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public ICollection<Transaction> QueryTransactions(string chain = null, int amount = 20)
        {
            if (string.IsNullOrEmpty(chain)) //no specific chain
            {
                return _context.Transactions
                    .OrderByDescending(p => p.Timestamp)
                    .Include(p => p.Block)
                    .Take(amount)
                    .ToList();
            }

            return _context.Transactions
                .Include(p => p.Block)
                .Where(p => p.Block.Chain.Address.Equals(chain) || p.Block.Chain.Name.Equals(chain))
                .OrderByDescending(p => p.Timestamp)
                .Take(amount)
                .ToList();
        }

        public ICollection<Transaction> QueryAddressTransactions(string address, int amount = 20)
        {
            return _context.Accounts.SingleOrDefault(p => p.Address.Equals(address))?
                .AccountTransactions
                .Select(c => c.Transaction)
                .ToList();
        }

        public Transaction QueryTransaction(string hash)
        {
            return _context.Transactions
                .Include(p => p.Block)
                .Include(p => p.Block.Chain)
                .SingleOrDefault(p => p.Hash.Equals(hash));
        }

        public int QueryAddressTransactionCount(string address, string chain = null)
        {
            var account = _context.Accounts
                .Include(p => p.AccountTransactions)
                .ThenInclude(p => p.Transaction)
                .ThenInclude(p => p.Block)
                .ThenInclude(p => p.Chain)
                .SingleOrDefault(p => p.Address.Equals(address));

            if (account == null) return 0;

            if (string.IsNullOrEmpty(chain))
            {
                return account.AccountTransactions.Count;
            }

            return account.AccountTransactions
                .Count(p => p.Transaction.Block.Chain.Address.Equals(chain)
                            || p.Transaction.Block.Chain.Name.Equals(chain));
        }

        public int QueryTotalChainTransactionCount(string chain = null)
        {
            if (string.IsNullOrEmpty(chain)) //total of all chains
            {
                return _context.Transactions.Count();
            }

            var contextChain = _context.Chains.SingleOrDefault(p => p.Address.Equals(chain) || p.Name.Equals(chain));

            if (contextChain == null) return 0;

            return contextChain.Blocks.Select(p => p.Transactions.Count).Sum();
        }

        public ICollection<Transaction> QueryLastTokenTransactions(string tokenSymbol, int amount = 20)
        {
            var txList = new List<Transaction>();

            var eventList = _context.Transactions
                .OrderByDescending(p => p.Timestamp)
                .Include(p => p.Block)
                .Include(p => p.Block.Chain)
                .ToList();

            foreach (var tx in eventList) //todo move this to share
            {
                foreach (var txEvent in tx.Events)
                {
                    var symbol = Serialization.Unserialize<string>(txEvent.Data.Decode()); //todo remove serialization dependency
                    if (symbol.Equals(tokenSymbol))
                    {
                        txList.Add(tx);
                        break;
                    }
                }
            }

            return txList.Take(amount).ToList();
        }

        public string GetEventContent(Block block, Domain.ValueObjects.Event evt) //todo remove Native event dependency and move this
        {
            Event nativeEvent;
            if (evt.Data != null)
            {
                nativeEvent = new Event((Blockchain.Contracts.EventKind)evt.EventKind,
                    Address.FromText(evt.EventAddress), evt.Data.Decode());
            }
            else
            {
                nativeEvent =
                    new Event((Blockchain.Contracts.EventKind)evt.EventKind, Address.FromText(evt.EventAddress));
            }

            string PlatformName = "Phantasma";
            int NativeTokenDecimals = 8;

            switch (evt.EventKind)
            {
                case EventKind.ChainCreate:
                    {
                        var tokenData = nativeEvent.GetContent<Address>();
                        var chainName = block.Chain.Name;
                        return $"{chainName} chain created at address <a href=\"/chain/{tokenData.ToString()}\">{tokenData.ToString()}</a>.";
                    }
                case EventKind.TokenCreate:
                    {
                        var symbol = nativeEvent.GetContent<string>();
                        var token = _context.Tokens.Single(t => t.Symbol.Equals(symbol));
                        return $"{token.Name} token created with symbol <a href=\"/token/{symbol}\">{symbol}</a>.";
                    }
                case EventKind.GasEscrow:
                    {
                        var gasEvent = nativeEvent.GetContent<GasEventData>();
                        var amount = TokenUtils.ToDecimal(gasEvent.amount, NativeTokenDecimals);
                        var price = TokenUtils.ToDecimal(gasEvent.price, NativeTokenDecimals);
                        return $"{amount} {PlatformName} tokens escrowed for contract gas, with price of {price} per gas unit";
                    }
                case EventKind.GasPayment:
                    {
                        var gasEvent = nativeEvent.GetContent<GasEventData>();
                        var amount = TokenUtils.ToDecimal(gasEvent.amount, NativeTokenDecimals);
                        var price = TokenUtils.ToDecimal(gasEvent.price, NativeTokenDecimals);
                        return $"{amount} {PlatformName} tokens paid for contract gas, with price of {price} per gas unit";

                    }
                case EventKind.TokenMint:
                case EventKind.TokenBurn:
                case EventKind.TokenSend:
                case EventKind.TokenEscrow:
                case EventKind.TokenReceive:
                    {
                        var data = Serialization.Unserialize<TokenEventData>(nativeEvent.Data);
                        var token = _context.Tokens.Single(t => t.Symbol.Equals(data.symbol));
                        string action;

                        switch (evt.EventKind)
                        {
                            case EventKind.TokenMint: action = "minted"; break;
                            case EventKind.TokenBurn: action = "burned"; break;
                            case EventKind.TokenSend: action = "sent"; break;
                            case EventKind.TokenReceive: action = "received"; break;
                            case EventKind.TokenEscrow: action = "escrowed"; break;

                            default: action = "???"; break;
                        }

                        string chainText;

                        if (data.chainAddress.ToString() != block.ChainAddress)
                        {
                            Address srcAddress, dstAddress;

                            if (evt.EventKind == EventKind.TokenReceive)
                            {
                                srcAddress = data.chainAddress;
                                dstAddress = Address.FromText(block.ChainAddress);
                            }
                            else
                            {
                                srcAddress = Address.FromText(block.ChainAddress);
                                dstAddress = data.chainAddress;
                            }

                            chainText = $"from <a href=\"/chain/{srcAddress}\">{GetChainName(srcAddress.ToString())} chain</a> to <a href=\"/chain/{dstAddress}\">{GetChainName(dstAddress.ToString())} chain";
                        }
                        else
                        {
                            chainText = $"in <a href=\"/chain/{data.chainAddress}\">{GetChainName(data.chainAddress.ToString())} chain";
                        }

                        string fromAt = action == "sent" ? "from" : "at";
                        return $"{TokenUtils.ToDecimal(data.value, (int)token.Decimals)} {token.Name} tokens {action} {fromAt} </a> address <a href=\"/address/{nativeEvent.Address}\">{nativeEvent.Address}</a> {chainText}.";
                    }

                default: return "Nothing.";
            }
        }

        private string GetChainName(string address)
        {
            return _context.Chains.Single(p => p.Address.Equals(address)).Name;
        }
    }
}
