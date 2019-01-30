using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Domain.ValueObjects;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;
using Phantasma.IO;
using Phantasma.Numerics;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.Utils
{
    public static class TransactionUtils
    {
        public static string GetTxDescription(Transaction tx, TransactionViewModel vm = null)
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();
            var phantasmaChains = context.Chains;
            var phantasmaTokens = context.Tokens;
            string description = null;

            string senderToken = null;
            Address senderChain = Address.FromText(tx.Block.ChainAddress);
            Address senderAddress = Address.Null;

            string receiverToken = null;
            Address receiverChain = Address.Null;
            Address receiverAddress = Address.Null;

            BigInteger amount = 0;

            foreach (var evt in tx.Events)
            {
                switch (evt.EventKind)
                {
                    case EventKind.TokenSend:
                        {
                            var data = Serialization.Unserialize<TokenEventData>(evt.Data.Decode());
                            amount = data.value;
                            senderAddress = Address.FromText(evt.EventAddress);
                            senderToken = data.symbol;
                        }
                        break;

                    case EventKind.TokenReceive:
                        {
                            var data = Serialization.Unserialize<TokenEventData>(evt.Data.Decode());
                            amount = data.value;
                            receiverAddress = Address.FromText(evt.EventAddress);
                            receiverChain = data.chainAddress;
                            receiverToken = data.symbol;
                        }
                        break;

                    case EventKind.TokenEscrow:
                        {
                            var data = Serialization.Unserialize<TokenEventData>(evt.Data.Decode());
                            amount = data.value;
                            var amountDecimal = TokenUtils.ToDecimal(amount, (int)
                                phantasmaTokens.Single(p => p.Symbol == data.symbol).Decimals);
                            receiverAddress = Address.FromText(evt.EventAddress);
                            receiverChain = data.chainAddress;
                            var chain = GetChainName(receiverChain.Text, phantasmaChains);
                            description =
                                $"{amountDecimal} {data.symbol} tokens escrowed for address {receiverAddress} in {chain}";
                        }
                        break;
                    case EventKind.AddressRegister:
                        {
                            var name = Serialization.Unserialize<string>(evt.Data.Decode());
                            description = $"{evt.EventAddress} registered the name '{name}'";
                        }
                        break;

                    case EventKind.FriendAdd:
                        {
                            var address = Serialization.Unserialize<Address>(evt.Data.Decode());
                            description = $"{evt.EventAddress} added '{address.ToString()} to friends.'";
                        }
                        break;

                    case EventKind.FriendRemove:
                        {
                            var address = Serialization.Unserialize<Address>(evt.Data.Decode());
                            description = $"{evt.EventAddress} removed '{address.ToString()} from friends.'";
                        }
                        break;
                }
            }

            if (description == null)
            {
                if (amount > 0 && senderAddress != Address.Null && receiverAddress != Address.Null &&
                    senderToken != null && senderToken == receiverToken)
                {
                    var amountDecimal = TokenUtils.ToDecimal(amount, (int)
                        phantasmaTokens.Single(p => p.Symbol == senderToken).Decimals);

                    if (vm != null)
                    {
                        vm.AmountTransfer = amountDecimal;
                        vm.TokenSymbol = senderToken;
                        vm.SenderAddress = senderAddress.ToString();
                        vm.ReceiverAddress = receiverAddress.ToString();
                    }

                    description =
                        $"{amountDecimal} {senderToken} sent from {senderAddress.ToString()} to {receiverAddress.ToString()}";
                }
                else if (amount > 0 && receiverAddress != Address.Null && receiverToken != null)
                {
                    var amountDecimal = TokenUtils.ToDecimal(amount, (int)
                        phantasmaTokens.Single(p => p.Symbol == receiverToken).Decimals);

                    if (vm != null)
                    {
                        vm.AmountTransfer = amountDecimal;
                        vm.TokenSymbol = receiverToken;
                        vm.ReceiverAddress = receiverAddress.ToString();
                    }

                    description = $"{amountDecimal} {receiverToken} received on {receiverAddress.Text} ";
                }
                else
                {
                    description = "Custom transaction";
                }

                if (receiverChain != Address.Null && receiverChain != senderChain)
                {
                    description +=
                        $" from {GetChainName(senderChain.Text, phantasmaChains)} chain to {GetChainName(receiverChain.Text, phantasmaChains)} chain";
                }
            }
            return description;
        }

        public static string GetEventContent(Block block, Event evt) //todo remove Native event dependency and move this
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();
            var phantasmaChains = context.Chains;
            var phantasmaTokens = context.Tokens;

            string PlatformName = "Phantasma";
            int NativeTokenDecimals = 8;

            switch (evt.EventKind)
            {
                case EventKind.ChainCreate:
                    {
                        var tokenData = Serialization.Unserialize<Address>(evt.Data.Decode());
                        var chainName = block.Chain.Name;
                        return $"{chainName} chain created at address <a href=\"/chain/{tokenData.ToString()}\">{tokenData.ToString()}</a>.";
                    }
                case EventKind.TokenCreate:
                    {
                        var symbol = Serialization.Unserialize<string>(evt.Data.Decode());
                        var token = phantasmaTokens.Single(t => t.Symbol.Equals(symbol));
                        return $"{token.Name} token created with symbol <a href=\"/token/{symbol}\">{symbol}</a>.";
                    }
                case EventKind.GasEscrow:
                case EventKind.GasPayment:
                    {
                        var gasEvent = Serialization.Unserialize<GasEventData>(evt.Data.Decode());
                        var amount = TokenUtils.ToDecimal(gasEvent.amount, NativeTokenDecimals);
                        var price = TokenUtils.ToDecimal(gasEvent.price, NativeTokenDecimals);

                        if (evt.EventKind == EventKind.GasEscrow)
                        {
                            return $"{amount} {PlatformName} tokens escrowed for contract gas, with price of {price} per gas unit";
                        }

                        return $"{amount} {PlatformName} tokens paid for contract gas, with price of {price} per gas unit";
                    }
                case EventKind.AddressRegister:
                    {
                        var name = Serialization.Unserialize<string>(evt.Data.Decode());
                        return $"{evt.EventAddress} registered account name: {name}";
                    }
                case EventKind.TokenMint:
                case EventKind.TokenBurn:
                case EventKind.TokenSend:
                case EventKind.TokenEscrow:
                case EventKind.TokenReceive:
                    {
                        var data = Serialization.Unserialize<TokenEventData>(evt.Data.Decode());
                        var token = phantasmaTokens.Single(t => t.Symbol.Equals(data.symbol));
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

                            chainText = $"from <a href=\"/chain/{srcAddress}\">{GetChainName(srcAddress.ToString(), phantasmaChains)} chain</a> to <a href=\"/chain/{dstAddress}\">{GetChainName(dstAddress.ToString(), phantasmaChains)} chain";
                        }
                        else
                        {
                            chainText = $"in <a href=\"/chain/{data.chainAddress}\">{GetChainName(data.chainAddress.ToString(), phantasmaChains)} chain";
                        }

                        string fromAt = action == "sent" ? "from" : "at";
                        return $"{TokenUtils.ToDecimal(data.value, (int)token.Decimals)} {token.Name} tokens {action} {fromAt} </a> address <a href=\"/address/{evt.EventAddress}\">{evt.EventAddress}</a> {chainText}.";
                    }

                default: return "Nothing.";
            }
        }

        public static bool IsTransferEvent(Event txEvent)//todo confirm this
        {
            return txEvent.EventKind == EventKind.TokenSend || txEvent.EventKind == EventKind.TokenReceive;
        }

        private static string GetChainName(string address, IEnumerable<Chain> phantasmaChains)
        {
            return phantasmaChains.Single(p => p.Address.Equals(address)).Name;
        }

        public static string GetTokenSymbolFromTokenEventData(Event txEvent)
        {
            return Serialization.Unserialize<TokenEventData>(txEvent.Data.Decode()).symbol;
        }
    }
}
