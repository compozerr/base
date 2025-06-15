# Complete Module Development Example

This document provides a comprehensive example of developing a new feature across both the backend and frontend of a module. We'll create a subscription management feature in the `stripe` module.

## Feature Requirements

Let's imagine we need to implement a feature that allows users to:
1. View subscription details for a project
2. Change the subscription tier
3. Cancel a subscription

## 1. Backend Implementation

### Step 1: Define the Models and DTOs

First, we'll define the necessary DTOs in the module's backend:

```csharp
// modules/stripe/backend/Stripe/Models/SubscriptionDto.cs
using System;
using Api.Abstractions;

namespace Stripe.Models
{
    public class SubscriptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;
        public ServerTierId ServerTierId { get; set; } = null!;
        public DateTime CurrentPeriodStart { get; set; }
        public DateTime CurrentPeriodEnd { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
```

### Step 2: Implement the Subscription Endpoints

#### Get Project Subscriptions

```csharp
// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Get/GetProjectSubscriptionsQuery.cs
using System.Threading;
using System.Threading.Tasks;
using Api.Abstractions;
using MediatR;

namespace Stripe.Endpoints.Subscriptions.Get
{
    public class GetProjectSubscriptionsQuery : IRequest<GetProjectSubscriptionsResponse>
    {
        public ProjectId ProjectId { get; set; } = null!;
    }
}

// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Get/GetProjectSubscriptionsResponse.cs
using System.Collections.Generic;
using Stripe.Models;

namespace Stripe.Endpoints.Subscriptions.Get
{
    public class GetProjectSubscriptionsResponse
    {
        public List<SubscriptionDto> Subscriptions { get; set; } = new();
    }
}

// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Get/GetProjectSubscriptionsQueryHandler.cs
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.Subscriptions.Get
{
    public class GetProjectSubscriptionsQueryHandler 
        : IRequestHandler<GetProjectSubscriptionsQuery, GetProjectSubscriptionsResponse>
    {
        private readonly IStripeService _stripeService;

        public GetProjectSubscriptionsQueryHandler(IStripeService stripeService)
        {
            _stripeService = stripeService;
        }

        public async Task<GetProjectSubscriptionsResponse> Handle(
            GetProjectSubscriptionsQuery request, 
            CancellationToken cancellationToken)
        {
            var subscriptions = await _stripeService.GetSubscriptionsForProjectAsync(
                request.ProjectId, 
                cancellationToken);

            return new GetProjectSubscriptionsResponse
            {
                Subscriptions = subscriptions
            };
        }
    }
}

// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Get/GetProjectSubscriptionsRoute.cs
using System.Threading.Tasks;
using Api.Abstractions;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Stripe.Endpoints.Subscriptions.Get
{
    public class GetProjectSubscriptionsRoute : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/v1/projects/{projectId}/subscriptions", async (
                string projectId,
                ISender sender) =>
            {
                var result = await sender.Send(new GetProjectSubscriptionsQuery
                {
                    ProjectId = new ProjectId(projectId)
                });

                return Results.Ok(result);
            })
            .WithName("GetProjectSubscriptions")
            .WithTags("Subscriptions")
            .RequireAuthorization();
        }
    }
}
```

#### Update Subscription Tier

