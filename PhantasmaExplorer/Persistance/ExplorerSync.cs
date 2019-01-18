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

        public static void StartSync()
        {
            var explorerSync = new ExplorerSync();
            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;
                try
                {
                    while (true)
                    {
                        await explorerSync.Sync();
                        Thread.Sleep(2000);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    StartSync(); //todo this does not work
                }
            }).Start();
        }

        public async Task Sync()
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();
            foreach (var chain in context.Chains)
            {
                var remoteHeight = await _phantasmaRpcService.GetBlockHeight.SendRequestAsync(chain.Address);
                var localHeight = chain.Height;
                while (remoteHeight >= localHeight)
                {
                    Console.WriteLine($"NEW BLOCK: Chain: {chain.Name}, block: {remoteHeight}");
                    var block = await _phantasmaRpcService.GetBlockByHeight.SendRequestAsync(chain.Address, (int)(localHeight + 1));
                    await SyncBlock(context, chain, block); //todo this is throwing duplicated block key exception

                    //need to fetch height again bc context may not be updated
                    localHeight = chain.Height;
                }
            }
        }

        public async Task SyncBlock(ExplorerDbContext context, Chain chain, BlockDto blockDto)
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

            context.SaveChanges();
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

            context.SaveChanges();
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

