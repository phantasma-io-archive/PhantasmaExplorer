using Phantasma.Explorer.Domain.Entities;
using System.Collections.Generic;

namespace Phantasma.Explorer.Utils
{
    public static class ChainUtils
    {
        public static Dictionary<string, string> SetupChainChildren(List<Chain> chains, string chainAddress)
        {
            var result = new Dictionary<string, string>();

            foreach (var repoChain in chains)
            {
                if (repoChain.ParentAddress.Equals(chainAddress))
                {
                    result[repoChain.Name] = repoChain.Address;
                }
            }

            return result;
        }
    }
}