```csharp
// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Update/UpdateSubscriptionCommand.cs
using System.Threading;
using System.Threading.Tasks;
using Api.Abstractions;
using MediatR;

namespace Stripe.Endpoints.Subscriptions.Update
{
    public class UpdateSubscriptionCommand : IRequest<UpdateSubscriptionResponse>
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public ServerTierId ServerTierId { get; set; } = null!;
        public ProjectId ProjectId { get; set; } = null!;
    }
}

// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Update/UpdateSubscriptionResponse.cs
using Stripe.Models;

namespace Stripe.Endpoints.Subscriptions.Update
{
    public class UpdateSubscriptionResponse
    {
        public SubscriptionDto Subscription { get; set; } = null!;
    }
}

// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Update/UpdateSubscriptionCommandHandler.cs
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.Subscriptions.Update
{
    public class UpdateSubscriptionCommandHandler 
        : IRequestHandler<UpdateSubscriptionCommand, UpdateSubscriptionResponse>
    {
        private readonly IStripeService _stripeService;

        public UpdateSubscriptionCommandHandler(IStripeService stripeService)
        {
            _stripeService = stripeService;
        }

        public async Task<UpdateSubscriptionResponse> Handle(
            UpdateSubscriptionCommand request, 
            CancellationToken cancellationToken)
        {
            var updatedSubscription = await _stripeService.UpdateSubscriptionTierAsync(
                request.SubscriptionId,
                request.ServerTierId,
                request.ProjectId,
                cancellationToken);

            return new UpdateSubscriptionResponse
            {
                Subscription = updatedSubscription
            };
        }
    }
}

// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Update/UpdateSubscriptionRoute.cs
using System.Threading.Tasks;
using Api.Abstractions;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Stripe.Endpoints.Subscriptions.Update
{
    public class UpdateSubscriptionRoute : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("/api/v1/projects/{projectId}/subscriptions/{subscriptionId}", async (
                string projectId,
                string subscriptionId,
                UpdateSubscriptionRequest request,
                ISender sender) =>
            {
                var result = await sender.Send(new UpdateSubscriptionCommand
                {
                    ProjectId = new ProjectId(projectId),
                    SubscriptionId = subscriptionId,
                    ServerTierId = new ServerTierId(request.ServerTierId)
                });

                return Results.Ok(result);
            })
            .WithName("UpdateSubscription")
            .WithTags("Subscriptions")
            .RequireAuthorization();
        }
    }
}

// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Update/UpdateSubscriptionRequest.cs
namespace Stripe.Endpoints.Subscriptions.Update
{
    public class UpdateSubscriptionRequest
    {
        public string ServerTierId { get; set; } = string.Empty;
    }
}
```

#### Cancel Subscription

```csharp
// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Cancel/CancelSubscriptionCommand.cs
using System.Threading;
using System.Threading.Tasks;
using Api.Abstractions;
using MediatR;

namespace Stripe.Endpoints.Subscriptions.Cancel
{
    public class CancelSubscriptionCommand : IRequest<CancelSubscriptionResponse>
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public ProjectId ProjectId { get; set; } = null!;
        public bool CancelImmediately { get; set; } = false;
    }
}

// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Cancel/CancelSubscriptionResponse.cs
using Stripe.Models;

namespace Stripe.Endpoints.Subscriptions.Cancel
{
    public class CancelSubscriptionResponse
    {
        public SubscriptionDto Subscription { get; set; } = null!;
    }
}

// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Cancel/CancelSubscriptionCommandHandler.cs
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.Subscriptions.Cancel
{
    public class CancelSubscriptionCommandHandler 
        : IRequestHandler<CancelSubscriptionCommand, CancelSubscriptionResponse>
    {
        private readonly IStripeService _stripeService;

        public CancelSubscriptionCommandHandler(IStripeService stripeService)
        {
            _stripeService = stripeService;
        }

        public async Task<CancelSubscriptionResponse> Handle(
            CancelSubscriptionCommand request, 
            CancellationToken cancellationToken)
        {
            var cancelledSubscription = await _stripeService.CancelSubscriptionAsync(
                request.SubscriptionId,
                request.ProjectId,
                request.CancelImmediately,
                cancellationToken);

            return new CancelSubscriptionResponse
            {
                Subscription = cancelledSubscription
            };
        }
    }
}

// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Cancel/CancelSubscriptionRoute.cs
using System.Threading.Tasks;
using Api.Abstractions;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Stripe.Endpoints.Subscriptions.Cancel
{
    public class CancelSubscriptionRoute : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/v1/projects/{projectId}/subscriptions/{subscriptionId}", async (
                string projectId,
                string subscriptionId,
                CancelSubscriptionRequest request,
                ISender sender) =>
            {
                var result = await sender.Send(new CancelSubscriptionCommand
                {
                    ProjectId = new ProjectId(projectId),
                    SubscriptionId = subscriptionId,
                    CancelImmediately = request.CancelImmediately
                });

                return Results.Ok(result);
            })
            .WithName("CancelSubscription")
            .WithTags("Subscriptions")
            .RequireAuthorization();
        }
    }
}

// modules/stripe/backend/Stripe/Endpoints/Subscriptions/Cancel/CancelSubscriptionRequest.cs
namespace Stripe.Endpoints.Subscriptions.Cancel
{
    public class CancelSubscriptionRequest
    {
        public bool CancelImmediately { get; set; } = false;
    }
}
```

