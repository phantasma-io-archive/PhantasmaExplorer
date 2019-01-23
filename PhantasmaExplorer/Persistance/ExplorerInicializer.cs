using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Domain.ValueObjects;
using Phantasma.RpcClient.Interfaces;

namespace Phantasma.Explorer.Persistance
{
    public class ExplorerInicializer
    {
        private IPhantasmaRpcService _phantasmaRpcService;

        public static async Task Initialize(ExplorerDbContext context)
        {
            var initializer = new ExplorerInicializer();
            await initializer.SeedEverythingAsync(context);
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
                    await SeedApps(context);
                }

                if (!context.Tokens.Any())
                {
                    await SeedTokens(context);
                }

                if (!context.Chains.Any())
                {
                    await SeedChains(context);
                }

                sw.Stop();
                Console.WriteLine("Elapsed time to initializing db = {0}", sw.Elapsed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Exception occurred during DB initialization, explorer cannot start");
                throw;
            }
        }

        private async Task SeedApps(ExplorerDbContext context)
        {
            var appList = await _phantasmaRpcService.GetApplications.SendRequestAsync();

            foreach (var dto in appList)
            {
                context.Apps.Add(new App
                {
                    Id = dto.Id,
                    Url = dto.Url,
                    Description = dto.Description,
                    Title = dto.Title,
                    Icon = dto.Icon
                });
            }

            await context.SaveChangesAsync();
        }

        private async Task SeedTokens(ExplorerDbContext context)
        {
            var tokenList = await _phantasmaRpcService.GetTokens.SendRequestAsync();

            foreach (var tokenDto in tokenList)
            {
                context.Tokens.Add(new Token
                {
                    Name = tokenDto.Name,
                    Symbol = tokenDto.Symbol,
                    Decimals = (uint)tokenDto.Decimals,
                    Flags = (TokenFlags)tokenDto.Flags,
                    MaxSupply = tokenDto.MaxSupply,
                    CurrentSupply = tokenDto.CurrentSupply,
                    OwnerAddress = tokenDto.OwnerAddress
                });
            }

            await context.SaveChangesAsync();
        }

        private async Task SeedChains(ExplorerDbContext context)
        {
            var chains = await _phantasmaRpcService.GetChains.SendRequestAsync();

            foreach (var chainDto in chains)
            {
                Console.WriteLine($"Seeding chain {chainDto.Name}");

                var chain = new Chain
                {
                    Address = chainDto.Address,
                    Name = chainDto.Name,
                    Height = chainDto.Height,
                    ParentAddress = chainDto.ParentAddress
                };

                context.Accounts.Add(new Account { Address = chain.Address });
                context.Chains.Add(chain);

                await SeedBlocks(context, chain);
            }

            await context.SaveChangesAsync();
        }

        private async Task SeedBlocks(ExplorerDbContext context, Chain chain)
        {
            var height = await _phantasmaRpcService.GetBlockHeight.SendRequestAsync(chain.Address);

            for (int i = 1; i <= height; i++)
            {
                Console.WriteLine($"Seeding block {i}");

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

                    //Events
                    foreach (var eventDto in transactionDto.Events)
                    {
                        transaction.Events.Add(new Event
                        {
                            Data = eventDto.Data,
                            EventAddress = eventDto.EventAddress,
                            EventKind = (EventKind)eventDto.EvtKind,
                        });

                        await UpdateAccount(context, transaction, eventDto.EventAddress);//todo some address are not getting saved
                    }

                    block.Transactions.Add(transaction);
                }

                chain.Blocks.Add(block);

                Console.WriteLine($"Finished seeding block {blockDto.Height}");
                Console.WriteLine("****************************************");
            }

            await context.SaveChangesAsync();
        }

        private async Task UpdateAccount(ExplorerDbContext context, Transaction transaction, string eventDtoEventAddress)
        {
            var account = context.Accounts.SingleOrDefault(p => p.Address.Equals(eventDtoEventAddress));

            if (account != null)
            {
                var accountTx = new AccountTransaction
                {
                    Account = account,
                    Transaction = transaction
                };

                if (account.AccountTransactions.Any(t => t.Transaction.Hash == transaction.Hash)) return;

                account.AccountTransactions.Add(accountTx);

                context.Accounts.Update(account);
            }
            else
            {
                account = new Account
                {
                    Address = eventDtoEventAddress
                };

                account.AccountTransactions.Add(new AccountTransaction { Account = account, Transaction = transaction });

                context.Accounts.Add(account);
            }

            await context.SaveChangesAsync();
        }
    }
}
