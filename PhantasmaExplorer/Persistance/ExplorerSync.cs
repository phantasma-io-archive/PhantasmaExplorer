using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Domain.ValueObjects;
using Phantasma.RpcClient.DTOs;
using Phantasma.RpcClient.Interfaces;
using TokenFlags = Phantasma.Explorer.Domain.Entities.TokenFlags;

namespace Phantasma.Explorer.Persistance
{
    public class ExplorerSync
    {
        private readonly IPhantasmaRpcService _phantasmaRpcService;

        public ExplorerSync()
        {
            _phantasmaRpcService = Explorer.AppServices.GetService<IPhantasmaRpcService>();
        }

        public static async Task StartSync()
        {
            var explorerSync = new ExplorerSync();
            while (true)
            {
                Thread.Sleep(2000);
                await explorerSync.Sync();
            }
        }

        public async Task Sync()
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();
            foreach (var chain in context.Chains)
            {
                var height = await _phantasmaRpcService.GetBlockHeight.SendRequestAsync(chain.Address);
                if (height > chain.Height)
                {
                    Console.WriteLine($"NEW BLOCK: Chain: {chain.Name}, block: {height}");
                    var block = await _phantasmaRpcService.GetBlockByHeight.SendRequestAsync(chain.Address, height);
                    await SyncBlockAsync(context, chain, block);
                }
            }
        }

        public async Task SyncBlockAsync(ExplorerDbContext context, Chain chain, BlockDto blockDto)
        {
            Console.WriteLine($"Seeding block {blockDto.Height}");

            var block = new Block
            {
                Chain = chain,
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
                    Script = transactionDto.Script
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

                    await UpdateAccount(context, transaction, eventDto.EventAddress);
                }

                block.Transactions.Add(transaction);
            }

            chain.Blocks.Add(block);

            Console.WriteLine($"Finished seeding block {blockDto.Height}");
            Console.WriteLine("****************************************");

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

            //Update balance
            var accountBalance = await _phantasmaRpcService.GetAccount.SendRequestAsync(account.Address);
            foreach (var tokenBalance in accountBalance.Tokens)
            {
                var token = context.Tokens.Find(tokenBalance.Symbol);
                if ((token.Flags & TokenFlags.Fungible) != 0)
                {
                    UpdateTokenBalance(account, tokenBalance);
                }
                else
                {
                    UpdateNfTokenBalance(account, tokenBalance);
                }
            }

            await context.SaveChangesAsync();
        }

        private void UpdateTokenBalance(Account account, BalanceSheetDto tokenBalance)
        {
            account.TokenBalance.Add(new FBalance
            {
                Chain = tokenBalance.ChainName,
                TokenSymbol = tokenBalance.Symbol,
                Amount = tokenBalance.Amount
            });
        }

        private void UpdateNfTokenBalance(Account account, BalanceSheetDto tokenBalance)
        {
            foreach (var tokenId in tokenBalance.Ids)
            {
                var nftoken = new NonFungibleToken
                {
                    Chain = tokenBalance.ChainName,
                    TokenSymbol = tokenBalance.Symbol,
                    Id = tokenId,
                    Account = account,
                };

                account.NonFungibleTokens.Add(nftoken);
            }
        }
    }
}

