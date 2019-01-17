using System;
using System.Collections.Generic;
using System.Linq;
using Phantasma.Core.Types;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.Utils;
using Phantasma.Numerics;
using Phantasma.VM;

namespace Phantasma.Explorer.ViewModels
{
    public class TransactionViewModel
    {
        public string Hash { get; set; }
        public DateTime Date { get; set; }
        public string ChainName { get; set; }
        public string ChainAddress { get; set; }
        public string Description { get; set; }
        public string BlockHash { get; set; }
        public uint BlockHeight { get; set; }
        public string Result { get; set; }

        public IEnumerable<EventViewModel> Events { get; set; }
        public IEnumerable<Instruction> Instructions { get; set; }

        // in case of transfer
        public decimal AmountTransfer { get; set; }
        public string TokenSymbol { get; set; }
        public string SenderAddress { get; set; }
        public string ReceiverAddress { get; set; }

        public static TransactionViewModel FromTransaction(Transaction tx)  //todo fix vm .From
        {
            var vm = new TransactionViewModel();

            var disasm = new Disassembler(tx.Script.Decode()); //Todo fix me
            var context = (ExplorerDbContext)Explorer.AppServices.GetService(typeof(ExplorerDbContext)); //Todo fix me

            string description = TransactionUtils.GetTxDescription(tx, context.Chains.ToList(), context.Tokens.ToList(), vm);


            vm.ChainAddress = tx.Block.ChainAddress;
            vm.ChainName = tx.Block.ChainName;
            vm.BlockHeight = tx.Block.Height;
            vm.BlockHash = tx.BlockHash;
            vm.Date = new Timestamp(tx.Block.Timestamp);
            vm.Hash = tx.Hash;
            vm.Events = tx.Events.Select(evt => EventViewModel.FromEvent(tx, evt));
            vm.Description = description;
            vm.Instructions = disasm.Instructions;
            vm.Result = tx.Result;
            return vm;
        }
    }
}