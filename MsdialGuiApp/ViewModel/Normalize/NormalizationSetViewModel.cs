﻿using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Normalize
{
    class NormalizationSetViewModel : ViewModelBase
    {
        public NormalizationSetViewModel(
            AlignmentResultContainer container,
            IMatchResultRefer refer,
            ParameterBase parameter) {

            this.container = container;
            this.refer = refer;
            this.parameter = parameter;

            Parameter = new ParameterBaseVM(parameter);
            var notifier = new PropertyChangedNotifier(Parameter);
            Disposables.Add(notifier);
            notifier
                .SubscribeTo(nameof(Parameter.IsNormalizeNone), () => OnPropertyChanged(nameof(CanExecute)))
                .SubscribeTo(nameof(Parameter.IsNormalizeIS), () => OnPropertyChanged(nameof(CanExecute)))
                .SubscribeTo(nameof(Parameter.IsNormalizeLowess), () => OnPropertyChanged(nameof(CanExecute)))
                .SubscribeTo(nameof(Parameter.IsNormalizeIsLowess), () => OnPropertyChanged(nameof(CanExecute)))
                .SubscribeTo(nameof(Parameter.IsNormalizeSplash), () => OnPropertyChanged(nameof(CanExecute)))
                .SubscribeTo(nameof(Parameter.IsNormalizeTic), () => OnPropertyChanged(nameof(CanExecute)))
                .SubscribeTo(nameof(Parameter.IsNormalizeMTic), () => OnPropertyChanged(nameof(CanExecute)));
        }

        public ParameterBaseVM Parameter { get; }

        private readonly AlignmentResultContainer container;
        private readonly IMatchResultRefer refer;
        private readonly ParameterBase parameter;

        public SplashSetViewModel SplashVM {
            get {
                if (splashVM is null) {
                    splashVM = new SplashSetViewModel(container, refer, parameter);
                    Disposables.Add(splashVM);
                }
                return splashVM;
            }
        }
        private SplashSetViewModel splashVM;

        public bool CanExecute {
            get {
                return new[]
                {
                    Parameter.IsNormalizeNone,
                    Parameter.IsNormalizeIS,
                    Parameter.IsNormalizeLowess,
                    Parameter.IsNormalizeIsLowess,
                    Parameter.IsNormalizeSplash,
                    Parameter.IsNormalizeTic,
                    Parameter.IsNormalizeMTic,
                }.Count(isnorm => isnorm) == 1;
            }
        }
    }
}