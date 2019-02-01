using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Domain.ValueObjects;
using Phantasma.Explorer.Persistance.Infrastructure;
using Phantasma.Explorer.Site;
using Phantasma.Explorer.Utils;
using Phantasma.RpcClient.Interfaces;

namespace Phantasma.Explorer.Persistance
{
    public class ExplorerInicializer
    {
        private IPhantasmaRpcService _phantasmaRpcService;

        public static async Task<bool> Initialize(ExplorerDbContext context)
        {
            if (context.Chains.Any())
            {
                return false;
            }

            var initializer = new ExplorerInicializer();
            await initializer.SeedEverythingAsync(context);

            return true;
        }

        public async Task SeedEverythingAsync(ExplorerDbContext context)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                context.Database.EnsureCreated();
                _phantasmaRpcService = (IPhantasmaRpcService)Explorer.AppServices.GetService(typeof(IPhantasmaRpcService));

                if (!context.Apps.Any())
                {
                    var appList = await _phantasmaRpcService.GetApplications.SendRequestAsync();
                    await SyncUtils.SyncApps(context, appList);
                }

                if (!context.Tokens.Any())
                {
                    var tokenList = await _phantasmaRpcService.GetTokens.SendRequestAsync();
                    await SyncUtils.SyncToken(context, tokenList);
                }

                if (!context.Chains.Any())
                {
                    await SeedChains(context);
                }

                if (!context.Blocks.Any())
                {
                    await SeedBlocks(context);
                }

                sw.Stop();
                Console.WriteLine("Elapsed time to initializing db = {0}", sw.Elapsed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Exception occurred during DB initialization, explorer cannot start");
            }
        }

        private async Task SeedChains(ExplorerDbContext context)
        {
            var chainsDto = await _phantasmaRpcService.GetChains.SendRequestAsync();
            await SyncUtils.SyncChains(chainsDto, context);
        }

        private async Task SeedBlocks(ExplorerDbContext context)
        {
            foreach (var chain in context.Chains)
            {
                Console.WriteLine($"Seeding {chain.Name} chain blocks");
                await SeedBlocksByChain(context, chain);
            }
        }

        private async Task SeedBlocksByChain(ExplorerDbContext context, Chain chain)
        {
            var height = await _phantasmaRpcService.GetBlockHeight.SendRequestAsync(chain.Address);
            using (var progress = new ProgressBar())
            {
                for (int i = 1; i <= height; i++)
                {
                    progress.Report((double)i / height);

                    var blockDto = await _phantasmaRpcService.GetBlockByHeight.SendRequestAsync(chain.Address, i);
                    var block = new Block
                    {
                        Chain = chain,
                        ChainName = chain.Name,
                        Hash = blockDto.Hash,
                        PreviousHash = blockDto.PreviousHash,
                        Timestamp = blockDto.Timestamp,
                        Height = blockDto.Height,
                        Payload = blockDto.Payload,
                        Reward = blockDto.Reward,
                        ValidatorAddress = blockDto.ValidatorAddress
                    };

                    //Transactions
                    foreach (var transactionDto in blockDto.Txs)
                    {
                        var transaction = new Transaction
                        {
                            Block = block,
                            Hash = transactionDto.Txid,
                            Timestamp = transactionDto.Timestamp,
                            Script = transactionDto.Script,
                            Result = transactionDto.Result
                        };

                        bool counterIncremented = false;
                        //Events
                        foreach (var eventDto in transactionDto.Events)
                        {
                            var domainEvent = new Event
                            {
                                Data = eventDto.Data,
                                EventAddress = eventDto.EventAddress,
                                EventKind = eventDto.EventKind,
                            };
                            transaction.Events.Add(domainEvent);

                            await SyncUtils.UpdateAccount(context, transaction, eventDto.EventAddress);

                            if (!counterIncremented)
                            {
                                if (TransactionUtils.IsTransferEvent(domainEvent))
                                {
                                    var tokenSymbol = TransactionUtils.GetTokenSymbolFromTokenEventData(domainEvent);
                                    SyncUtils.AddToTokenTxCounter(context, tokenSymbol);
                                    counterIncremented = true;
                                }
                            }
                        }

                        block.Transactions.Add(transaction);
                    }

                    chain.Height = block.Height;
                    chain.Blocks.Add(block);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
