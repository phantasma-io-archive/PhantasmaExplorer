using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Numerics;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.Infrastructure.Models
{
    public class ChainDataAccess //todo revisit public stuff
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string ParentAddress { get; set; }
        public uint Height { get; set; }
        public List<ChainDto> Children { get; set; }

        private readonly Dictionary<Hash, BlockDto> _blocks = new Dictionary<Hash, BlockDto>();
        private readonly Dictionary<TokenDto, BalanceSheet> _tokenBalances = new Dictionary<TokenDto, BalanceSheet>();
        private readonly Dictionary<TokenDto, OwnershipSheet> _tokenOwnerships = new Dictionary<TokenDto, OwnershipSheet>(); //todo public add
        private readonly Dictionary<Address, HashSet<TransactionDto>> _addressTransactions = new Dictionary<Address, HashSet<TransactionDto>>();

        public ChainDataAccess(ChainDto dto)
        {
            Name = dto.Name;
            Address = dto.Address;
            ParentAddress = dto.ParentAddress;
            Height = dto.Height;
        }

        public void AddChildren(List<ChainDto> children)
        {
            Children = new List<ChainDto>(children);
        }

        public void SetBlock(BlockDto block)
        {
            _blocks[Hash.Parse(block.Hash)] = block;
            Height = block.Height;
        }

        public void UpdateAddressTransactions(Address address, TransactionDto tx)
        {
            if (_addressTransactions.ContainsKey(address))
            {
                _addressTransactions[address].Add(tx);
            }
            else
            {
                _addressTransactions.Add(address, new HashSet<TransactionDto> { tx });
            }

        }

        public void UpdateTokenBalance(TokenDto token, Address address, BigInteger balance, bool add)
        {
            var sheet = _tokenBalances.ContainsKey(token) ? _tokenBalances[token] : new BalanceSheet();
            if (add)
            {
                sheet.Add(address, balance);
            }
            else
            {
                sheet.Subtract(address, balance);
            }
            _tokenBalances[token] = sheet;
        }

        public void UpdateTokenOwnership(TokenDto token, Address address, BigInteger id, bool add)
        {
            var sheet = _tokenOwnerships.ContainsKey(token) ? _tokenOwnerships[token] : new OwnershipSheet();
            if (add)
            {
                sheet.Give(address, id);
            }
            else
            {
                sheet.Take(address, id);
            }
            _tokenOwnerships[token] = sheet;
        }

        public BalanceSheet GetTokenBalances(TokenDto dto) => _tokenBalances.ContainsKey(dto) ? _tokenBalances[dto] : null;

        public OwnershipSheet GetTokenOwnerships(TokenDto dto) => _tokenOwnerships.ContainsKey(dto) ? _tokenOwnerships[dto] : null;

        public BigInteger GetTokenAddressBalance(TokenDto dto, Address address) => _tokenBalances.ContainsKey(dto) ? _tokenBalances[dto].Get(address) : 0;

        public IEnumerable<BigInteger> GetTokenAddressOwnership(TokenDto dto, Address address) => _tokenOwnerships.ContainsKey(dto) ? _tokenOwnerships[dto].Get(address) : new BigInteger[0];

        // Get DTOs
        public List<BlockDto> GetBlocks => _blocks.Values.OrderByDescending(p => p.Height).ToList(); //todo remove orderBy, and make it save in correct order

        public ChainDto GetChainInfo => new ChainDto
        {
            Address = Address,
            Name = Name,
            Height = Height,
            ParentAddress = ParentAddress
        };

        //
        public BlockDto FindBlockByHash(Hash hash)
        {
            if (_blocks.ContainsKey(hash))
            {
                return _blocks[hash];
            }

            return null;
        }

        public BlockDto FindBlockByHeight(int height)
        {
            return GetBlocks.Find(p => p.Height == height);
        }
    }
}
