using System;
using System.Collections.Generic;
using System.Text;
using LunarLabs.WebServer.Templates;
using Phantasma.Blockchain;
using Phantasma.Cryptography;
using Phantasma.Explorer.Utils;

namespace Phantasma.Explorer.Site
{
    public class PriceTag : TemplateNode
    {
        private string key;

        public PriceTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = key;
        }

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            var temp = TemplateEngine.EvaluateObject(context, pointer, key);
            if (temp == null)
            {
                return;
            }

            if (!decimal.TryParse(temp.ToString(), out var price))
            {
                return;
            }

            output.Append($"{price:F2}");
        }
    }

    public class TimeAgoTag : TemplateNode
    {
        private string key;

        public TimeAgoTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = key;
        }

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            var temp = TemplateEngine.EvaluateObject(context, pointer, key);
            if (temp == null)
            {
                return;
            }

            var timestamp = temp is DateTime timestamp1 ? timestamp1 : new DateTime();

            var timeago = DateUtils.RelativeTime(timestamp);

            output.Append($"{timeago}");
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

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            /*
            var temp = TemplateEngine.EvaluateObject(context, pointer, key);
            if (temp == null)
            {
                return;
            }*/

            var id = rnd.Next().ToString();
            var url = key.Replace("_", "/");
            var obj = "{url:'"+url+"', id:'"+id+"'}";
            output.Append($"<script>dynamicContents.push({obj});</script><div style=\"display:inline\" id=\"dynamic_{id}\">...</div>");
        }
    }

    public class LinkChainTag : TemplateNode
    {
        private string key;

        public LinkChainTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = key;
        }

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            var temp = TemplateEngine.EvaluateObject(context, pointer, key);
            if (temp == null)
            {
                return;
            }

            var chain = (Chain)temp;
            output.Append($"<a href=\"/chain/{chain.Address.Text}\">{chain.Name}</a>");
        }
    }

    public class LinkTransactionTag : TemplateNode
    {
        private string key;

        public LinkTransactionTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = key;
        }

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            var temp = TemplateEngine.EvaluateObject(context, pointer, key);
            if (temp == null)
            {
                return;
            }

            var tx = (Transaction)temp;
            output.Append($"<a href=\"/tx/{tx.Hash}\">{tx.Hash}</a>");
        }
    }

    public class LinkAddressTag : TemplateNode
    {
        private string key;

        public LinkAddressTag(TemplateDocument doc, string key) : base(doc)
        {
            this.key = key;
        }

        public override void Execute(Queue<TemplateDocument> queue, object context, object pointer, StringBuilder output)
        {
            var temp = TemplateEngine.EvaluateObject(context, pointer, key);
            if (temp == null)
            {
                return;
            }

            var address = (Address)temp;
            output.Append($"<a href=\"address/{address.Text}\">{address.Text}</a>");
        }
    }

}
