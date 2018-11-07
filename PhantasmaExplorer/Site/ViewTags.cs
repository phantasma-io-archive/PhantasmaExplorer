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

            var chain = (string)temp;
            context.output.Append(!string.IsNullOrEmpty((string)temp) ? $"<a href=\"/chain/{chain}\">{chain}</a>" : "-");
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
            context.output.Append($"<a href=/tx/{tx}>{tx}</a>");
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
            context.output.Append($"<a href=/block/{hash}>{hash}</a>");
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

    public class LinkAppTag : TemplateNode
    {
        private RenderingKey key;

        public LinkAppTag(TemplateDocument doc, string key) : base(doc)
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

            var appId = (string)temp;
            context.output.Append($"<a href=/app/{appId}>{appId}</a>");
        }
    }

    public class AppIconTag : TemplateNode
    {
        private RenderingKey key;

        public AppIconTag(TemplateDocument doc, string key) : base(doc)
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

            var url = (string)temp;
            context.output.Append($"<img src=\"{url}\" alt=\"\" style=\"width: 20px; height: 20px; \">");
        }
    }

    public class LinkExternalTag : TemplateNode
    {
        private RenderingKey key;

        public LinkExternalTag(TemplateDocument doc, string key) : base(doc)
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

            var url = (string)temp;
            context.output.Append($"<a href=\"{url}\">{url}</a>");
        }
    }
}
