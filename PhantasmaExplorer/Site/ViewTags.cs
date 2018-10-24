using System;
using System.Collections.Generic;
using System.Text;
using LunarLabs.WebServer.Templates;
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
}
