using System;
using LunarLabs.WebServer.Templates;
using Phantasma.Blockchain;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Site
{
    public class ValueTag : TemplateNode
    {
        private RenderingKey key;

        public ValueTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = RenderingKey.Parse(key, RenderingType.Numeric);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(key);
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

    public class TimeAgoTag : TemplateNode
    {
        private RenderingKey key;

        public TimeAgoTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = RenderingKey.Parse(key, RenderingType.DateTime);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(key);
            if (temp == null)
            {
                return;
            }

            var timestamp = temp is DateTime timestamp1 ? timestamp1 : new DateTime();

            var timeago = DateUtils.RelativeTime(timestamp);

            context.output.Append($"{timeago}");
        }
    }

    public class AsyncTag : TemplateNode
    {
        private string key;
        private Random rnd = new Random();

        public AsyncTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = key;
        }

        public override void Execute(RenderingContext context)
        {
            /*
            var temp = TemplateEngine.EvaluateObject(context, pointer, key);
            if (temp == null)
            {
                return;
            }*/

            var id = rnd.Next().ToString();
            var url = key.Replace("_", "/");
            var obj = "{url:'" + url + "', id:'" + id + "'}";
            context.output.Append($"<script>dynamicContents.push({obj});</script><div style=\"display:inline\" id=\"dynamic_{id}\">...</div>");
        }
    }

    public class LinkChainTag : TemplateNode
    {
        private RenderingKey key;

        public LinkChainTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(key);
            if (temp == null)
            {
                return;
            }

            var chain = temp as Chain;
            if (chain != null)
            {
                context.output.Append($"<a href=\"/chain/{chain.Address.Text}\">{chain.Name}</a>");
            }
            else
            {
                context.output.Append("-");
            }
        }
    }

    public class LinkTransactionTag : TemplateNode
    {
        private RenderingKey key;

        public LinkTransactionTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(key);
            if (temp == null)
            {
                return;
            }

            var tx = (string)temp;
            context.output.Append($"<a href=/tx/{tx}><small>{tx}</small></a>");
        }
    }

    public class LinkBlockTag : TemplateNode
    {
        private RenderingKey key;

        public LinkBlockTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(key);
            if (temp == null)
            {
                return;
            }

            var hash = (string)temp;
            context.output.Append($"<a href=/block/{hash}><small>{hash}</small></a>");
        }
    }

    public class LinkAddressTag : TemplateNode
    {
        private RenderingKey key;

        public LinkAddressTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(key);
            if (temp == null)
            {
                return;
            }

            var address = (string)temp;
            context.output.Append($"<a href=/address/{address}>{address}</a>");
        }
    }

    public class DescriptionTag : TemplateNode
    {
        private RenderingKey key;

        public DescriptionTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = RenderingKey.Parse(key, RenderingType.String);
        }

        public override void Execute(RenderingContext context)
        {
            var temp = context.EvaluateObject(key);
            if (temp == null)
            {
                return;
            }
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

        }
    }

}
