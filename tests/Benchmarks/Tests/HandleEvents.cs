﻿// ==========================================================================
//  HandleEvents.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using Benchmarks.Tests.TestData;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Infrastructure.CQRS.Events;
using Squidex.Infrastructure.CQRS.Events.Actors;
using Squidex.Infrastructure.States;

namespace Benchmarks.Tests
{
    public sealed class HandleEvents : IBenchmark
    {
        private const int NumEvents = 5000;
        private IServiceProvider services;
        private IEventStore eventStore;
        private EventConsumerActor eventConsumerActor;
        private EventDataFormatter eventDataFormatter;
        private MyEventConsumer eventConsumer;

        public void RunInitialize()
        {
            services = Services.Create();

            eventConsumer = new MyEventConsumer(NumEvents);

            eventStore = services.GetRequiredService<IEventStore>();

            eventDataFormatter = services.GetRequiredService<EventDataFormatter>();
            eventConsumerActor = services.GetRequiredService<EventConsumerActor>();

            eventConsumerActor.ActivateAsync(services.GetRequiredService<StateHolder<EventConsumerState>>()).Wait();
            eventConsumerActor.Activate(eventConsumer);
        }

        public long Run()
        {
            var streamName = Guid.NewGuid().ToString();

            for (var eventId = 0; eventId < NumEvents; eventId++)
            {
                var eventData = eventDataFormatter.ToEventData(new Envelope<IEvent>(new MyEvent { EventNumber = eventId + 1 }), Guid.NewGuid());

                eventStore.AppendEventsAsync(Guid.NewGuid(), streamName, eventId - 1, new[] { eventData }).Wait();
            }

            eventConsumer.WaitAndVerify();

            return NumEvents;
        }

        public void RunCleanup()
        {
            services.Cleanup();
        }
    }
}
