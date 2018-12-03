using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudBoilerplateNet.Models
{
    public class WebhookNotificationEventArgs : EventArgs, IEquatable<WebhookNotificationEventArgs>
    {
        public IdentifierSet IdentifierSet { get; }
        public string Operation { get; }
        public DateTime CreatedTimestamp { get; }

        public WebhookNotificationEventArgs(IdentifierSet identifierSet, string operation, DateTime createdTimestamp)
        {
            if (identifierSet != null && !string.IsNullOrEmpty(operation))
            {
                IdentifierSet = identifierSet;
                Operation = operation;
                CreatedTimestamp = createdTimestamp;
            }
        }

        public bool Equals(WebhookNotificationEventArgs other)
        {
            if (other != null && ReferenceEquals(this, other))
            {
                return true;
            }

            return Operation.Equals(other.Operation, StringComparison.Ordinal) && IdentifierSet.Equals(other.IdentifierSet);
        }
    }
}
