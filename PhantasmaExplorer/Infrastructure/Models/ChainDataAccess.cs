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

        public Dictionary<Hash, BlockDto> Blocks { get; set; }
        private Dictionary<TokenDto, BalanceSheet> _tokenBalances = new Dictionary<TokenDto, BalanceSheet>();
        private Dictionary<TokenDto, OwnershipSheet> _tokenOwnerships = new Dictionary<TokenDto, OwnershipSheet>();

        public ChainDataAccess(ChainDto dto, Dictionary<Hash, BlockDto> blocks)
        {
            Name = dto.Name;
            Address = dto.Address;
            ParentAddress = dto.ParentAddress;
            Blocks = blocks;
        }


        public BalanceSheet GetTokenBalances(TokenDto dto) =>  null;//todo

        public OwnershipSheet GetTokenOwnerships(TokenDto dto) => null;//todo

        // Get DTOs
        public List<BlockDto> GetBlocks => Blocks.Values.ToList();

        public ChainDto GetChainInfo => new ChainDto
        {
            Address = Address,
            Name = Name,
            Children = Children,
            Height = Height,
            ParentAddress = ParentAddress
        };
    }
}
