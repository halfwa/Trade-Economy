using Automatonymous;
using GreenPipes;
using Trade.Common;
using Trade.Exchanger.Service.Contracts;
using Trade.Exchanger.Service.Entities;
using Trade.Exchanger.Service.Exceptions;
using Trade.Exchanger.Service.StateMachines;

namespace Trade.Exchanger.Service.Activities
{
    public class CalculatePurchaseTotalActivity : Activity<PurchaseState, PurchaseRequested>
    {
        private readonly IRepository<CatalogItem> _repository;

        public CalculatePurchaseTotalActivity(
            IRepository<CatalogItem> repository
        )
        {
            _repository = repository;
        }

        public void Accept(StateMachineVisitor visitor)
        {
           visitor.Visit(this);
        }

        public async Task Execute(BehaviorContext<PurchaseState, PurchaseRequested> context, Behavior<PurchaseState, PurchaseRequested> next)
        {
            var message = context.Data;

            var item = await _repository.GetAsync(message.ItemId);

            if (item == null)
            {
                throw new UnknownItemException(message.ItemId);
            }

            context.Instance.PurchaseTotal = item.Price * message.Quantity;
            context.Instance.LastUpdated = DateTime.UtcNow; 

            await next.Execute(context).ConfigureAwait(false);
        }

        public Task Faulted<TException>(BehaviorExceptionContext<PurchaseState, PurchaseRequested, TException> context, Behavior<PurchaseState, PurchaseRequested> next) where TException : Exception
        {
            return next.Faulted(context);
        }

        public void Probe(ProbeContext context)
        {
            context.CreateScope("calculate-purchase-total");
        }
    }
}
