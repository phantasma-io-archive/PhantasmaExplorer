using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Domain.ValueObjects;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.Utils;

namespace Phantasma.Explorer.ViewModels
{
    public class EventViewModel
    {
        public EventKind Kind { get; set; }
        public string Content { get; set; }

        internal static EventViewModel FromEvent(Transaction tx, Event evt)
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>(); //todo remove this
            return new EventViewModel
            {
                Kind = evt.EventKind,
                Content = TransactionUtils.GetEventContent(tx.Block, evt, context.Chains.ToList(), context.Tokens.ToList()) //todo improve
            };
        }
    }
}
