using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Domain.ValueObjects;

namespace Phantasma.Explorer.ViewModels
{
    public class EventViewModel
    {
        public EventKind Kind { get; set; }
        public string Content { get; set; }

        internal static EventViewModel FromEvent(Transaction tx, Event evt)
        {
            return new EventViewModel
            {
                Kind = evt.EventKind,
                Content = new TransactionQueries().GetEventContent(tx.Block, evt)
            };
        }
    }
}
