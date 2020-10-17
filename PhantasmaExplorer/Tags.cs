using LunarLabs.Templates;
using Phantasma.Core.Types;
using Phantasma.Cryptography;
using Phantasma.Domain;
using Phantasma.Explorer.Utils;
using Phantasma.Numerics;
using Phantasma.Pay.Chains;
using System;
using System.Globalization;

namespace Phantasma.Explorer
{
    public class ValueTag : TemplateNode
    {
        private readonly RenderingKey _key;

        public ValueTag(Document doc, string key) : base(doc)
        {
            _key = RenderingKey.Parse(key, RenderingType.Numeric);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(_key);
            if (temp == null)
            {
                return;
            }

            if (!decimal.TryParse(temp.ToString(), out var price))
            {
                return;
            }

            var Symbol = "$";
            context.output.Append($"{Symbol}{price:F2}");
        }
    }

    public class NumberTag : TemplateNode
    {
        private readonly RenderingKey _key;

        public NumberTag(Document doc, string key) : base(doc)
        {
            _key = RenderingKey.Parse(key, RenderingType.Numeric);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(_key);
            if (temp == null)
            {
                return;
            }

            if (!decimal.TryParse(temp.ToString(), out var number))
            {
                return;
            }

            context.output.Append(ToKMB(number));
        }

        public static string ToKMB(decimal num)
        {
            if (num > 999999999 || num < -999999999)
            {
                return num.ToString("0,,,.###B", CultureInfo.InvariantCulture);
            }
            else
            if (num > 999999 || num < -999999)
            {
                return num.ToString("0,,.##M", CultureInfo.InvariantCulture);
            }
            else
            if (num > 999 || num < -999)
            {
                return num.ToString("0,.#K", CultureInfo.InvariantCulture);
            }
            else
            {
                return num.ToString(CultureInfo.InvariantCulture);
            }
        }
    }

    public class HexTag : TemplateNode
    {
        private readonly RenderingKey _key;

        public HexTag(Document doc, string key) : base(doc)
        {
            _key = RenderingKey.Parse(key, RenderingType.Any);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(_key);
            if (temp == null)
            {
                return;
            }

            var array = temp as byte[];

            if (array != null && array.Length > 0)
            {
                context.output.Append(Base16.Encode(array));
            }
            else
            {
                context.output.Append("-");

            }
        }

        public static string ToKMB(decimal num)
        {
            if (num > 999999999 || num < -999999999)
            {
                return num.ToString("0,,,.###B", CultureInfo.InvariantCulture);
            }
            else
            if (num > 999999 || num < -999999)
            {
                return num.ToString("0,,.##M", CultureInfo.InvariantCulture);
            }
            else
            if (num > 999 || num < -999)
            {
                return num.ToString("0,.#K", CultureInfo.InvariantCulture);
            }
            else
            {
                return num.ToString(CultureInfo.InvariantCulture);
            }
        }
    }

    public class TimeAgoTag : TemplateNode
    {
        private readonly RenderingKey _key;

        public TimeAgoTag(Document doc, string key) : base(doc)
        {
            _key = RenderingKey.Parse(key, RenderingType.DateTime);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(_key);
            if (temp == null)
            {
                return;
            }

            DateTime date;

            if (temp is DateTime)
            {
                date = (DateTime)temp;
            }
            else
            {
                date = (DateTime)((Timestamp)temp);
            }

            var timeago = DateUtils.RelativeTime(date);

            context.output.Append($"{timeago}");
        }
    }

    public class AsyncTag : TemplateNode
    {
        private readonly string _key;
        private readonly Random _rnd = new Random();

        public AsyncTag(Document doc, string key) : base(doc)
        {
            _key = key;
        }

        public override void Execute(RenderingContext context)
        {
            /*
            var temp = TemplateEngine.EvaluateObject(context, pointer, key);
            if (temp == null)
            {
                return;
            }*/

            var id = _rnd.Next().ToString();
            var url = _key.Replace("_", "/");
            var obj = "{url:'" + url + "', id:'" + id + "'}";
            context.output.Append($"<script>dynamicContents.push({obj});</script><div style=\"display:inline\" id=\"dynamic_{id}\">...</div>");
        }
    }

    public class LinkChainTag : TemplateNode
    {
        private readonly RenderingKey _key;

        public LinkChainTag(Document doc, string key) : base(doc)
        {
            _key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(_key);
            if (temp == null)
            {
                return;
            }

            var chain = (string)temp;
            context.output.Append(!string.IsNullOrEmpty((string)temp) ? $"<a href=\"/chain/{chain}\">{chain}</a>" : "-");
        }
    }

    public class LinkTransactionTag : TemplateNode
    {
        private readonly RenderingKey _key;

        public LinkTransactionTag(Document doc, string key) : base(doc)
        {
            _key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(_key);
            if (temp == null)
            {
                return;
            }

            string hash;

            if (temp is Hash)
            {
                hash = ((Hash)temp).ToString();
            }
            else
            {
                hash = (string)temp;
            }

            context.output.Append($"<a href=/tx/{hash}>{hash}</a>");
        }
    }

