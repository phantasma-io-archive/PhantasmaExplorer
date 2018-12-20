using Phantasma.Cryptography;

namespace Phantasma.Explorer.Utils
{
    public static class AddressUtils
    {
        public static Address ValidateAddress(string addressInput)
        {
            if (Address.IsValidAddress(addressInput))
            {
                return Address.FromText(addressInput);
            }
            return Address.Null;
        }
    }
}
