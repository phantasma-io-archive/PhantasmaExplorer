using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Core.Types;
using Phantasma.Cryptography;
using Phantasma.Explorer.Site;
using Phantasma.VM.Utils;

namespace Phantasma.Explorer
{
    public struct MenuContext
    {
        public string text;
        public string url;
        public bool active;
    }

    public class Explorer
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Phantasma Block Explorer....");
            //InitTestTx();

            var ownerKey = KeyPair.FromWIF("L2G1vuxtVRPvC6uZ1ZL8i7Dbqxk9VPXZMGvZu9C3LXpxKK51x41N");
            var nexus = new Nexus(ownerKey);

            var bankChain = nexus.FindChainByName("bank");

            #region TESTING TXs
            // TODO move this to a separate method...            
            var targetAddress = Address.FromText("PGasVpbFYdu7qERihCsR22nTDQp1JwVAjfuJ38T8NtrCB");

            // mainchain transfer
            {
                var transactions = new List<Transaction>();
                var script = ScriptUtils.CallContractScript(nexus.RootChain, "TransferTokens", ownerKey.Address, targetAddress, Nexus.NativeTokenSymbol, TokenUtils.ToBigInteger(5));
                var tx = new Transaction(script, 0, 0);
                tx.Sign(ownerKey);
                transactions.Add(tx);

                var block = new Block(nexus.RootChain, ownerKey.Address, Timestamp.Now, transactions, nexus.RootChain.lastBlock);
                if (!block.Chain.AddBlock(block))
                {
                    throw new Exception("test block failed");
                }
            }

            // side chain send
            Hash sideSendHash;
            {
                var transactions = new List<Transaction>();
                var script = ScriptUtils.CallContractScript(nexus.RootChain, "SendTokens", bankChain.Address, ownerKey.Address, targetAddress, Nexus.NativeTokenSymbol, TokenUtils.ToBigInteger(7));
                var tx = new Transaction(script, 0, 0);
                tx.Sign(ownerKey);
                transactions.Add(tx);

                var block = new Block(nexus.RootChain, ownerKey.Address, Timestamp.Now, transactions, nexus.RootChain.lastBlock);
                if (!block.Chain.AddBlock(block))
                {
                    throw new Exception("test block failed");
                }

                sideSendHash = tx.Hash;
            }

            // side chain receive
            {
                var transactions = new List<Transaction>();
                var script = ScriptUtils.CallContractScript(bankChain, "ReceiveTokens", nexus.RootChain.Address, targetAddress, sideSendHash);
                var tx = new Transaction(script, 0, 0);
                tx.Sign(ownerKey);
                transactions.Add(tx);

                var block = new Block(bankChain, ownerKey.Address, Timestamp.Now, transactions, nexus.RootChain.lastBlock);
                if (!block.Chain.AddBlock(block))
                {
                    throw new Exception("test block failed");
                }
            }
            #endregion

            var curPath = Directory.GetCurrentDirectory();
            Console.WriteLine("Current path: " + curPath);

            var site = HostBuilder.CreateSite(args, "public");
            var viewsRenderer = new ViewsRenderer(site, "views");
            viewsRenderer.SetupControllers(nexus);
            viewsRenderer.InitMenus();
            viewsRenderer.SetupHandlers();
            viewsRenderer.RunServer();
        }
    }
}
