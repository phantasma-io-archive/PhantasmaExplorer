using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Domain.ValueObjects;
using Phantasma.Numerics;
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
            }
            else
            {
                account = new Account
                {
                    Address = eventDtoEventAddress
                };

                account.AccountTransactions.Add(new AccountTransaction { Account = account, Transaction = transaction });
                
                await context.Accounts.AddAsync(account);
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
                        ParentAddress = chainDto.ParentAddress,
                        Contracts = chainDto.Contracts.ToArray()
                    };

                    await context.Accounts.AddAsync(new Account { Address = chain.Address });
                    await context.Chains.AddAsync(chain);
                }
            }

            await context.SaveChangesAsync();
        }

        internal static async Task SyncToken(ExplorerDbContext context, IList<TokenDto> tokenList)
        {
            foreach (var tokenDto in tokenList)
            {
                var contextToken = context.Tokens.SingleOrDefault(p => p.Symbol.Equals(tokenDto.Symbol));

                if (contextToken == null)
                {
                    contextToken = new Token
                    {
                        Name = tokenDto.Name,
                        Symbol = tokenDto.Symbol,
                        Decimals = (uint)tokenDto.Decimals,
                        Flags = tokenDto.Flags,
                        MaxSupply = tokenDto.MaxSupply,
                        CurrentSupply = tokenDto.CurrentSupply,
                        OwnerAddress = tokenDto.OwnerAddress,
                    };

                    if (tokenDto.MetadataList != null)
                    {
                        foreach (var metadataDto in tokenDto.MetadataList)
                        {
                            contextToken.MetadataList.Add(new TokenMetadata
                            {
                                Key = metadataDto.Key,
                                Value = metadataDto.Value
                            });
                        }
                    }

                    context.Tokens.Add(contextToken);
                }
                else
                {
                    if (!AreTokenEqual(tokenDto, contextToken))
                    {
                        contextToken.Name = tokenDto.Name;
                        contextToken.Decimals = (uint)tokenDto.Decimals;
                        contextToken.Flags = tokenDto.Flags;
                        contextToken.MaxSupply = tokenDto.MaxSupply;
                        contextToken.CurrentSupply = tokenDto.CurrentSupply;
                        contextToken.OwnerAddress = tokenDto.OwnerAddress;
                    }

                    if (!IsTokenMetadataEqual(tokenDto, contextToken))
                    {
                        contextToken.MetadataList.Clear();//reset

                        foreach (var metadataDto in tokenDto.MetadataList)
                        {
                            contextToken.MetadataList.Add(new TokenMetadata
                            {
                                Key = metadataDto.Key,
                                Value = metadataDto.Value
                            });
                        }
                    }

                    context.Tokens.Update(contextToken);
                }
            }

            await context.SaveChangesAsync();
        }

        private static bool AreTokenEqual(TokenDto tokenDto, Token token)
        {
            return tokenDto.Symbol.Equals(token.Symbol)
                   && tokenDto.CurrentSupply.Equals(token.CurrentSupply)
                   && tokenDto.MaxSupply.Equals(token.MaxSupply)
                   && tokenDto.Decimals.Equals((int)token.Decimals)
                   && tokenDto.Flags.Equals(token.Flags)
                   && tokenDto.Name.Equals(token.Name)
                   && tokenDto.OwnerAddress.Equals(token.OwnerAddress);
        }

        private static bool IsTokenMetadataEqual(TokenDto tokenDto, Token token)
        {
            if (tokenDto.MetadataList == null)
            {
                if (!token.MetadataList.Any()) return true;
            }
            else
            {
                foreach (var tokenMetadataDto in tokenDto.MetadataList)
                {
                    if (token.MetadataList.SingleOrDefault(p =>
                            p.Key.Equals(tokenMetadataDto.Key) && p.Value.Equals(tokenMetadataDto.Value.Decode())) != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
