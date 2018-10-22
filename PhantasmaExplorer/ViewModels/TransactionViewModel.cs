using System;
using System.Collections.Generic;
using Phantasma.Blockchain;
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
        public DateTime Timestamp { get; set; }

        public static TransactionViewModel FromTransaction(BlockViewModel block, Transaction tx,
            List<EventViewModel> evts)
        {
            var disasm = new Disassembler(tx.Script); //Todo fix me

            return new TransactionViewModel
            {
                Block = block,
                ChainAddress = block.ChainAddress,
                ChainName = block.ChainName,
                Date = block.Timestamp,
                Hash = tx.Hash.ToString(),
                FromAddress = "????",
                FromName = "Anonymous",
                Timestamp = block.Timestamp,
                Events = evts,
                Instructions = disasm.GetInstructions()
            };
        }
    }
}