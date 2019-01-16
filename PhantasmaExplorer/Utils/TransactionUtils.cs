using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.ViewModels;
using Phantasma.Numerics;
using EventKind = Phantasma.Explorer.Domain.Entities.EventKind;
using Token = Phantasma.Explorer.Domain.Entities.Token;

namespace Phantasma.Explorer.Utils
{
    public static class TransactionUtils
    {
        public static string GetTxDescription(Transaction tx,
            List<Chain> phantasmaChains, List<Token> phantasmaTokens, TransactionViewModel vm = null)
        //todo revisit
        {
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
                                phantasmaTokens.SingleOrDefault(p => p.Symbol == data.symbol).Decimals);
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
                        phantasmaTokens.SingleOrDefault(p => p.Symbol == senderToken).Decimals);

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
                        phantasmaTokens.SingleOrDefault(p => p.Symbol == receiverToken).Decimals);

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

        private static string GetChainName(string address, List<Chain> phantasmaChains)
        {
            foreach (var element in phantasmaChains)
            {
                if (element.Address == address) return element.Name;
            }

            return string.Empty;
        }
    }
}
