using Phantasma.Blockchain;
using Phantasma.Blockchain.Contracts;
using Phantasma.Explorer.Infrastructure.Interfaces;

namespace Phantasma.Explorer.ViewModels
{
    public class EventViewModel
    {
        public EventKind Kind { get; set; }
        public string Content { get; set; }

        internal static EventViewModel FromEvent(IRepository repository, Transaction tx, Event evt)
        {
            var block = repository.NexusChain.FindBlockForTransaction(tx);

            return new EventViewModel()
            {
                Kind = evt.Kind,
                Content = repository.GetEventContent(block, evt)
            };
        }
    }
}
