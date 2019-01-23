using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Domain.ValueObjects;
using Phantasma.Explorer.Utils;

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
                Content = TransactionUtils.GetEventContent(tx.Block, evt) //todo improve
            };
        }
    }
}
