using System.Collections.Generic;
using System.Text;
using LunarLabs.WebServer.Templates;

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
}
