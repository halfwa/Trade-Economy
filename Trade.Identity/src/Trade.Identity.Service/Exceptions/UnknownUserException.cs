using System;

namespace Trade.Identity.Service.Consumers
{
    [Serializable]
    internal class UnknownUserException : Exception
    {
        public UnknownUserException(Guid userId) 
            : base($"Unknown user {userId}")
        {
            UserId= userId;
        }

        public Guid UserId { get; }
    }
}