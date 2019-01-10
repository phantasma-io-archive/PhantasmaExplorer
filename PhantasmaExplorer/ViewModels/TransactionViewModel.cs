using System;
using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Numerics;
using Phantasma.RpcClient.DTOs;
using Phantasma.VM;

namespace Phantasma.Explorer.ViewModels
{
    public class TransactionViewModel
    {
        public string Hash { get; set; }
        public BlockViewModel Block { get; set; }
        public DateTime Date { get; set; }
        public string ChainName { get; set; }
        public string ChainAddress { get; set; }
        public IEnumerable<EventViewModel> Events { get; set; }
        public IEnumerable<Instruction> Instructions { get; set; }
        public string Description { get; set; }

        // in case of transfer
        public decimal AmountTransfer { get; set; }
        public string TokenSymbol { get; set; }
        public string SenderAddress { get; set; }
        public string ReceiverAddress { get; set; }

        public static TransactionViewModel FromTransaction(IRepository repository, BlockViewModel block, TransactionDto tx)
        {
            var vm = new TransactionViewModel();
            var disasm = new Disassembler(tx.Script.Decode()); //Todo fix me

            string description = GetTxDescription(vm, tx, repository.GetAllChainsInfo()?.ToList(), repository.GetTokens()?.ToList());

            vm.Block = block;
            vm.ChainAddress = block.ChainAddress;
            vm.ChainName = block.ChainName;
            vm.Date = block.Timestamp;
            vm.Hash = tx.Txid;
            vm.Events = tx.Events.Select(evt => EventViewModel.FromEvent(repository, tx, evt));
            vm.Description = description;
            vm.Instructions = disasm.Instructions;

            return vm;
        }

        //todo revisit
        public static string GetTxDescription(TransactionViewModel vm, TransactionDto tx, List<ChainDto> phantasmaChains, List<TokenDto> phantasmaTokens)
        {
            string description = null;

            string senderToken = null;
            Address senderChain = Address.FromText(tx.ChainAddress);
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
                    nativeEvent = new Event((EventKind)evt.EvtKind,
                        Address.FromText(evt.EventAddress), evt.Data.Decode());
                }
                else
                {
                    nativeEvent =
                        new Event((EventKind)evt.EvtKind, Address.FromText(evt.EventAddress));
                }

                switch (evt.EvtKind)
                {
                    case EvtKind.TokenSend:
                        {
                            var data = nativeEvent.GetContent<TokenEventData>();
                            amount = data.value;
                            senderAddress = nativeEvent.Address;
                            senderToken = data.symbol;
                        }
                        break;

                    case EvtKind.TokenReceive:
                        {
                            var data = nativeEvent.GetContent<TokenEventData>();
                            amount = data.value;
                            receiverAddress = nativeEvent.Address;
                            receiverChain = data.chainAddress;
                            receiverToken = data.symbol;
                        }
                        break;

                    case EvtKind.TokenEscrow:
                        {
                            var data = nativeEvent.GetContent<TokenEventData>();
                            amount = data.value;
                            var amountDecimal = TokenUtils.ToDecimal(amount,
                                phantasmaTokens.SingleOrDefault(p => p.Symbol == data.symbol).Decimals);
                            receiverAddress = nativeEvent.Address;
                            receiverChain = data.chainAddress;
                            var chain = GetChainName(receiverChain.Text, phantasmaChains);
                            description =
                                $"{amountDecimal} {data.symbol} tokens escrowed for address {receiverAddress} in {chain}";
                        }
                        break;
                    case EvtKind.AddressRegister:
                        {
                            var name = nativeEvent.GetContent<string>();
                            description = $"{nativeEvent.Address} registered the name '{name}'";
                        }
                        break;

                    case EvtKind.FriendAdd:
                        {
                            var address = nativeEvent.GetContent<Address>();
                            description = $"{nativeEvent.Address} added '{address} to friends.'";
                        }
                        break;

                    case EvtKind.FriendRemove:
                        {
                            var address = nativeEvent.GetContent<Address>();
                            description = $"{nativeEvent.Address} removed '{address} from friends.'";
                        }
                        break;
                }
            }

            if (description == null)
            {
                if (amount > 0 && senderAddress != Address.Null && receiverAddress != Address.Null &&
                    senderToken != null && senderToken == receiverToken)
                {
                    var amountDecimal = TokenUtils.ToDecimal(amount,
                        phantasmaTokens.SingleOrDefault(p => p.Symbol == senderToken).Decimals);
                    vm.AmountTransfer = amountDecimal;
                    vm.TokenSymbol = senderToken;
                    vm.SenderAddress = senderAddress.ToString();
                    vm.ReceiverAddress = receiverAddress.ToString();
                    description =
                        $"{amountDecimal} {senderToken} sent from {senderAddress.Text} to {receiverAddress.Text}";
                }
                else if (amount > 0 && receiverAddress != Address.Null && receiverToken != null)
                {
                    var amountDecimal = TokenUtils.ToDecimal(amount,
                        phantasmaTokens.SingleOrDefault(p => p.Symbol == receiverToken).Decimals);
                    vm.AmountTransfer = amountDecimal;
                    vm.TokenSymbol = receiverToken;
                    vm.ReceiverAddress = receiverAddress.ToString();
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

        private static string GetChainName(string address, List<ChainDto> phantasmaChains)
        {
            foreach (var element in phantasmaChains)
            {
                if (element.Address == address) return element.Name;
            }

            return string.Empty;
        }
    }
}