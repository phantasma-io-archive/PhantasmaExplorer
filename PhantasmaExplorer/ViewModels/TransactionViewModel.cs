using System;
using System.Collections.Generic;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
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
        public string FromName { get; set; }
        public string FromAddress { get; set; }
        public IEnumerable<EventViewModel> Events { get; set; }
        public IEnumerable<Instruction> Instructions { get; set; }
        public string Description { get; set; }

        public static TransactionViewModel FromTransaction(BlockViewModel block, Transaction tx,
            List<EventViewModel> evts)
        {
            var disasm = new Disassembler(tx.Script); //Todo fix me

            string description = null;

            Token senderToken = null;
            Address senderChain = Address.Null;
            Address senderAddress = Address.Null;

            Token receiverToken = null;
            Address receiverChain = Address.Null;
            Address receiverAddress = Address.Null;

            BigInteger amount = 0;

            //Nexus nexus = null;

            foreach (var evt in tx.Events)
            {
                switch (evt.Kind) 
                {
                    case EventKind.TokenSend:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            amount = data.amount;
                            senderAddress = evt.Address;
                            senderChain = data.chainAddress;
                            //senderToken = nexus.FindTokenBySymbol(data.symbol);
                        }
                        break;

                    case EventKind.TokenReceive:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            amount = data.amount;
                            receiverAddress = evt.Address;
                            receiverChain = data.chainAddress;
                            //receiverToken = nexus.FindTokenBySymbol(data.symbol);
                        }
                        break;
                }
            }

            if (description == null)
            {
                if (amount > 0 && senderAddress != Address.Null && receiverAddress != Address.Null && senderToken != null && senderToken == receiverToken)
                {
                    description = $"{TokenUtils.ToDecimal(amount)} {senderToken.Symbol} sent from {senderAddress.Text} to {receiverAddress.Text}";
                }
                else
                {
                    description = "Custom transaction";
                }
            }

            return new TransactionViewModel
            {
                Block = block,
                ChainAddress = block.ChainAddress,
                ChainName = block.ChainName,
                Date = block.Timestamp,
                Hash = tx.Hash.ToString(),
                FromAddress = "????",
                FromName = "Anonymous",
                Events = evts,
                Description = description,
                Instructions = disasm.GetInstructions()
            };
        }
    }
}