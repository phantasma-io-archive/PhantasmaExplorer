using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.Domain.Entities
{
    public class Token
    {
        public Token()
        {
            Flags = TokenFlags.None;
        }

        public string Symbol { get; set; }
        public string Name { get; set; }
        public uint Decimals { get; set; }
        public uint TransactionCount { get; set; }
        public string CurrentSupply { get; set; }
        public string MaxSupply { get; set; }
        public string OwnerAddress { get; set; }

        public TokenFlags Flags { get; set; }
    }
}
