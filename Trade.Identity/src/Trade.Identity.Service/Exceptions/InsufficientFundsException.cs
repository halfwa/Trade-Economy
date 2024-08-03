using System;

namespace Trade.Identity.Service.Exceptions
{
    [Serializable]
    internal class InsufficientFundsException : Exception
    {
        public InsufficientFundsException(Guid userId, decimal gilToDebit) 
            : base($"Not enough gil to debit {gilToDebit} from user {userId}")
        {
            UserId = userId;
            GilToDebit = gilToDebit;    
        }

        public Guid UserId { get; set; }
        public decimal GilToDebit { get; set; }
    }
}