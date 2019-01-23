using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;
using Phantasma.IO;
using Phantasma.Numerics;
using EventKind = Phantasma.Explorer.Domain.Entities.EventKind;

namespace Phantasma.Explorer.Utils
{
    public static class TransactionUtils
    {
        public static string GetTxDescription(Transaction tx, TransactionViewModel vm = null)
        //todo revisit
        {
            var context = (ExplorerDbContext)Explorer.AppServices.GetService(typeof(ExplorerDbContext)); //Todo fix me
            var phantasmaChains = context.Chains.ToList();
            var phantasmaTokens = context.Tokens.ToList();
            string description = null;

            string senderToken = null;
            Address senderChain = Address.FromText(tx.Block.ChainAddress);
            Address senderAddress = Address.Null;

            string receiverToken = null;
            Address receiverChain = Address.Null;
            Address receiverAddress = Address.Null;

            BigInteger amount = 0;

            foreach (var evt in tx.Events) //todo move this
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

                switch (evt.EventKind)
                {
                    case EventKind.TokenSend:
                        {
                            var data = nativeEvent.GetContent<TokenEventData>();
                            amount = data.value;
                            senderAddress = nativeEvent.Address;
                            senderToken = data.symbol;
                        }
                        break;

                    case EventKind.TokenReceive:
                        {
                            var data = nativeEvent.GetContent<TokenEventData>();
                            amount = data.value;
                            receiverAddress = nativeEvent.Address;
                            receiverChain = data.chainAddress;
                            receiverToken = data.symbol;
                        }
                        break;

                    case EventKind.TokenEscrow:
                        {
                            var data = nativeEvent.GetContent<TokenEventData>();
                            amount = data.value;
                            var amountDecimal = TokenUtils.ToDecimal(amount, (int)
                                phantasmaTokens.Single(p => p.Symbol == data.symbol).Decimals);
                            receiverAddress = nativeEvent.Address;
                            receiverChain = data.chainAddress;
                            var chain = GetChainName(receiverChain.Text, phantasmaChains);
                            description =
                                $"{amountDecimal} {data.symbol} tokens escrowed for address {receiverAddress} in {chain}";
                        }
                        break;
                    case EventKind.AddressRegister:
                        {
                            var name = nativeEvent.GetContent<string>();
                            description = $"{nativeEvent.Address.ToString()} registered the name '{name}'";
                        }
                        break;

                    case EventKind.FriendAdd:
                        {
                            var address = nativeEvent.GetContent<Address>();
                            description = $"{nativeEvent.Address} added '{address.ToString()} to friends.'";
                        }
                        break;

                    case EventKind.FriendRemove:
                        {
                            var address = nativeEvent.GetContent<Address>();
                            description = $"{nativeEvent.Address} removed '{address.ToString()} from friends.'";
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
                        $"{amountDecimal} {senderToken} sent from {senderAddress.Text} to {receiverAddress.Text}";
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

        public static string GetEventContent(Block block, Domain.ValueObjects.Event evt) //todo remove Native event dependency and move this
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();
            var phantasmaChains = context.Chains.ToList();
            var phantasmaTokens = context.Tokens.ToList();

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
                        var token = phantasmaTokens.Single(t => t.Symbol.Equals(symbol));
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
                case EventKind.AddressRegister:
                    {
                        var name = nativeEvent.GetContent<string>();
                        return $"{nativeEvent.Address} registered account name: {name}";
                    }
                case EventKind.TokenMint:
                case EventKind.TokenBurn:
                case EventKind.TokenSend:
                case EventKind.TokenEscrow:
                case EventKind.TokenReceive:
                    {
                        var data = Serialization.Unserialize<TokenEventData>(nativeEvent.Data);
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
                        return $"{TokenUtils.ToDecimal(data.value, (int)token.Decimals)} {token.Name} tokens {action} {fromAt} </a> address <a href=\"/address/{nativeEvent.Address}\">{nativeEvent.Address}</a> {chainText}.";
                    }

                default: return "Nothing.";
            }
        }

        private static string GetChainName(string address, List<Chain> phantasmaChains)
        {
            return phantasmaChains.Single(p => p.Address.Equals(address)).Name;
        }
    }
}
