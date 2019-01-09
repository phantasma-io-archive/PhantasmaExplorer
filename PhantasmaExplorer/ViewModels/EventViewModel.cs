using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.ViewModels
{
    public class EventViewModel
    {
        public EvtKind Kind { get; set; }
        public string Content { get; set; }

        internal static EventViewModel FromEvent(IRepository repository, TransactionDto tx, EventDto evt)
        {
            var block = repository.FindBlockForTransaction(tx.Txid);

            return new EventViewModel()
            {
                Kind = evt.EvtKind,
                Content = repository.GetEventContent(block, evt)
            };
        }
    }
}
