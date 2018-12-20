using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.Infrastructure.Models
{
    public class ChainDataAccess //todo revisit public stuff
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string ParentAddress { get; set; }
        public int Height { get; set; }
        public List<ChainDto> Children { get; set; }

        private Dictionary<Hash, BlockDto> _blocks;
        private Dictionary<TokenDto, BalanceSheet> _tokenBalances = new Dictionary<TokenDto, BalanceSheet>();
        private Dictionary<TokenDto, OwnershipSheet> _tokenOwnerships = new Dictionary<TokenDto, OwnershipSheet>();

        public ChainDataAccess(ChainDto dto, Dictionary<Hash, BlockDto> blocks)
        {
            Name = dto.Name;
            Address = dto.Address;
            ParentAddress = dto.ParentAddress;
            _blocks = blocks;
        }


        public BalanceSheet GetTokenBalances(TokenDto dto) => null;//todo

        public OwnershipSheet GetTokenOwnerships(TokenDto dto) => null;//todo

        // Get DTOs
        public List<BlockDto> GetBlocks => _blocks.Values.OrderByDescending(p => p.Height).ToList(); //todo remove orderBy, and make it save in correct order

        public ChainDto GetChainInfo => new ChainDto
        {
            Address = Address,
            Name = Name,
            Children = Children,
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
