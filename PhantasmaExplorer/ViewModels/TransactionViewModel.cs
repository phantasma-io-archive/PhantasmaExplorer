using System;
using System.Collections.Generic;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Cryptography;
using Phantasma.IO;
using Phantasma.VM;

namespace Phantasma.Explorer.ViewModels
{
    public class TransactionViewModel
    {
        public string Hash { get; set; }
        public Block Block;
        public DateTime Date { get; set; }
        public string ChainName { get; set; }
        public string ChainAddress { get; set; }
        public string FromName { get; set; }
        public string FromAddress { get; set; }
        public IEnumerable<EventViewModel> Events { get; set; }
        public IEnumerable<Instruction> Instructions;

        private static string GetChainName(Nexus nexus, Address chainAddress)
        {
            var chain = nexus.FindChainByAddress(chainAddress);
            if (chain != null)
            {
                return chain.Name;
            }

            return "???";
        }

        // TODO exception and error handling
        private static string GetEventContent(Nexus nexus, Block block, Event evt)
        {
            switch (evt.Kind)
            {
                case EventKind.ChainCreate:
                    {
                        var chainAddress = Serialization.Unserialize<Address>(evt.Data);
                        var chain = nexus.FindChainByAddress(chainAddress);
                        return $"{chain.Name} chain created at address <a href=\"/chain/{chainAddress}\">{chainAddress}</a>.";
                    }

                case EventKind.TokenCreate:
                    {
                        var symbol = Serialization.Unserialize<string>(evt.Data);
                        var token = nexus.FindTokenBySymbol(symbol);
                        return $"{token.Name} token created with symbol <a href=\"/token/{symbol}\">{symbol}</a>.";
                    }

                case EventKind.TokenMint:
                case EventKind.TokenBurn:
                case EventKind.TokenSend:
                case EventKind.TokenReceive:
                    {
                        var data = Serialization.Unserialize<TokenEventData>(evt.Data);
                        var token = nexus.FindTokenBySymbol(data.symbol);
                        string action;

                        switch (evt.Kind)
                        {
                            case EventKind.TokenMint: action = "minted"; break;
                            case EventKind.TokenBurn: action = "burned"; break;
                            case EventKind.TokenSend: action = "sent"; break;
                            case EventKind.TokenReceive: action = "received"; break;
                            default: action = "???"; break;
                        }

                        string chainText;

                        if (data.chainAddress != block.Chain.Address)
                        {
                            Address srcAddress, dstAddress;

                            if (evt.Kind == EventKind.TokenReceive)
                            {
                                srcAddress = data.chainAddress;
                                dstAddress = block.Chain.Address;
                            }
                            else
                            {
                                srcAddress = block.Chain.Address;
                                dstAddress = data.chainAddress;
                            }

                            chainText = $"from <a href=\"/chain/{srcAddress}\">{GetChainName(nexus, srcAddress)} chain</a> to <a href=\"/chain/{dstAddress}\">{GetChainName(nexus, dstAddress)} chain";
                        }
                        else
                        {
                            chainText = $"in <a href=\"/chain/{data.chainAddress}\">{GetChainName(nexus, data.chainAddress)} chain";
                        }

                        return $"{TokenUtils.ToDecimal(data.amount)} {token.Name} tokens {action} at </a> address <a href=\"/address/{evt.Address}\">{evt.Address}</a> {chainText}.";
                    }

                default: return "Nothing.";
            }
        }

        public static TransactionViewModel FromTransaction(Nexus nexus, Block block, Transaction tx)
        {
            var evts = new List<EventViewModel>();
            foreach (var evt in tx.Events)
            {
                evts.Add(new EventViewModel()
                {
                    Kind = evt.Kind,
                    Content = GetEventContent(nexus, block, evt),
                });
            }

            var disasm = new Disassembler(tx.Script);

            return new TransactionViewModel()
            {
                Block = block,
                ChainAddress = block.Chain.Address.Text,
                ChainName = block.Chain.Name,
                Date = block.Timestamp,
                Hash = tx.Hash.ToString(),
                FromAddress = "????",
                FromName = "Anonymous",
                Events = evts,
                Instructions = disasm.GetInstructions(),
            };
        }
    }

    public class EventViewModel
    {
        public EventKind Kind { get; set; }
        public string Content { get; set; }
    }
}
