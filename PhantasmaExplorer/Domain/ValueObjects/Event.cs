using System.Collections.Generic;
using Phantasma.RpcClient.DTOs;
using Phantasma.Explorer.Domain.Infrastructure;

namespace Phantasma.Explorer.Domain.ValueObjects
{
    public class Event : ValueObject
    {
        public string EventAddress { get; set; }
        public string Data { get; set; }

        public EventKind EventKind { get; set; }


        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return EventAddress;
            yield return Data;
            yield return EventKind;
        }
    }
}