    public class LinkOrganizationTag : TemplateNode
    {
        private readonly RenderingKey _key;

        public LinkOrganizationTag(Document doc, string key) : base(doc)
        {
            _key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(_key);
            if (temp == null)
            {
                return;
            }

            var org = (OrganizationData)temp;

            context.output.Append($"<a href=/dao/{org.ID}>{org.Name}</a>");
        }
    }

    public class LinkContractTag : TemplateNode
    {
        private readonly RenderingKey _key;

        public LinkContractTag(Document doc, string key) : base(doc)
        {
            _key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(_key);
            if (temp == null)
            {
                return;
            }

            var contractName = (string)temp;

            context.output.Append($"<a href=/contract/{contractName}>{contractName}</a>");
        }
    }

    public class LinkBlockTag : TemplateNode
    {
        private readonly RenderingKey _chainKey;
        private readonly RenderingKey _hashKey;

        public LinkBlockTag(Document doc, string key) : base(doc)
        {
            var temp = key.Split(',');
            _chainKey = RenderingKey.Parse(temp[0], RenderingType.String);
            _hashKey = RenderingKey.Parse(temp[1], RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(_hashKey);
            if (temp == null)
            {
                return;
            }

            string hash;

            if (temp is Hash)
            {
                hash = ((Hash)temp).ToString();
            }
            else
            {
                hash = (string)temp;
            }

            temp = context.EvaluateObject(_chainKey);
            if (temp == null)
            {
                return;
            }

            var chain = (string)temp;

            context.output.Append($"<a href=/block/{chain}/{hash}>{hash}</a>");
        }
    }

    public class LinkAddressTag : TemplateNode
    {
        private readonly RenderingKey _key;
        private readonly NexusData nexus;

        private bool hideName;

        public LinkAddressTag(NexusData nexus, Document doc, string key) : base(doc)
        {
            this.nexus = nexus;

            if (key.StartsWith("_"))
            {
                hideName = true;
                key = key.Substring(1);
            }

            _key = RenderingKey.Parse(key, RenderingType.String);
        }

        public static string GenerateLink(NexusData nexus, Address address, string name = null)
        {
            if (address.IsInterop)
            {
                try
                {
                    string addressText = null;
                    string url = null;

                    switch (address.PlatformID)
                    {
                        case NeoWallet.NeoID:
                            {
                                addressText = Pay.Chains.NeoWallet.DecodeAddress(address);
                                url = "https://neoscan.io/address/";
                                break;
                            }

                        case EthereumWallet.EthereumID:
                            {
                                addressText = Pay.Chains.EthereumWallet.DecodeAddress(address);
                                url = "https://etherscan.io/address/";
                                break;
                            }
                    }

                    if (url != null && addressText != null)
                    {
                        return $"<a href=\"{url}/{addressText}\" target=\"_blank\">{addressText}</a>";
                    }

                }
                catch (Exception e)
                {
                    // skip for now, check later tx ED66C4F42E52EB720F7B4DC25B2CE25F65315F384F1F6A27BAEC56C01CE36094
                }
            }

            if (name == null)
            {
                var account = nexus.FindAccount(address, false);
                if (account != null && account.Name != ValidationUtils.ANONYMOUS)
                {
                    name = account.Name;
                }
                else
                {
                    name = address.Text;
                }
            }

            return $"<a href=\"/address/{address.Text}\">{name}</a>";
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(_key);
            if (temp == null)
            {
                return;
            }

            Address address;

            if (temp is Address)
            {
                address = (Address)temp;
            }
            else
            {
                address = Address.FromText((string)temp);
            }

            string output;

            if (hideName)
            {
                output = GenerateLink(this.nexus, address, address.Text);
            }
            else
            {
                output = GenerateLink(this.nexus, address);
            }

            context.output.Append(output);
        }
    }

    public class DescriptionTag : TemplateNode
    {
        private readonly RenderingKey _key;

        public DescriptionTag(Document doc, string key) : base(doc)
        {
            _key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(_key);
            if (temp == null)
            {
                return;
            }

            if (temp is TransactionData)
            {
                var tx = (TransactionData)temp;
                context.output.Append(tx.Description);
            }

            /*
            //< td >< a href = "/token/{{Symbol}}" >{ { Name} }</ a ></ td >
            var vm = (TransactionViewModel)temp;
            if (string.IsNullOrEmpty(vm.TokenSymbol))
            {
                context.output.Append($"{vm.Description}");
                return;
            }
            context.output.Append($"{vm.AmountTransfer} <a href=/token/{vm.TokenSymbol}> {vm.TokenSymbol} </a> sent from " +
                                  $"<a href=/address/{vm.SenderAddress}>{vm.SenderAddress}</a> to " +
                                  $"<a href=/address/{vm.ReceiverAddress}>{vm.ReceiverAddress}</a>");
            */
        }
    }

    public class LinkExternalTag : TemplateNode
    {
        private readonly RenderingKey _key;

        public LinkExternalTag(Document doc, string key) : base(doc)
        {
            _key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(_key);
            if (temp == null)
            {
                return;
            }

            var url = (string)temp;
            context.output.Append($"<a href=\"{url}\">{url}</a>");
        }
    }
}
