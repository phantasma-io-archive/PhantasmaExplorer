using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Cryptography;
using Phantasma.Domain;
using Phantasma.Explorer.Application;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Domain.ValueObjects;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;
using Phantasma.Numerics;
using Phantasma.RpcClient.DTOs;
using Phantasma.Storage;
using EventKind = Phantasma.RpcClient.DTOs.EventKind;

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
            string senderChain = tx.Block.ChainAddress;
            Address senderAddress = Address.Null;

            string receiverToken = null;
            string receiverChain = "";
            Address receiverAddress = Address.Null;

            BigInteger amount = 0;

            foreach (var evt in tx.Events)
            {
                switch (evt.EventKind)
                {
                    case EventKind.TokenSend:
                        {
                            var data = Serialization.Unserialize<TokenEventData>(evt.Data.Decode());
                            amount = data.Value;
                            senderAddress = Address.FromText(evt.EventAddress);
                            senderToken = data.Symbol;
                        }
                        break;

                    case EventKind.TokenReceive:
                        {
                            var data = Serialization.Unserialize<TokenEventData>(evt.Data.Decode());
                            amount = data.Value;
                            receiverAddress = Address.FromText(evt.EventAddress);
                            receiverChain = data.ChainName;
                            receiverToken = data.Symbol;
                        }
                        break;

                    //case EventKind.TokenEscrow:
                    //    {
                    //        var data = Serialization.Unserialize<TokenEventData>(evt.Data.Decode());
                    //        amount = data.Value;
                    //        var amountDecimal = UnitConversion.ToDecimal(amount, (int)
                    //            phantasmaTokens.Single(p => p.Symbol == data.Symbol).Decimals);
                    //        receiverAddress = Address.FromText(evt.EventAddress);
                    //        receiverChain = data.ChainName;
                    //        description =
                    //            $"{amountDecimal} {data.Symbol} tokens escrowed for address {receiverAddress} in {receiverChain}";
                    //    }
                    //    break;
                    case EventKind.AddressRegister:
                        {
                            var name = Serialization.Unserialize<string>(evt.Data.Decode());
                            description = $"{evt.EventAddress} registered the name '{name}'";
                        }
                        break;

                    case EventKind.AddressLink:
                        {
                            var address = Serialization.Unserialize<Address>(evt.Data.Decode());
                            description = $"{evt.EventAddress} linked an address '{address.ToString()}.'";
                        }
                        break;

                    case EventKind.AddressUnlink:
                        {
                            var address = Serialization.Unserialize<Address>(evt.Data.Decode());
                            description = $"{evt.EventAddress} unlinked an address '{address.ToString()}.'";
                        }
                        break;
                }
            }

            if (description == null)
            {
                if (amount > 0 && senderAddress != Address.Null && receiverAddress != Address.Null &&
                    senderToken != null && senderToken == receiverToken)
                {
                    var amountDecimal = UnitConversion.ToDecimal(amount, (int)
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
                    var amountDecimal = UnitConversion.ToDecimal(amount, (int)
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

                if (!string.IsNullOrEmpty(receiverChain) && receiverChain != senderChain)
                {
                    description +=
                        $" from {senderChain} chain to {receiverChain} chain";
                }
            }
            return description;
        }

        public static string GetEventContent(Block block, Domain.ValueObjects.Event evt) //todo remove Native event dependency and move this
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();
            var phantasmaChains = context.Chains;
            var phantasmaTokens = context.Tokens;

            string PlatformName = "Phantasma";
            int nativeTokenDecimals = (int)
                context.Tokens.Single(p => p.Symbol.Equals(AppSettings.NativeSymbol)).Decimals;

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
                        var amount = UnitConversion.ToDecimal(gasEvent.amount, nativeTokenDecimals);
                        var price = UnitConversion.ToDecimal(gasEvent.price, nativeTokenDecimals);

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
                //case EventKind.TokenEscrow:
                case EventKind.TokenStake:
                case EventKind.TokenUnstake:
                case EventKind.TokenReceive:
                case EventKind.TokenClaim:
                    {
                        var data = Serialization.Unserialize<TokenEventData>(evt.Data.Decode());
                        var token = phantasmaTokens.Single(t => t.Symbol.Equals(data.Symbol));
                        string action;

                        switch (evt.EventKind)
                        {
                            case EventKind.TokenMint: action = "minted"; break;
                            case EventKind.TokenBurn: action = "burned"; break;
                            case EventKind.TokenSend: action = "sent"; break;
                            case EventKind.TokenReceive: action = "received"; break;
                            case EventKind.TokenStake: action = "staked"; break;
                            case EventKind.TokenUnstake: action = "unstaked"; break;
                           // case EventKind.TokenEscrow: action = "escrowed"; break;
                            case EventKind.TokenClaim: action = "claimed"; break;

                            default: action = "???"; break;
                        }

                        string chainText;

                        if (data.ChainName != block.ChainName)
                        {
                            string srcName, dstName;

                            if (evt.EventKind == EventKind.TokenReceive)
                            {
                                srcName = data.ChainName;
                                dstName = block.ChainName;
                            }
                            else
                            {
                                srcName = block.ChainName;
                                dstName = data.ChainName;
                            }

                            chainText = $"from <a href=\"/chain/{srcName}\">{srcName} chain</a> to <a href=\"/chain/{dstName}\">{dstName} chain";
                        }
                        else
                        {
                            chainText = $"in <a href=\"/chain/{data.ChainName}\">{data.ChainName} chain";
                        }

                        string fromAt = action == "sent" ? "from" : "at";
                        return $"{UnitConversion.ToDecimal(data.Value, (int)token.Decimals)} {token.Name} tokens {action} {fromAt} </a> address <a href=\"/address/{evt.EventAddress}\">{evt.EventAddress}</a> {chainText}.";
                    }

                default: return "Nothing.";
            }
        }

        private static string GetChainName(string address, IEnumerable<Chain> phantasmaChains)
        {
            var name = phantasmaChains.SingleOrDefault(p => p.Address.Equals(address))?.Name;
            return name;
        }
    }
}
