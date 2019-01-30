using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.Persistance.Infrastructure
{
    public static class SyncUtils
    {
        internal static async Task UpdateAccount(ExplorerDbContext context, Transaction transaction, string eventDtoEventAddress)
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

                await context.Accounts.AddAsync(account);

                account.AccountTransactions.Add(new AccountTransaction { Account = account, Transaction = transaction });
            }

            await context.SaveChangesAsync();
        }

        internal static async Task SyncChains(IList<ChainDto> chains, ExplorerDbContext context)
        {
            foreach (var chainDto in chains)
            {
                if (context.Chains.SingleOrDefault(p => p.Address.Equals(chainDto.Address)) == null)
                {
                    Console.WriteLine($"Sync {chainDto.Name} chain info");

                    var chain = new Chain
                    {
                        Address = chainDto.Address,
                        Name = chainDto.Name,
                        Height = chainDto.Height,
                        ParentAddress = chainDto.ParentAddress
                    };

                    await context.Accounts.AddAsync(new Account { Address = chain.Address });
                    await context.Chains.AddAsync(chain);
                }
            }

            await context.SaveChangesAsync();
        }

        internal static async Task SyncApps(ExplorerDbContext context, IList<AppDto> appList)
        {
            foreach (var dto in appList)
            {
                if (context.Apps.SingleOrDefault(p => p.Id.Equals(dto.Id)) == null)
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
        }

        internal static void AddToTokenTxCounter(ExplorerDbContext context, string tokenDataSymbol)
        {
            var token = context.Tokens.SingleOrDefault(p => p.Symbol.Equals(tokenDataSymbol));
            if (token != null)
            {
                token.TransactionCount++;
            }
        }
    }
}
