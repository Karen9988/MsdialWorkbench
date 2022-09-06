﻿using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Normalize;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class InternalStandardLowessNormalizeModel
    {
        private readonly AlignmentResultContainer _container;
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly IMessageBroker _messageBroker;

        public InternalStandardLowessNormalizeModel(AlignmentResultContainer container, IReadOnlyList<AnalysisFileBean> files, IMessageBroker messageBroker) {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _files = files;
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        }

        public void Normalize() {
            var _broker = _messageBroker;
            var task = TaskNotification.Start("Normalize..");
            var publisher = new TaskProgressPublisher(_broker, task);
            using (publisher.Start()) {
                Normalization.ISNormThenByLowessNormalize(_files, _container.AlignmentSpotProperties, IonAbundanceUnit.NormalizedByInternalStandardPeakHeight);
                _container.IsNormalized = true;
            }
        }

        public ReadOnlyReactivePropertySlim<bool> CanNormalizeProperty = new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true));
    }
}