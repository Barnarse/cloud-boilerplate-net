using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CloudBoilerplateNet.Models;

namespace CloudBoilerplateNet.Helpers
{
    public class ConsecutiveNotificationComparer : IEqualityComparer<WebhookNotificationEventArgs>
    {
        private const int MIN_RETRY_INTERVAL_SECONDS = 1;

        public bool Equals(WebhookNotificationEventArgs x, WebhookNotificationEventArgs y)
        {
            return x.Equals(y) && y.CreatedTimestamp >= x.CreatedTimestamp && y.CreatedTimestamp < x.CreatedTimestamp.AddSeconds(MIN_RETRY_INTERVAL_SECONDS);
        }

        public int GetHashCode(WebhookNotificationEventArgs obj)
        {
            return obj.GetHashCode();
        }
    }
}
