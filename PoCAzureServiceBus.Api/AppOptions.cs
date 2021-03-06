﻿namespace PoCAzureServiceBus.Api
{
    using Microsoft.Azure.Management.ServiceBus.Models;
    public class AppOptions
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string SubscriptionId { get; set; }
        public string DataCenterLocation { get; set; }
        public SkuName ServiceBusSkuName { get; set; }
        public SkuTier? ServiceBusSkuTier { get; set; }
    }
}
