using System;
using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Numerics;
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
        public decimal GasLimit { get; set; }
        public decimal GasPrice { get; set; }

        // in case of transfer
        public decimal AmountTransfer { get; set; }
        public string TokenSymbol { get; set; }
        public string SenderAddress { get; set; }
        public string ReceiverAddress { get; set; }

        public static TransactionViewModel FromTransaction(IRepository repository, BlockViewModel block, Transaction tx)
        {
            var nexus = repository.NexusChain;
            var vm = new TransactionViewModel();
            var disasm = new Disassembler(tx.Script); //Todo fix me

            string description = null;

            Token senderToken = null;
            Address senderChain = nexus.FindChainByName(tx.ChainName).Address;
            Address senderAddress = Address.Null;

            Token receiverToken = null;
            Address receiverChain = Address.Null;
            Address receiverAddress = Address.Null;

            BigInteger amount = 0;

            var chainBlock = nexus.FindBlockForHash(tx.Hash);
            var evts = chainBlock.GetEventsForTransaction(tx.Hash);

            foreach (var evt in evts)//todo move this
            {
                switch (evt.Kind)
                {
                    case EventKind.TokenSend:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            amount = data.value;
                            senderAddress = evt.Address;
                            senderChain = data.chainAddress;
                            senderToken = nexus.FindTokenBySymbol(data.symbol);
                        }
                        break;

                    case EventKind.TokenReceive:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            amount = data.value;
                            receiverAddress = evt.Address;
                            receiverChain = data.chainAddress;
                            receiverToken = nexus.FindTokenBySymbol(data.symbol);
                        }
                        break;

                    case EventKind.TokenEscrow:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            amount = data.value;
                            var token = nexus.FindTokenBySymbol(data.symbol);
                            var amountDecimal = TokenUtils.ToDecimal(amount, token.Decimals);
                            receiverAddress = evt.Address;
                            receiverChain = data.chainAddress;
                            var chain = nexus.FindChainByAddress(receiverChain).Name;
                            description = $"{amountDecimal} {token.Name} tokens escrowed for address {receiverAddress} in {chain}";
                        }
                        break;

                    case EventKind.AddressRegister:
                        {
                            var name = evt.GetContent<string>();
                            description = $"{evt.Address} registered the name '{name}'";
                        }
                        break;

                    case EventKind.FriendAdd:
                        {
                            var address = evt.GetContent<Address>();
                            description = $"{evt.Address} added '{address} to friends.'";
                        }
                        break;

                    case EventKind.FriendRemove:
                        {
                            var address = evt.GetContent<Address>();
                            description = $"{evt.Address} removed '{address} from friends.'";
                        }
                        break;
                }
            }

            if (description == null)
            {
                if (amount > 0 && senderAddress != Address.Null && receiverAddress != Address.Null && senderToken != null && senderToken == receiverToken)
                {
                    var amountDecimal = TokenUtils.ToDecimal(amount, senderToken.Decimals);
                    description = $"{amountDecimal} {senderToken} sent from {senderAddress.Text} to {receiverAddress.Text}";
                }
                else if (amount > 0 && receiverAddress != Address.Null && receiverToken != null)
                {
                    var amountDecimal = TokenUtils.ToDecimal(amount, receiverToken.Decimals);
                    description = $"{amountDecimal} {receiverToken} received on {receiverAddress.Text} ";
                }
                else
                {
                    description = "Custom transaction";
                }

                if (receiverChain != Address.Null && receiverChain != senderChain)
                {
                    description += $" from {nexus.FindChainByAddress(senderChain).Name} chain to {nexus.FindChainByAddress(receiverChain).Name} chain";
                }
            }

            vm.Block = block;
            vm.ChainAddress = block.ChainAddress;
            vm.ChainName = block.ChainName;
            vm.Date = block.Timestamp;
            vm.Hash = tx.Hash.ToString();
            vm.Events = evts.Select(evt => EventViewModel.FromEvent(repository, tx, evt));
            vm.Description = description;
            vm.Instructions = disasm.Instructions;
            vm.GasLimit = TokenUtils.ToDecimal(tx.GasLimit, Nexus.NativeTokenDecimals);
            vm.GasPrice = TokenUtils.ToDecimal(tx.GasPrice, Nexus.NativeTokenDecimals);

            return vm;
        }
    }
}