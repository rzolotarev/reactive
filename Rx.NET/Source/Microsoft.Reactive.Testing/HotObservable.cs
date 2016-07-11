﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Collections.Generic;
using System.Reactive;
using System;
using System.Reactive.Disposables;

namespace Microsoft.Reactive.Testing
{
    class HotObservable<T> : ITestableObservable<T>
    {
        readonly TestScheduler scheduler;
        readonly List<IObserver<T>> observers = new List<IObserver<T>>();
        readonly List<Subscription> subscriptions = new List<Subscription>();
        readonly Recorded<Notification<T>>[] messages;

        public HotObservable(TestScheduler scheduler, params Recorded<Notification<T>>[] messages)
        {
            if (scheduler == null)
                throw new ArgumentNullException(nameof(scheduler));
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            this.scheduler = scheduler;
            this.messages = messages;

            for (var i = 0; i < messages.Length; ++i)
            {
                var notification = messages[i].Value;
                scheduler.ScheduleAbsolute(default(object), messages[i].Time, (scheduler1, state1) =>
                {
                    var _observers = observers.ToArray();
                    for (var j = 0; j < _observers.Length; ++j)
                    {
                        notification.Accept(_observers[j]);
                    }
                    return Disposable.Empty;
                });
            }
        }

        public virtual IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            observers.Add(observer);
            subscriptions.Add(new Subscription(scheduler.Clock));
            var index = subscriptions.Count - 1;

            return Disposable.Create(() =>
            {
                observers.Remove(observer);
                subscriptions[index] = new Subscription(subscriptions[index].Subscribe, scheduler.Clock);
            });
        }

        public IList<Subscription> Subscriptions
        {
            get { return subscriptions; }
        }

        public IList<Recorded<Notification<T>>> Messages
        {
            get { return messages; }
        }
    }
}