### Step 3: Implement the Stripe Service

```csharp
// modules/stripe/backend/Stripe/Services/IStripeService.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Abstractions;
using Stripe.Models;

namespace Stripe.Services
{
    public interface IStripeService
    {
        Task<List<SubscriptionDto>> GetSubscriptionsForProjectAsync(
            ProjectId projectId, 
            CancellationToken cancellationToken = default);
            
        Task<SubscriptionDto> UpdateSubscriptionTierAsync(
            string subscriptionId,
            ServerTierId serverTierId,
            ProjectId projectId,
            CancellationToken cancellationToken = default);
            
        Task<SubscriptionDto> CancelSubscriptionAsync(
            string subscriptionId,
            ProjectId projectId,
            bool cancelImmediately,
            CancellationToken cancellationToken = default);
    }
}

// modules/stripe/backend/Stripe/Services/StripeService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Api.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe.Models;
using Stripe.Options;
using StripeSdk = Stripe;

namespace Stripe.Services
{
    public class StripeService : IStripeService
    {
        private readonly StripeOptions _options;
        private readonly ILogger<StripeService> _logger;
        private readonly StripeSdk.StripeClient _stripeClient;

        public StripeService(
            IOptions<StripeOptions> options,
            ILogger<StripeService> logger)
        {
            _options = options.Value;
            _logger = logger;
            _stripeClient = new StripeSdk.StripeClient(_options.ApiKey);
        }

        public async Task<List<SubscriptionDto>> GetSubscriptionsForProjectAsync(
            ProjectId projectId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var service = new StripeSdk.SubscriptionService(_stripeClient);
                var options = new StripeSdk.SubscriptionListOptions
                {
                    Customer = GetCustomerIdFromProject(projectId.Value),
                    Expand = new List<string> { "data.plan.product" }
                };
                
                var subscriptions = await service.ListAsync(options, cancellationToken: cancellationToken);
                
                return subscriptions.Select(MapToSubscriptionDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions for project {ProjectId}", projectId.Value);
                throw;
            }
        }
        
        public async Task<SubscriptionDto> UpdateSubscriptionTierAsync(
            string subscriptionId,
            ServerTierId serverTierId,
            ProjectId projectId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var service = new StripeSdk.SubscriptionService(_stripeClient);
                
                // Get the price ID for the selected tier
                string priceId = GetPriceIdForTier(serverTierId.Value);
                
                var options = new StripeSdk.SubscriptionUpdateOptions
                {
                    Items = new List<StripeSdk.SubscriptionItemOptions>
                    {
                        new StripeSdk.SubscriptionItemOptions
                        {
                            Id = await GetSubscriptionItemId(subscriptionId, cancellationToken),
                            Price = priceId,
                        }
                    },
                    ProrationBehavior = "create_prorations",
                    Expand = new List<string> { "plan.product" }
                };
                
                var subscription = await service.UpdateAsync(subscriptionId, options, cancellationToken: cancellationToken);
                
                return MapToSubscriptionDto(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription tier for project {ProjectId}", projectId.Value);
                throw;
            }
        }
        
        public async Task<SubscriptionDto> CancelSubscriptionAsync(
            string subscriptionId,
            ProjectId projectId,
            bool cancelImmediately,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var service = new StripeSdk.SubscriptionService(_stripeClient);
                
                StripeSdk.Subscription subscription;
                
                if (cancelImmediately)
                {
                    // Cancel immediately
                    subscription = await service.CancelAsync(subscriptionId, null, cancellationToken);
                }
                else
                {
                    // Cancel at period end
                    var options = new StripeSdk.SubscriptionUpdateOptions
                    {
                        CancelAtPeriodEnd = true,
                        Expand = new List<string> { "plan.product" }
                    };
                    
                    subscription = await service.UpdateAsync(subscriptionId, options, cancellationToken: cancellationToken);
                }
                
                return MapToSubscriptionDto(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription for project {ProjectId}", projectId.Value);
                throw;
            }
        }
        
        // Helper methods for mapping and data retrieval
        private SubscriptionDto MapToSubscriptionDto(StripeSdk.Subscription subscription)
        {
            var plan = subscription.Items.Data.FirstOrDefault()?.Plan;
            
            return new SubscriptionDto
            {
                Id = subscription.Id,
                Name = plan?.Product?.Name ?? "Unknown Plan",
                Status = subscription.Status,
                PlanId = plan?.Id ?? "",
                ServerTierId = new ServerTierId(GetTierIdFromPriceId(plan?.Id ?? "")),
                CurrentPeriodStart = subscription.CurrentPeriodStart,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                Amount = plan?.Amount ?? 0 / 100m,
                Currency = plan?.Currency?.ToUpper() ?? "USD"
            };
        }
        
        private string GetCustomerIdFromProject(string projectId)
        {
            // In a real implementation, this would look up the Stripe customer ID
            // associated with the project in your database
            return $"cus_project_{projectId}";
        }
        
        private string GetPriceIdForTier(string tierId)
        {
            // Map tier IDs to Stripe price IDs
            var priceTierMap = new Dictionary<string, string>
            {
                {"basic", "price_basic_monthly"},
                {"professional", "price_professional_monthly"},
                {"enterprise", "price_enterprise_monthly"}
            };
            
            if (priceTierMap.TryGetValue(tierId.ToLower(), out var priceId))
            {
                return priceId;
            }
            
            throw new ArgumentException($"No price found for tier ID {tierId}");
        }
        
        private string GetTierIdFromPriceId(string priceId)
        {
            // Map Stripe price IDs back to tier IDs
            var tierPriceMap = new Dictionary<string, string>
            {
                {"price_basic_monthly", "basic"},
                {"price_professional_monthly", "professional"},
                {"price_enterprise_monthly", "enterprise"}
            };
            
            if (tierPriceMap.TryGetValue(priceId.ToLower(), out var tierId))
            {
                return tierId;
            }
            
            return "basic"; // Default fallback
        }
        
        private async Task<string> GetSubscriptionItemId(string subscriptionId, CancellationToken cancellationToken)
        {
            var service = new StripeSdk.SubscriptionService(_stripeClient);
            var subscription = await service.GetAsync(subscriptionId, cancellationToken: cancellationToken);
            
            return subscription.Items.Data.FirstOrDefault()?.Id 
                ?? throw new Exception($"No subscription item found for subscription {subscriptionId}");
        }
    }
}
```

