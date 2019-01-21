using System;
using System.Collections.Generic;
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
        private readonly List<string> _addressChanged;

        private static int _retries;

        private const int MaxRetries = 5;

        public ExplorerSync()
        {
            _phantasmaRpcService = Explorer.AppServices.GetService<IPhantasmaRpcService>();
            _addressChanged = new List<string>();
        }

        public static void StartSync()
        {
            var explorerSync = new ExplorerSync();
            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;
                try
                {
                    if (_retries >= MaxRetries)
                    {
                        Console.WriteLine("Something went wrong with synchronization");
                        return;
                    }
                    while (true)
                    {
                        await explorerSync.Sync();
                        Thread.Sleep(6000);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    StartSync();
                    _retries++;
                }
            }).Start();
        }

        public async Task Sync()
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();
            foreach (var chain in context.Chains)
            {
                while (await _phantasmaRpcService.GetBlockHeight.SendRequestAsync(chain.Address) > chain.Height)
                {
                    Console.WriteLine($"NEW BLOCK: Chain: {chain.Name}, block: {chain.Height + 1}");
                    var block = await _phantasmaRpcService.GetBlockByHeight.SendRequestAsync(chain.Address, (int)(chain.Height + 1));

                    await SyncBlock(context, chain, block);
                }
            }
            //todo find smarter way to do this
            await UpdateAccountBalances(context);
        }

        public async Task SyncBlock(ExplorerDbContext context, Chain chain, BlockDto blockDto)
        {
            Console.WriteLine($"Seeding block {blockDto.Height}");

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

                    AddToUpdateList(eventDto.EventAddress);
                    UpdateAccount(context, transaction, eventDto.EventAddress);
                }
            }

            chain.Height = block.Height;
            context.Update(chain);

            await context.SaveChangesAsync();

            Console.WriteLine($"Finished seeding block {blockDto.Height}");
            Console.WriteLine("****************************************");
            Console.WriteLine();
        }

        private void UpdateAccount(ExplorerDbContext context, Transaction transaction, string eventDtoEventAddress)
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
            }
            else
            {
                account = new Account
                {
                    Address = eventDtoEventAddress
                };

                account.AccountTransactions.Add(new AccountTransaction { Account = account, Transaction = transaction });
            }
        }

        private async Task UpdateAccountBalances(ExplorerDbContext context)
        {
            Console.WriteLine("*********************************");
            Console.WriteLine("Updating account balances");

            foreach (var address in _addressChanged)
            {
                var account = context.Accounts.Find(address);

                if (account != null)
                {
                    var accountBalance = await _phantasmaRpcService.GetAccount.SendRequestAsync(account.Address);

                    account.Name = accountBalance.Name;
                    account.TokenBalance.Clear();
                    account.NonFungibleTokens.Clear();

                    foreach (var tokenBalance in accountBalance.Tokens)
                    {
                        var token = context.Tokens.Find(tokenBalance.Symbol);

                        if ((token.Flags & TokenFlags.Fungible) != 0)
                        {
                            account.TokenBalance.Add(new FBalance
                            {
                                Chain = tokenBalance.ChainName,
                                TokenSymbol = tokenBalance.Symbol,
                                Amount = tokenBalance.Amount
                            });
                        }
                        else
                        {
                            UpdateNfTokenBalance(context, account, tokenBalance);
                        }

                        await context.SaveChangesAsync();
                    }
                }
            }

            _addressChanged.Clear();
        }

        private void UpdateNfTokenBalance(ExplorerDbContext context, Account account, BalanceSheetDto tokenBalance)
        {
            foreach (var tokenId in tokenBalance.Ids)
            {
                var existingToken = context.NonFungibleTokens.SingleOrDefault(p => p.Id.Equals(tokenId));
                if (existingToken == null)
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
                else
                {
                    existingToken.Account = account;
                }
            }
        }

        private void AddToUpdateList(string address)
        {
            if (!_addressChanged.Contains(address))
            {
                _addressChanged.Add(address);
            }
        }
    }
}

