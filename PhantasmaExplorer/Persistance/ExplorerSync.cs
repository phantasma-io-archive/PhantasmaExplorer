using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Application;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Domain.ValueObjects;
using Phantasma.Explorer.Persistance.Infrastructure;
using Phantasma.Explorer.Utils;
using Phantasma.Numerics;
using Phantasma.RpcClient.DTOs;
using Phantasma.RpcClient.Interfaces;

namespace Phantasma.Explorer.Persistance
{
    public class ExplorerSync
    {
        private static int _retries;
        private const int MaxRetries = 5;
        public static bool ContinueSync { get; set; } = true;

        private int SyncAdditionalDataCounter { get; set; }

        private readonly IPhantasmaRpcService _phantasmaRpcService;
        private readonly List<string> _addressChanged;

        public ExplorerSync()
        {
            _phantasmaRpcService = Explorer.AppServices.GetService<IPhantasmaRpcService>();
            _addressChanged = new List<string>();
        }

        public static void StartSync(ExplorerDbContext context)
        {
            var explorerSync = new ExplorerSync();
            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;
                try
                {
                    if (_retries >= MaxRetries)
                    {
                        Console.WriteLine("There are no new blocks to sync.");
                        _retries = 0;
                        return;
                    }
                    while (ContinueSync)
                    {
                        Console.WriteLine("Remember, to stop sync process safely, press 3");
                        Console.WriteLine("It may take a while to stop");
                        Console.WriteLine("\n\n");

                        await explorerSync.Sync(context);
                        Thread.Sleep(AppSettings.SyncTime);
                    }

                    Console.WriteLine("Sync has stopped!");

                    _retries = 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    StartSync(context);
                    _retries++;
                }
            }).Start();
        }

        public static void UpdateAllAddressBalances()
        {
            var explorerSync = new ExplorerSync();
            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;
                var context = Explorer.AppServices.GetService<ExplorerDbContext>();

                await explorerSync.UpdateAccountBalances(context, context.Accounts.Select(p => p.Address).ToList());

                Console.WriteLine("Account balances are updated");
            }).Start();
        }

        public async Task Sync(ExplorerDbContext context)
        {
            SyncAdditionalDataCounter++;

            foreach (var chain in context.Chains)
            {
                while (await _phantasmaRpcService.GetBlockHeight.SendRequestAsync(chain.Address) > chain.Height)
                {
                    if (ContinueSync)
                    {
                        Console.WriteLine($"NEW BLOCK: Chain: {chain.Name}, block: {chain.Height + 1}");
                        var block = await _phantasmaRpcService.GetBlockByHeight.SendRequestAsync(chain.Address, (int)(chain.Height + 1));

                        await SyncBlock(context, chain, block);
                    }
                    else
                    {
                        Console.WriteLine("Sync has stopped");
                        return;
                    }
                }
            }
            //todo find smarter way to do this
            await UpdateAccountBalances(context, _addressChanged);
            _addressChanged.Clear();

            if (SyncAdditionalDataCounter >= 5)
            {
                Console.WriteLine("Sync new chains?");
                await SyncChains(context);

                Console.WriteLine("Sync new apps?");
                var appList = await _phantasmaRpcService.GetApplications.SendRequestAsync();
                await SyncUtils.SyncApps(context, appList);

                Console.WriteLine("Sync new tokens?");
                var tokenList = await _phantasmaRpcService.GetTokens.SendRequestAsync();
                await SyncUtils.SyncToken(context, tokenList);

                SyncAdditionalDataCounter = 0;
            }
        }

        private async Task SyncChains(ExplorerDbContext context)
        {
            var chainsDto = await _phantasmaRpcService.GetChains.SendRequestAsync();
            await SyncUtils.SyncChains(chainsDto, context);
        }

        private async Task SyncBlock(ExplorerDbContext context, Chain chain, BlockDto blockDto)
        {
            if (context.Blocks.FirstOrDefault(p => p.Hash.Equals(blockDto.Hash)) != null) return;

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
                    var domainEvent = new Event
                    {
                        Data = eventDto.Data,
                        EventAddress = eventDto.EventAddress,
                        EventKind = eventDto.EventKind,
                    };
                    transaction.Events.Add(domainEvent);

                    AddToUpdateList(eventDto.EventAddress);
                    await SyncUtils.UpdateAccount(context, transaction, eventDto.EventAddress);                 
                }
            }

            chain.Height = block.Height;
            context.Update(chain);

            await context.SaveChangesAsync();

            Console.WriteLine($"Finished syncing block {blockDto.Height}");
            Console.WriteLine("****************************************");
            Console.WriteLine();
        }

        internal async Task UpdateAccountBalances(ExplorerDbContext context, List<string> addressList)
        {
            Console.WriteLine("*********************************");
            Console.WriteLine("Updating account balances");

            foreach (var address in addressList)
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
                        var token = context.Tokens.Find(tokenBalance.Symbol) ?? await SyncToken(context, tokenBalance.Symbol);
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
                else
                {
                    account = new Account
                    {
                        Address = address
                    };

                    await context.Accounts.AddAsync(account);
                }
            }
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

        private async Task<Token> SyncToken(ExplorerDbContext context, string symbol)
        {
            var tokensDto = await _phantasmaRpcService.GetTokens.SendRequestAsync();

            var tokenDto = tokensDto.SingleOrDefault(p => p.Symbol.Equals(symbol));

            if (tokenDto != null)
            {
                Token token = new Token
                {
                    Name = tokenDto.Name,
                    Symbol = tokenDto.Symbol,
                    Decimals = (uint)tokenDto.Decimals,
                    Flags = tokenDto.Flags,
                    MaxSupply = tokenDto.MaxSupply,
                    CurrentSupply = tokenDto.CurrentSupply,
                    OwnerAddress = tokenDto.OwnerAddress
                };

                if (tokenDto.MetadataList != null)
                {
                    foreach (var metadataDto in tokenDto.MetadataList)
                    {
                        token.MetadataList.Add(new TokenMetadata
                        {
                            Key = metadataDto.Key,
                            Value = metadataDto.Value
                        });
                    }
                }

                await context.Tokens.AddAsync(token);

                await context.SaveChangesAsync();

                return token;
            }

            return null;
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