### Step 4: Add Configuration Settings

```csharp
// modules/stripe/backend/Stripe/Options/StripeOptions.cs
namespace Stripe.Options
{
    public class StripeOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }
}

// modules/stripe/backend/Stripe/StripeFeature.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Core.Abstractions;
using Stripe.Options;
using Stripe.Services;

namespace Stripe
{
    public class StripeFeature : IFeature
    {
        public void AddFeature(WebApplicationBuilder builder)
        {
            // Register options
            builder.Services.Configure<StripeOptions>(
                builder.Configuration.GetSection("Stripe"));
            
            // Register services
            builder.Services.AddScoped<IStripeService, StripeService>();
        }
        
        public void UseFeature(WebApplication app)
        {
            // No middleware needed for this feature
        }
    }
}
```

## 2. Frontend Implementation

### Step 1: Create the Subscription Management Components

```tsx
// modules/stripe/frontend/src/components/subscription-status.tsx
import React from 'react';

interface SubscriptionStatusProps {
  status: string;
}

export default function SubscriptionStatus({ status }: SubscriptionStatusProps) {
  const getStatusColor = () => {
    switch (status.toLowerCase()) {
      case 'active':
        return 'bg-green-100 text-green-800';
      case 'trialing':
        return 'bg-blue-100 text-blue-800';
      case 'past_due':
        return 'bg-yellow-100 text-yellow-800';
      case 'canceled':
      case 'unpaid':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };
  
  const getStatusLabel = () => {
    switch (status.toLowerCase()) {
      case 'active':
        return 'Active';
      case 'trialing':
        return 'Trial';
      case 'past_due':
        return 'Past Due';
      case 'canceled':
        return 'Canceled';
      case 'unpaid':
        return 'Unpaid';
      default:
        return status;
    }
  };
  
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusColor()}`}>
      {getStatusLabel()}
    </span>
  );
}

// modules/stripe/frontend/src/components/subscription-list.tsx
import React from 'react';
import { api } from '@/api-client';
import SubscriptionStatus from './subscription-status';

interface SubscriptionListProps {
  projectId: string;
}

export default function SubscriptionList({ projectId }: SubscriptionListProps) {
  const { data, isLoading, error } = api.v1.getProjectsProjectIdSubscriptions.useQuery({
    path: { projectId }
  });
  
  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="h-8 bg-gray-200 animate-pulse rounded w-1/3"></div>
        <div className="h-32 bg-gray-100 animate-pulse rounded"></div>
      </div>
    );
  }
  
  if (error) {
    return (
      <div className="rounded-md bg-red-50 p-4">
        <div className="flex">
          <div className="text-sm text-red-700">
            Error loading subscription information: {error.message}
          </div>
        </div>
      </div>
    );
  }
  
  if (!data?.subscriptions || data.subscriptions.length === 0) {
    return (
      <div className="rounded-md bg-blue-50 p-4">
        <div className="flex">
          <div className="text-sm text-blue-700">
            No subscription found for this project. 
            <a href="/billing" className="font-medium underline">
              Upgrade now
            </a>
          </div>
        </div>
      </div>
    );
  }
  
  return (
    <div className="space-y-4">
      <h3 className="text-lg font-medium">Subscription</h3>
      
      {data.subscriptions.map(subscription => (
        <div key={subscription.id} className="border rounded-md p-4 space-y-3">
          <div className="flex items-center justify-between">
            <div className="font-medium">{subscription.name}</div>
            <SubscriptionStatus status={subscription.status} />
          </div>
          
          <div className="text-sm text-gray-500 space-y-1">
            <div>
              Billing period: {new Date(subscription.currentPeriodStart).toLocaleDateString()} - {new Date(subscription.currentPeriodEnd).toLocaleDateString()}
            </div>
            <div>
              {subscription.cancelAtPeriodEnd && 
                <span className="text-amber-600">
                  Will cancel at the end of billing period
                </span>
              }
            </div>
          </div>
          
          <div className="pt-2 flex justify-end">
            <button 
              onClick={() => window.location.href = `/projects/${projectId}/settings/billing`}
              className="text-sm text-blue-600 hover:text-blue-800"
            >
              Manage subscription
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}

// modules/stripe/frontend/src/components/tier-selector.tsx
import React, { useState } from 'react';
import { api } from '@/api-client';

interface TierSelectorProps {
  projectId: string;
  currentTierId?: string;
  subscriptionId?: string;
  onTierChange?: (tierId: string) => void;
  disabled?: boolean;
}

export default function TierSelector({ 
  projectId, 
  currentTierId,
  subscriptionId,
  onTierChange,
  disabled = false
}: TierSelectorProps) {
  const [selectedTierId, setSelectedTierId] = useState(currentTierId || '');
  const [isChanging, setIsChanging] = useState(false);
  
  const { data: tierData } = api.v1.getServersTiers.useQuery();
  
  const { mutateAsync: updateSubscription } = api.v1.putProjectsProjectIdSubscriptionsSubscriptionId.useMutation({
    path: { projectId, subscriptionId: subscriptionId || '' }
  });
  
  const handleTierChange = async () => {
    if (!subscriptionId || selectedTierId === currentTierId) return;
    
    setIsChanging(true);
    
    try {
      await updateSubscription({
        body: {
          serverTierId: selectedTierId
        }
      });
      
      // Invalidate queries to refresh data
      api.v1.getProjectsProjectIdSubscriptions.invalidateQueries({
        parameters: { path: { projectId } }
      });
      
      onTierChange?.(selectedTierId);
    } catch (error) {
      console.error("Failed to update subscription tier:", error);
      // Set back to current tier on error
      setSelectedTierId(currentTierId || '');
    } finally {
      setIsChanging(false);
    }
  };
  
  if (!tierData?.tiers) {
    return (
      <div className="h-32 bg-gray-100 animate-pulse rounded"></div>
    );
  }
  
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {tierData.tiers.map((tier) => (
          <div 
            key={tier.id?.value}
            className={`border rounded-md p-4 cursor-pointer ${
              selectedTierId === tier.id?.value 
                ? 'border-blue-500 ring-2 ring-blue-200' 
                : 'hover:border-gray-300'
            }`}
            onClick={() => !disabled && setSelectedTierId(tier.id?.value || '')}
          >
            <div className="font-medium">{tier.name}</div>
            <div className="text-2xl font-bold">${tier.pricePerMonth}/mo</div>
            <div className="text-sm text-gray-500 mt-2">
              {tier.cpuLimit} CPU, {tier.memoryLimitMb}MB RAM
            </div>
          </div>
        ))}
      </div>
      
      {subscriptionId && selectedTierId !== currentTierId && (
        <div className="flex justify-end">
          <button
            onClick={handleTierChange}
            disabled={isChanging || disabled}
            className={`px-4 py-2 rounded text-white ${
              isChanging || disabled
                ? 'bg-gray-400'
                : 'bg-blue-600 hover:bg-blue-700'
            }`}
          >
            {isChanging ? 'Updating...' : 'Change Tier'}
          </button>
        </div>
      )}
    </div>
  );
}

// modules/stripe/frontend/src/components/cancel-subscription-button.tsx
import React, { useState } from 'react';
import { api } from '@/api-client';

interface CancelSubscriptionButtonProps {
  projectId: string;
  subscriptionId: string;
}

export default function CancelSubscriptionButton({ 
  projectId, 
  subscriptionId 
}: CancelSubscriptionButtonProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [isImmediate, setIsImmediate] = useState(false);
  const [isCancelling, setIsCancelling] = useState(false);
  
  const { mutateAsync: cancelSubscription } = api.v1.deleteProjectsProjectIdSubscriptionsSubscriptionId.useMutation({
    path: { projectId, subscriptionId }
  });
  
  const handleCancel = async () => {
    setIsCancelling(true);
    
    try {
      await cancelSubscription({
        body: {
          cancelImmediately: isImmediate
        }
      });
      
      // Invalidate queries to refresh data
      api.v1.getProjectsProjectIdSubscriptions.invalidateQueries({
        parameters: { path: { projectId } }
      });
      
      setIsOpen(false);
    } catch (error) {
      console.error("Failed to cancel subscription:", error);
    } finally {
      setIsCancelling(false);
    }
  };
  
  return (
    <>
      <button
        onClick={() => setIsOpen(true)}
        className="px-4 py-2 border border-red-300 text-red-700 rounded hover:bg-red-50"
      >
        Cancel Subscription
      </button>
      
      {isOpen && (
        <div className="fixed inset-0 bg-black bg-opacity-25 flex items-center justify-center z-50">
          <div className="bg-white p-6 rounded-lg shadow-lg max-w-md w-full">
            <h3 className="text-xl font-bold mb-4">Cancel Subscription</h3>
            
            <p className="mb-4">
              Are you sure you want to cancel your subscription?
            </p>
            
            <div className="mb-4">
              <label className="flex items-center">
                <input
                  type="checkbox"
                  checked={isImmediate}
                  onChange={() => setIsImmediate(!isImmediate)}
                  className="h-4 w-4 text-blue-600 rounded border-gray-300"
                />
                <span className="ml-2 text-sm">
                  Cancel immediately (Otherwise at end of billing period)
                </span>
              </label>
            </div>
            
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setIsOpen(false)}
                disabled={isCancelling}
                className="px-4 py-2 border rounded"
              >
                Cancel
              </button>
              
              <button
                onClick={handleCancel}
                disabled={isCancelling}
                className={`px-4 py-2 rounded text-white ${
                  isCancelling
                    ? 'bg-gray-400'
                    : 'bg-red-600 hover:bg-red-700'
                }`}
              >
                {isCancelling ? 'Processing...' : 'Confirm Cancellation'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
```

### Step 2: Create the Billing Settings Page in the Module

```tsx
// modules/stripe/frontend/src/routes/stripe/billing/index.tsx
import React from 'react';
import { createFileRoute, useParams } from '@tanstack/react-router';
import { api } from '@/api-client';
import TierSelector from '../../../components/tier-selector';
import SubscriptionStatus from '../../../components/subscription-status';
import CancelSubscriptionButton from '../../../components/cancel-subscription-button';

export const Route = createFileRoute('/stripe/billing/')({
  component: BillingSettingsPage,
});

function BillingSettingsPage() {
  const { projectId } = useParams({ from: '/projects/$projectId/settings/billing' });
  
  const { data: subscriptionData, isLoading } = api.v1.getProjectsProjectIdSubscriptions.useQuery({
    path: { projectId }
  });
  
  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 bg-gray-200 animate-pulse rounded w-1/3"></div>
        <div className="h-64 bg-gray-100 animate-pulse rounded"></div>
      </div>
    );
  }
  
  const subscription = subscriptionData?.subscriptions?.[0];
  
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold">Billing Settings</h2>
        <p className="text-gray-500">
          Manage your subscription and billing details
        </p>
      </div>
      
      {subscription ? (
        <div className="space-y-8">
          <div className="bg-white rounded-md shadow p-6">
            <h3 className="text-lg font-medium mb-4">Current Subscription</h3>
            
            <div className="flex items-center justify-between mb-4">
              <div>
                <div className="font-medium">{subscription.name}</div>
                <div className="text-sm text-gray-500">
                  ${(subscription.amount / 100).toFixed(2)}/{subscription.currency.toLowerCase()} per month
                </div>
              </div>
              
              <SubscriptionStatus status={subscription.status} />
            </div>
            
            <div className="text-sm text-gray-500 space-y-1 mb-4">
              <div>
                Billing period: {new Date(subscription.currentPeriodStart).toLocaleDateString()} - {new Date(subscription.currentPeriodEnd).toLocaleDateString()}
              </div>
              {subscription.cancelAtPeriodEnd && (
                <div className="text-amber-600">
                  Your subscription will be canceled at the end of the current billing period.
                </div>
              )}
            </div>
          </div>
          
          <div className="bg-white rounded-md shadow p-6">
            <h3 className="text-lg font-medium mb-4">Change Plan</h3>
            
            <TierSelector 
              projectId={projectId}
              currentTierId={subscription.serverTierId.value}
              subscriptionId={subscription.id}
              disabled={subscription.cancelAtPeriodEnd}
            />
          </div>
          
          <div className="bg-white rounded-md shadow p-6">
            <h3 className="text-lg font-medium mb-4">Cancel Subscription</h3>
            
            <p className="text-gray-500 mb-4">
              Canceling your subscription will stop billing but keep your project accessible until the end of your billing period.
            </p>
            
            {subscription.cancelAtPeriodEnd ? (
              <div className="text-amber-600">
                Your subscription is already set to cancel at the end of the current billing period.
              </div>
            ) : (
              <CancelSubscriptionButton 
                projectId={projectId}
                subscriptionId={subscription.id}
              />
            )}
          </div>
        </div>
      ) : (
        <div className="bg-white rounded-md shadow p-6">
          <h3 className="text-lg font-medium mb-4">No Active Subscription</h3>
          
          <p className="text-gray-500 mb-4">
            You don't have an active subscription for this project.
          </p>
          
          <div className="bg-white rounded-md shadow p-6">
            <h3 className="text-lg font-medium mb-4">Select a Plan</h3>
            
            <TierSelector projectId={projectId} />
          </div>
        </div>
      )}
    </div>
  );
}
```

### Step 3: Update the Package.json to Export Components

```json
// modules/stripe/frontend/package.json
{
  "name": "@repo/stripe",
  "version": "0.0.0",
  "private": true,
  "exports": {
    "./stripe-component": "./src/stripe-component.tsx",
    "./subscription-list": "./src/components/subscription-list.tsx",
    "./tier-selector": "./src/components/tier-selector.tsx"
  },
  "devDependencies": {
    "@repo/config": "*"
  }
}
```

## 3. Integration with Main Application

### Step 1: Add Subscription List to Project Details Page

```tsx
// frontend/src/routes/_auth/_dashboard/projects/$projectId/index.tsx
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Formatter } from '@/lib/formatter'
import { getProjectStateFromNumber as getProjectStateFromStateString, ProjectState } from '@/lib/project-state'
import { createFileRoute, getRouteApi } from '@tanstack/react-router'
import { Cpu, ExternalLink, Globe, HardDrive, MemoryStick } from 'lucide-react'
import { UsageGraph } from '@/components/usage-graph'
import StartStopProjectButton from '@/components/project/project-startstop-button'
import { api } from '@/api-client'
import { useMemo } from 'react'
import { Skeleton } from '@/components/ui/skeleton'
// Import the subscription list component from the stripe module
import SubscriptionList from '@repo/stripe/subscription-list'

export const Route = createFileRoute('/_auth/_dashboard/projects/$projectId/')({
    component: RouteComponent,
})

function RouteComponent() {
    const { projectId } = getRouteApi("/_auth/_dashboard/projects/$projectId").useLoaderData();
    const { data: project } = api.v1.getProjectsProjectId.useQuery({ path: { projectId } }, {
        refetchInterval: (project) => project.state.data?.state === ProjectState.Starting ? 2000 : false,
    })

    // ...existing code...

    return (
        <div className="space-y-6">
            {/* Existing project details */}
            {/* ... */}
            
            {/* Add subscription info */}
            <Card>
                <CardContent className="pt-6">
                    <SubscriptionList projectId={projectId} />
                </CardContent>
            </Card>
            
            {/* Other project info */}
            {/* ... */}
        </div>
    );
}
```

### Step 2: Add Billing Settings Route to Main Application

```tsx
// frontend/src/routes/_auth/_dashboard/projects/$projectId/settings/billing.tsx
import { createFileRoute } from '@tanstack/react-router'
import { lazy } from 'react'
import { TabsContent } from '@/components/ui/tabs'

// Import the billing page component from the stripe module
// Using lazy loading for better performance
const BillingSettingsPage = lazy(() => import('@repo/stripe/billing-page'))

export const Route = createFileRoute('/_auth/_dashboard/projects/$projectId/settings/billing')({
  component: BillingSettingsRoute,
})

function BillingSettingsRoute() {
  return (
    <TabsContent value="billing" className="space-y-4">
      <BillingSettingsPage />
    </TabsContent>
  )
}
```

### Step 3: Add Billing Tab to Settings Navigation

```tsx
// frontend/src/routes/_auth/_dashboard/projects/$projectId/settings/route.tsx
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Outlet } from '@tanstack/react-router'
import { createFileRoute, useParams } from '@tanstack/react-router'
import { Link } from '@/components/ui/link'

export const Route = createFileRoute('/_auth/_dashboard/projects/$projectId/settings/')({
  component: SettingsLayout,
})

function SettingsLayout() {
  const { projectId } = useParams({ from: '/_auth/_dashboard/projects/$projectId/settings/' })

  return (
    <>
      <Tabs defaultValue="general" className="w-full">
        <TabsList>
          <Link to="/projects/$projectId/settings/general" params={{ projectId }}>
            <TabsTrigger value="general">General</TabsTrigger>
          </Link>
          <Link to="/projects/$projectId/settings/environment" params={{ projectId }}>
            <TabsTrigger value="environment">Environment</TabsTrigger>
          </Link>
          <Link to="/projects/$projectId/settings/domains" params={{ projectId }}>
            <TabsTrigger value="domains">Domains</TabsTrigger>
          </Link>
          {/* Add new billing tab */}
          <Link to="/projects/$projectId/settings/billing" params={{ projectId }}>
            <TabsTrigger value="billing">Billing</TabsTrigger>
          </Link>
        </TabsList>

        <Outlet />
      </Tabs>
    </>
  )
}
```

## 4. Testing the Feature

To test the subscription management feature:

1. Start both backend and frontend:
   ```bash
   npm run dev
   ```

2. Navigate to a project's details page to see the subscription information panel

3. Go to the project settings and click on the "Billing" tab to access the full subscription management UI

4. Test updating the subscription tier and canceling a subscription

## Conclusion

This example demonstrates the complete process of implementing a new feature across both the backend and frontend of a module. We've created:

1. Backend CQRS endpoints for subscription management
2. Frontend components for displaying and managing subscriptions
3. Integration with the main application to expose the new features

This modular approach keeps related functionality together while allowing it to be integrated seamlessly into the main application.
